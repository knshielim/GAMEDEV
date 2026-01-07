using UnityEngine;
using System;
using System.Collections;

public enum Team
{
    Player,
    Enemy
}

public abstract class Unit : MonoBehaviour
{
    public event Action OnHealthChanged;

    // --- STAT SCALING TABLES ---
    protected static readonly float[] RarityHpMult = { 1.0f, 1.3f, 1.7f, 2.2f, 3.0f, 5.0f }; // Boss: 5x HP
    protected static readonly float[] RarityAtkMult = { 1.0f, 1.4f, 1.9f, 2.5f, 3.3f, 4.5f }; // Boss: 4.5x Attack
    protected static readonly float[] RaritySpdMult = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f, 1.2f }; // Boss: Slightly faster

    protected static readonly float[] LevelMult = { 1.0f, 1.0f, 1.0f, 1.2f, 1.3f };

    [Header("Config (Data Driven)")]
    [SerializeField] protected TroopData troopData;

    [Header("Base Stats (fallback if troopData = null)")]
    [SerializeField] private float baseMaxHealth = 20f;
    [SerializeField] private float baseMoveSpeed = 1.5f;
    [SerializeField] private float baseAttackSpeed = 1f;
    [SerializeField] private float baseAttackPoints = 3f;
    [SerializeField] private float deathDuration = 1.0f;

    [Header("Runtime Stats")]
    [SerializeField] private float _maxHealth = 20f;
    public float MaxHealth { get => _maxHealth; internal set => _maxHealth = value; }

    public float moveSpeed;
    public float attackSpeed;
    public float attackPoints;

    [Header("State")]
    public float currentHealth;
    public bool isDead { get; protected set; } = false;
    public float CurrentHealth => currentHealth;

    public Team UnitTeam { get; protected set; }

    protected float attackCooldown = 0f;
    protected bool isAttacking = false;
    protected float critRate;
    protected float critDamage;
    private bool statsInitialized = false;


    [Header("Components")]
    public Animator animator;
    private Collider2D unitCollider;
    protected Rigidbody2D rb;

    public TroopData GetTroopData()
    {
        return troopData;
    }

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        if (statsInitialized) 
        {
            unitCollider = GetComponent<Collider2D>();
            if (animator == null) animator = GetComponent<Animator>();
            return; 
        }

        //default value (just in case)
        MaxHealth = baseMaxHealth;
        moveSpeed = baseMoveSpeed;
        attackPoints = baseAttackPoints;
        attackSpeed = baseAttackSpeed;

        /*
        if (troopData != null)
        {
            float baseHp = troopData.maxHealth;
            float baseAtk = troopData.attack;
            float baseMove = troopData.moveSpeed;

            int rarityIndex = (int)troopData.rarity;

            int levelIndex;
            if (GameManager.Instance != null)
            {
                int stageLevel = (int)GameManager.Instance.currentLevel;
                levelIndex = Mathf.Clamp(stageLevel - 1, 0, 4);
            }
            else
            {
                levelIndex = Mathf.Clamp(troopData.level - 1, 0, 4);
            }

            float hpMul = RarityHpMult[rarityIndex] * LevelMult[levelIndex];
            float atkMul = RarityAtkMult[rarityIndex] * LevelMult[levelIndex];
            float moveMul = RaritySpdMult[rarityIndex] * LevelMult[levelIndex];

            MaxHealth = baseHp * hpMul;
            moveSpeed = baseMove * moveMul;
            attackPoints = baseAtk * atkMul;

            attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval);
        }
        else
        {
            moveSpeed = baseMoveSpeed;
            attackSpeed = baseAttackSpeed;
            attackPoints = baseAttackPoints;
        }
        */
        
        if (troopData != null)
        {
            // ====================================================
            // CASE A: THIS IS A PLAYER'S TROOP (Player Prefab)
            // Stats are based on the player's Upgrade Level
            // ====================================================
            if (UnitTeam == Team.Player)
            {
                // 1. Find out what the upgrade level of this troop is now
                int playerTroopLevel = 1;

                if (PersistenceManager.Instance != null)
                {
                    playerTroopLevel = PersistenceManager.Instance.GetTroopLevel(troopData.id);
                }
                else
                {
                    playerTroopLevel = 1;
                }

                // 2. Calculate stats using the Central Formula from TroopInstance
                var stats = TroopInstance.GetStatsForLevel(troopData, playerTroopLevel);

                // 3. Apply Stats
                MaxHealth = stats.hp;
                attackPoints = stats.atk;
                moveSpeed = stats.spd;
                
                // Attack Speed 
                attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval);

                Debug.Log($"[Unit Player] {name} spawned with Level {playerTroopLevel}. HP: {MaxHealth}, Atk: {attackPoints}");
            }

            // ====================================================
            // CASE B: THIS IS THE ENEMY FORCE (Prefab Enemy)
            // Stats are calculated based on Stage Level Difficulty (1-5)
            // ====================================================
            else if (UnitTeam == Team.Enemy)
            {
                // Take the current stage level 
                int stageLevel = 1;
                if (LevelManager.Instance != null)
                {
                    stageLevel = LevelManager.Instance.GetCurrentLevel();
                }

                // Get Multiplier from Array (RarityMult & LevelMult which are above Unit.cs script)
                int levelIndex = Mathf.Clamp(stageLevel - 1, 0, LevelMult.Length - 1);
                int rarityIndex = Mathf.Clamp((int)troopData.rarity, 0, RarityHpMult.Length - 1);

                float hpMul = RarityHpMult[rarityIndex] * LevelMult[levelIndex];
                float atkMul = RarityAtkMult[rarityIndex] * LevelMult[levelIndex];
                float moveMul = RaritySpdMult[rarityIndex]; 

                MaxHealth = troopData.maxHealth * hpMul;
                attackPoints = troopData.attack * atkMul;
                moveSpeed = troopData.moveSpeed * moveMul;
                
                attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval);

                Debug.Log($"[Unit Enemy] {name} spawned (Stage {stageLevel}). HP: {MaxHealth}, Atk: {attackPoints}");
            }
        }

        currentHealth = MaxHealth;

        unitCollider = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();

        OnHealthChanged?.Invoke();
    }    
    public void InitStatsFromInstance(TroopInstance instance)
    {
        if (instance == null) return;

        MaxHealth = instance.currentHealth; // or instance.maxHealth if you store it
        currentHealth = MaxHealth;
        attackPoints = instance.currentAttack;
        moveSpeed = instance.currentMoveSpeed;
        statsInitialized = true;
        OnHealthChanged?.Invoke();
    }
    public void SetHealth(float value)
    {
        currentHealth = Mathf.Clamp(value, 0, MaxHealth);
    }


    protected void SetupFriendlyCollisionIgnore()
    {
        Collider2D[] myColliders = GetComponents<Collider2D>();
        if (myColliders.Length == 0) return;

        Unit[] allUnits = FindObjectsOfType<Unit>();

        foreach (Unit otherUnit in allUnits)
        {
            if (otherUnit == this) continue;

            Collider2D[] otherColliders = otherUnit.GetComponents<Collider2D>();

            foreach (Collider2D myCol in myColliders)
            {
                foreach (Collider2D otherCol in otherColliders)
                {
                    if (myCol != null &&
                        otherCol != null &&
                        !myCol.isTrigger &&
                        !otherCol.isTrigger)
                    {
                        Physics2D.IgnoreCollision(myCol, otherCol, true);
                    }
                }
            }
        }
    }

    protected virtual void Update()
    {
        if (isDead) return;

        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (isAttacking)
        {
            if (rb != null) rb.velocity = Vector2.zero;
            HandleAttack();
        }
        else
        {
            Move();
        }
    }

    protected abstract void Move();
    protected abstract void PerformAttack(Collider2D target);
    protected abstract void FindAndPerformAttack();

    protected void HandleAttack()
    {
        SetAnimationState(false, true);

        attackCooldown += Time.deltaTime;
        if (attackCooldown >= 1f / attackSpeed)
        {
            attackCooldown = 0f;
            FindAndPerformAttack();
        }
    }

    public void TakeDamage(float dmg, bool isCrit = false, bool showPopup = true)
    {
        if (isDead) return;

        currentHealth -= dmg;
        OnHealthChanged?.Invoke();

        if (showPopup && DamagePopupSpawner.Instance != null)
        {
            Vector3 pos = transform.position + Vector3.up;

            // offset utama (menjauh dari badan)
            float xOffset = (UnitTeam == Team.Enemy) ? 0.6f : -0.6f;

            // random kecil (biar tidak numpuk)
            float randX = UnityEngine.Random.Range(-0.1f, 0.1f);
            float randY = UnityEngine.Random.Range(0.0f, 0.15f);

            pos += new Vector3(xOffset + randX, randY + 0.8f, 0f);

            DamagePopupSpawner.Instance.Spawn(dmg, isCrit, pos);
        }

        if (currentHealth <= 0) Die();
    }

    public virtual void Die()
    {
        if (isDead) return;
        isDead = true;
        isAttacking = false;
        AudioManager.Instance?.PlaySFX(AudioManager.Instance.troopDeathSFX);

        if (UnitTeam == Team.Enemy)
        {
            CoinManager.Instance?.AddPlayerCoins(20);
        }
        else if (UnitTeam == Team.Player)
        {
            CoinManager.Instance?.AddEnemyCoins(20);
        }

        SetAnimationState(false, false, true);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        if (unitCollider != null)
            unitCollider.enabled = false;

        yield return new WaitForSecondsRealtime(deathDuration);
        Destroy(gameObject);
    }

    protected void SetAnimationState(bool isMoving, bool isAttacking, bool isDeadTrigger = false)
    {
        if (animator == null) return;

        animator.SetBool("isMoving", isMoving);
        animator.SetBool("isAttacking", isAttacking);

        if (isDeadTrigger)
            animator.SetTrigger("isDead");
    }

    protected void SetTeam(Team team) => UnitTeam = team;

    public void SetTroopData(TroopData data)
    {
        troopData = data;
    }
    
    protected float CalculateDamage(float baseDamage, out bool isCrit)
    {
        // Random value 0.0 sampai 1.0
        isCrit = UnityEngine.Random.value <= critRate;
        
        // Gunakan float, jangan int
        float finalDamage = baseDamage;

        if (isCrit)
        {
            // Jangan pakai Mathf.RoundToInt agar desimal tetap ada
            finalDamage = baseDamage * critDamage;
            // Debug.Log($"🔥 CRITICAL HIT by {name}!");
        }

        return finalDamage;
    }
}
