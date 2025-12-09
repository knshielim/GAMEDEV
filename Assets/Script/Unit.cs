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
    protected static readonly float[] RarityHpMult = { 1.0f, 1.3f, 1.7f, 2.2f, 3.0f };
    protected static readonly float[] RarityAtkMult = { 1.0f, 1.4f, 1.9f, 2.5f, 3.3f };
    protected static readonly float[] RaritySpdMult = { 1.0f, 1.1f, 1.2f, 1.3f, 1.4f };

    protected static readonly float[] LevelMult = { 1.0f, 1.2f, 1.45f, 1.75f, 2.1f };

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
    public bool isDead = false;
    public float CurrentHealth => currentHealth;

    public Team UnitTeam { get; protected set; }

    protected float attackCooldown = 0f;
    protected bool isAttacking = false;

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

        MaxHealth = baseMaxHealth;

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

        currentHealth = MaxHealth;

        unitCollider = GetComponent<Collider2D>();
        if (animator == null) animator = GetComponent<Animator>();

        OnHealthChanged?.Invoke();
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
            if (troopData != null)
            {
                if (troopData.isRanged)
                {
                    AudioManager.Instance?.PlaySFX(AudioManager.Instance.rangedAttackSFX);
                }
                else
                {
                    AudioManager.Instance?.PlaySFX(AudioManager.Instance.meleeAttackSFX);
                }
            }
            FindAndPerformAttack();
        }
    }

    public void TakeDamage(float dmg)
    {
        if (isDead) return;

        currentHealth -= dmg;
        OnHealthChanged?.Invoke();

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

        yield return new WaitForSeconds(deathDuration);
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
}
