using UnityEngine;
using System;
using System.Collections;

public enum Team { Player, Enemy }

public abstract class Unit : MonoBehaviour
{
    public event Action OnHealthChanged;

    // --- STAT SCALING TABLES ---
    // index rarity: (int)TroopRarity -> 0..4
    protected static readonly float[] RarityHpMult   = { 1.0f, 1.3f, 1.7f, 2.2f, 3.0f };
    protected static readonly float[] RarityAtkMult  = { 1.0f, 1.4f, 1.9f, 2.5f, 3.3f };
    protected static readonly float[] RaritySpdMult  = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f };

    // level 1–5 → index 0–4
    protected static readonly float[] LevelMult      = { 1.0f, 1.2f, 1.45f, 1.75f, 2.1f };

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
    [HideInInspector] public float currentHealth;
    [HideInInspector] public bool isDead = false;
    public float CurrentHealth => currentHealth;

    public Team UnitTeam { get; protected set; }

    protected float attackCooldown = 0f;
    protected bool isAttacking = false;

    [Header("Components")]
    public Animator animator;
    private Collider2D unitCollider;
    protected Rigidbody2D rb;

    protected virtual void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        // Debug.Log($"[UNIT-DEBUG] {name} Team:{UnitTeam} troopData:{troopData}", this);

        // Debug.Log($"[UNIT-DEBUG] {name} troopData = {troopData}", this);
        MaxHealth = baseMaxHealth;

        if (troopData != null)
        {
            // 1. take base stats from TroopData
            float baseHp    = troopData.maxHealth;
            float baseAtk   = troopData.attack;
            float baseMove  = troopData.moveSpeed;

            // 2. calculate the rarity index & level
            int rarityIndex = (int)troopData.rarity;
            int levelIndex;
            if (GameManager.Instance != null)
            {
                int stageLevel = (int)GameManager.Instance.currentLevel; // 1–5
                levelIndex = Mathf.Clamp(stageLevel - 1, 0, 4);          // 0–4
            }
            else
            {
                // fallback
                levelIndex = Mathf.Clamp(troopData.level - 1, 0, 4);
            }

            // 3. take the multiplier
            float hpMul   = RarityHpMult[rarityIndex]  * LevelMult[levelIndex];
            float atkMul  = RarityAtkMult[rarityIndex] * LevelMult[levelIndex];
            float moveMul = RaritySpdMult[rarityIndex] * LevelMult[levelIndex];

            // 4. apply to runtime stat
            MaxHealth    = baseHp  * hpMul;
            moveSpeed    = baseMove * moveMul;
            attackPoints = baseAtk * atkMul;

            // Attack Speed ​​still uses Attack Interval
            attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval);

            //Debug.Log($"[UNIT] {troopData.displayName} R:{troopData.rarity} L:{troopData.level} | " +
            //          $"HP:{MaxHealth} ATK:{attackPoints} MS:{moveSpeed}");
        }
        else
        {
            moveSpeed = baseMoveSpeed;
            attackSpeed = baseAttackSpeed;
            attackPoints = baseAttackPoints;
            Debug.Log($"[UNIT] No TroopData on {name}, using base stats.");
        }

        currentHealth = MaxHealth;

        unitCollider = GetComponent<Collider2D>();
        if (animator == null)
            animator = GetComponent<Animator>();

        OnHealthChanged?.Invoke();
    }

    /// <summary>
    /// Makes this unit ignore physical collisions with ALL other units (both friendly and enemy),
    /// allowing units to overlap while still detecting enemies via triggers for combat.
    /// Call this AFTER UnitTeam is set (e.g., in derived Start() methods).
    /// </summary>
    protected void SetupFriendlyCollisionIgnore()
    {
        // Get all colliders on this unit
        Collider2D[] myColliders = GetComponents<Collider2D>();
        if (myColliders.Length == 0) return;

        // Find all other units in the scene
        Unit[] allUnits = FindObjectsOfType<Unit>();
        
        foreach (Unit otherUnit in allUnits)
        {
            // Skip self
            if (otherUnit == this) continue;
            
            // Ignore physical collisions with ALL other units (friendly AND enemy)
            // This prevents units from pushing each other apart
            // Trigger colliders are kept active so OnTriggerEnter2D still works for combat detection
            Collider2D[] otherColliders = otherUnit.GetComponents<Collider2D>();
            
            foreach (Collider2D myCol in myColliders)
            {
                foreach (Collider2D otherCol in otherColliders)
                {
                    // Only ignore physical collisions (non-trigger colliders)
                    // Triggers are kept active so OnTriggerEnter2D still works for enemy detection
                    if (myCol != null && otherCol != null && !myCol.isTrigger && !otherCol.isTrigger)
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
            if (rb != null)
                rb.velocity = Vector2.zero;

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

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        OnHealthChanged?.Invoke();

        if (currentHealth <= 0)
            Die();
    }

    protected void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;
        Debug.Log($"[DEATH] {gameObject.name} died. Team:{UnitTeam}");

        Debug.Log($"[DEATH] {gameObject.name} starting death sequence");

        if (UnitTeam == Team.Enemy)
        {
            int coinsEarned = 20;
            CoinManager.Instance?.AddPlayerCoins(coinsEarned);
            Debug.Log($"[COIN] Player earned {coinsEarned} from killing {gameObject.name}");
        }
        else if (UnitTeam == Team.Player)
        {
            int coinsEarned = 20;
            CoinManager.Instance?.AddEnemyCoins(coinsEarned);
            Debug.Log($"[COIN] Enemy earned {coinsEarned} from killing {gameObject.name}");
        }

        SetAnimationState(false, false, true);
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log($"[DEATH] {gameObject.name} coroutine started");

        if (unitCollider != null)
        {
            unitCollider.enabled = false;
            Debug.Log($"[DEATH] {gameObject.name} collider disabled");
        }

        Debug.Log($"[DEATH] {gameObject.name} playing death animation");
        yield return new WaitForSeconds(deathDuration);

        Debug.Log($"[DEATH] {gameObject.name} destroying now");
        Destroy(gameObject);
    }

    protected void SetAnimationState(bool isMoving, bool isAttacking, bool isDead = false)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
            animator.SetBool("isAttacking", isAttacking);
            if (isDead)
                animator.SetTrigger("isDead");
        }
    }

    protected void SetTeam(Team team) => UnitTeam = team;
    public void SetTroopData(TroopData data)
    {
        troopData = data;
    }
}
