using UnityEngine;

public class Enemy : MonoBehaviour
{
    [Header("Stats")]
    public float maxHealth = 20f;
    public float moveSpeed = 1.5f;
    public float attackSpeed = 1f; 
    public float attackPoints = 3f;

    [Header("Animation")]
    public Animator animator;

    public float currentHealth;
    private float attackCooldown = 0f;
    private bool isAttacking = false;

    private Troops targetTroop;
    private Tower targetTower;

    void Start()
    {
    currentHealth = maxHealth;

    if (animator == null)
        animator = GetComponent<Animator>();

    // flip the sprite to the left
    GetComponent<SpriteRenderer>().flipX = true;
    }


    void Update()
    {
        if (currentHealth <= 0)
        {
            Die();
            return;
        }

        if (isAttacking)
        {
            if (targetTroop != null)
                AttackTroop();
            else if (targetTower != null)
                AttackTower();
            else
                isAttacking = false; // no target
        }
        else
        {
            MoveLeft();
        }
    }

    void MoveLeft()
    {
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);

        // Animation
        if (animator != null)
        {
            animator.SetBool("isMoving", true);
            animator.SetBool("isAttacking", false);
        }
    }

    void AttackTroop()
    {
        if (targetTroop == null)
        {
            isAttacking = false;
            return;
        }

        attackCooldown += Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", true);
        }

        if (attackCooldown >= attackSpeed)
        {
            attackCooldown = 0f;
            targetTroop.TakeDamage(attackPoints);

            if (targetTroop.currentHealth <= 0) // Troop destroyed
                targetTroop = null;
        }
    }

    void AttackTower()
    {
        if (targetTower == null)
        {
            isAttacking = false;
            return;
        }

        attackCooldown += Time.deltaTime;

        if (animator != null)
        {
            animator.SetBool("isMoving", false);
            animator.SetBool("isAttacking", true);
        }

        if (attackCooldown >= attackSpeed)
        {
            attackCooldown = 0f;
            targetTower.TakeDamage(Mathf.CeilToInt(attackPoints));

            if (targetTower.currentHealth <= 0) // Tower destroyed
                targetTower = null;
        }
    }

    public void TakeDamage(float dmg)
    {
        currentHealth -= dmg;
        if (currentHealth <= 0)
            Die();
    }

    void Die()
    {
        if (animator != null)
            animator.SetTrigger("isDead");

        Destroy(gameObject, 0.5f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // Detect friendly Troops
        if (other.CompareTag("Troop") && other.GetComponent<Troops>() != null)
        {
            targetTroop = other.GetComponent<Troops>();
            isAttacking = true;
        }
        // Detect player's Tower
        else if (other.CompareTag("Tower") && other.GetComponent<Tower>() != null)
        {
            targetTower = other.GetComponent<Tower>();
            isAttacking = true;
        }
    }
}
