using UnityEngine;
using System;

public enum Team { Player, Enemy }

public abstract class Unit : MonoBehaviour
{
    public event Action OnHealthChanged;

    [Header("Base Stats")]
    [SerializeField] private float baseMaxHealth = 20f;
    [SerializeField] private float deathDuration = 1.0f; 

    public float MaxHealth { get; set; }

    public float moveSpeed = 1.5f;
    public float attackSpeed = 1f;
    public float attackPoints = 3f;

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

        if (attackCooldown >= attackSpeed)
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
        if (unitCollider != null)
        {
            unitCollider.enabled = false; 
        }

        SetAnimationState(false, false, true);
        if (UnitTeam == Team.Enemy)
        {
        int coinsEarned = 20; // Troops kill enemy
        CoinManager.Instance?.AddPlayerCoins(coinsEarned);
        Debug.Log($"[COIN] Player earned {coinsEarned}");
        }
    else if (UnitTeam == Team.Player)
    {
        int coinsEarned = 20; // Enemy kills Troop
        CoinManager.Instance?.AddEnemyCoins(coinsEarned);
        Debug.Log($"[COIN] Enemy earned {coinsEarned}");
    }

    // Clean up health event listeners
    OnHealthChanged = null;

    // Destroy the game object after death animation duration
    Destroy(gameObject, deathDuration);
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
}