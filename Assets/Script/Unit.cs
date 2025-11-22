using UnityEngine;
using System;
using System.Collections;

public enum Team { Player, Enemy }

public abstract class Unit : MonoBehaviour
{
    public event Action OnHealthChanged;

    [Header("Config (Data Driven)")]
    [SerializeField] private TroopData troopData;


    [Header("Base Stats (fallback if troopData = null)")]
    [SerializeField] private float baseMaxHealth = 20f;
    [SerializeField] private float baseMoveSpeed = 1.5f;
    [SerializeField] private float baseAttackSpeed = 1f;     
    [SerializeField] private float baseAttackPoints = 3f;
    [SerializeField] private float deathDuration = 1.0f; 

    [Header("Runtime Stats")]
    [SerializeField] private float _maxHealth = 20f;

    public float MaxHealth 
    { 
        get => _maxHealth; 
        internal set => _maxHealth = value; 
    }
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

    protected virtual void Start()
    {
        MaxHealth = baseMaxHealth;
        
        if (troopData != null)
        {
            MaxHealth   = troopData.maxHealth;
            moveSpeed   = troopData.moveSpeed;
            attackSpeed = 1f / Mathf.Max(0.01f, troopData.attackInterval); // interval → speed
            attackPoints = troopData.attack;

            Debug.Log($"[UNIT] Using TroopData {troopData.displayName} | HP:{MaxHealth} ATK:{attackPoints} MS:{moveSpeed}");
        }
        else
        {
            // fallback and use base stats 
            moveSpeed   = baseMoveSpeed;
            attackSpeed = baseAttackSpeed;
            attackPoints = baseAttackPoints;

            Debug.Log($"[UNIT] No TroopData on {name}, using base stats.");
        }

        // MaxHealth = baseMaxHealth;
        currentHealth = MaxHealth;
        
        unitCollider = GetComponent<Collider2D>();
        
        if (animator == null)
            animator = GetComponent<Animator>();

        OnHealthChanged?.Invoke(); 
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
            HandleAttack();
        }
        else
        {
            Move();
        }
    }

    protected abstract void Move();

    protected abstract void PerformAttack(Collider2D target);

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
    
    protected abstract void FindAndPerformAttack();

    public void TakeDamage(float dmg)
    {
        if (isDead) return; 
        
        currentHealth -= dmg;
        OnHealthChanged?.Invoke();

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    protected void Die()
    {
        if (isDead) return; 
        
        isDead = true; 
        isAttacking = false;
        
        Debug.Log($"[DEATH] {gameObject.name} starting death sequence");
        
        // Award coins based on which team died
        if (UnitTeam == Team.Enemy)
        {
            int coinsEarned = 20; // Player killed enemy
            CoinManager.Instance?.AddPlayerCoins(coinsEarned);
            Debug.Log($"[COIN] Player earned {coinsEarned} from killing {gameObject.name}");
        }
        else if (UnitTeam == Team.Player)
        {
            int coinsEarned = 20; // Enemy killed player troop
            CoinManager.Instance?.AddEnemyCoins(coinsEarned);
            Debug.Log($"[COIN] Enemy earned {coinsEarned} from killing {gameObject.name}");
        }

        // Set animation to death state
        SetAnimationState(false, false, true);

        // Start death sequence - collider will be disabled there
        StartCoroutine(DeathSequence());
    }

    private IEnumerator DeathSequence()
    {
        Debug.Log($"[DEATH] {gameObject.name} coroutine started");
        
        // Disable collider IMMEDIATELY to prevent new attacks
        if (unitCollider != null)
        {
            unitCollider.enabled = false;
            Debug.Log($"[DEATH] {gameObject.name} collider disabled");
        }
        
        // Wait for death animation to complete
        Debug.Log($"[DEATH] {gameObject.name} playing death animation");
        yield return new WaitForSeconds(deathDuration);
        
        Debug.Log($"[DEATH] {gameObject.name} destroying now");
        
        // Destroy the game object
        Destroy(gameObject);
    }

    protected void SetAnimationState(bool isMoving, bool isAttacking, bool isDead = false)
    {
        if (animator != null)
        {
            animator.SetBool("isMoving", isMoving);
            animator.SetBool("isAttacking", isAttacking);
            if (isDead)
            {
                animator.SetTrigger("isDead");
            }
        }
    }
    protected void SetTeam(Team team)
    {
        UnitTeam = team;
    }
}
