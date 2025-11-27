using UnityEngine;
using System;
using System.Collections;

public enum Team { Player, Enemy }

public abstract class Unit : MonoBehaviour
{
    public event Action OnHealthChanged;

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
        MaxHealth = baseMaxHealth;

        if (troopData != null)
        {
            MaxHealth = troopData.maxHealth;
            moveSpeed = troopData.moveSpeed;
            attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval); // interval → speed
            attackPoints = troopData.attack;
            Debug.Log($"[UNIT] Using TroopData {troopData.displayName} | HP:{MaxHealth} ATK:{attackPoints} MS:{moveSpeed}");
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
}
