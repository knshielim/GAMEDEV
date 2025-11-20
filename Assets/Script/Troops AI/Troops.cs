using UnityEngine;

public class Troops : MonoBehaviour
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

    private Enemy targetEnemy;
    private Tower targetTower;

    void Start()
    {
        currentHealth = maxHealth;

        if (animator == null)
            animator = GetComponent<Animator>();
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
            if (targetEnemy != null)
                AttackEnemy();
            else if (targetTower != null)
                AttackTower();
            else
                isAttacking = false;
        }
        else
        {
            MoveForward();
        }
    }

    void MoveForward()
    {
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);

        if (animator != null)
        {
            animator.SetBool("isMoving", true);
            animator.SetBool("isAttacking", false);
        }
    }

    void AttackEnemy()
    {
        if (targetEnemy == null)
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
            targetEnemy.TakeDamage(attackPoints);

            if (targetEnemy.currentHealth <= 0)
                targetEnemy = null;
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

            if (targetTower.currentHealth <= 0)
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
        if (other.CompareTag("Enemy") && other.GetComponent<Enemy>() != null)
        {
            targetEnemy = other.GetComponent<Enemy>();
            isAttacking = true;
        }
        else if (other.CompareTag("Tower") && other.GetComponent<Tower>() != null)
        {
            targetTower = other.GetComponent<Tower>();
            isAttacking = true;
        }
    }
}
