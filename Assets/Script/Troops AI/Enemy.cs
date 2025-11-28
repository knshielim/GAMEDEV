using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : Unit
{
    private Unit currentTarget;
    private List<Unit> targetsInRange = new List<Unit>();
    [SerializeField] private Tower targetTower;

    [Header("Attack Settings")]
    [Tooltip("How far this troop can attack. Used for both melee and ranged units.")]
    [SerializeField] private float attackRange = 0.5f;

    [Header("Projectile (for ranged troops)")]
    [Tooltip("If true, this troop will use projectiles instead of direct melee hits.")]
    [SerializeField] private bool useProjectile = false;

    [Tooltip("Projectile prefab to spawn when attacking.")]
    [SerializeField] private GameObject projectilePrefab;

    [Tooltip("Optional spawn point for the projectile. If null, uses this transform position.")]
    [SerializeField] private Transform projectileSpawnPoint;

    [Tooltip("How fast the projectile travels.")]
    [SerializeField] private float projectileSpeed = 8f;

    [Tooltip("How long before the projectile is automatically destroyed.")]
    [SerializeField] private float projectileLifetime = 3f;

    protected override void Start()
    {
        base.Start();
        UnitTeam = Team.Enemy;

        if (TryGetComponent<SpriteRenderer>(out var sr))
            sr.flipX = true;

        if (troopData != null)
        {
            attackRange = troopData.attackRange;
            useProjectile = troopData.isRanged;
            projectilePrefab = troopData.projectilePrefab;
            projectileSpeed = troopData.projectileSpeed;
            projectileLifetime = troopData.projectileLifetime;
        }

        SetupFriendlyCollisionIgnore();

        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
        {
            cc.radius = attackRange;
            cc.isTrigger = true;
        }
    }

    public void SetTargetTower(Tower tower)
    {
        targetTower = tower;
        Debug.Log($"[Enemy] {name} got target tower: {(tower != null ? tower.name : "NULL")}");
    }

    protected override void Move()
    {
        if (isDead) return; // STOP if dead

        targetsInRange.RemoveAll(t => t == null || t.isDead);

        // PRIORITY 1: Attack enemy units
        if (targetsInRange.Count > 0)
        {
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget != null)
            {
                float distance = Vector2.Distance(transform.position, currentTarget.transform.position);
                if (distance <= attackRange)
                {
                    isAttacking = true;
                    if (rb != null) rb.velocity = Vector2.zero;
                    SetAnimationState(false, true); // attack anim
                }
                else
                {
                    Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
                    transform.Translate(dir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false); // walk anim
                    isAttacking = false;
                }
            }
            return;
        }

        // PRIORITY 2: Attack tower
        if (targetTower != null)
        {
            float towerDistance = Vector2.Distance(transform.position, targetTower.transform.position);
            if (towerDistance <= attackRange)
            {
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true);
                return;
            }
            else
            {
                Vector2 dirToTower = (targetTower.transform.position - transform.position).normalized;
                transform.Translate(dirToTower * moveSpeed * Time.deltaTime);
                SetAnimationState(true, false);
                isAttacking = false;
                return;
            }
        }

        // PRIORITY 3: Move left if no targets
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

    protected override void FindAndPerformAttack()
    {
        if (isDead) return;

        targetsInRange.RemoveAll(t => t == null || t.isDead);

        currentTarget = targetsInRange
            .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
            .FirstOrDefault();

        if (currentTarget != null)
        {
            PerformAttack(currentTarget.GetComponent<Collider2D>());
            return;
        }

        if (targetTower != null)
        {
            PerformAttackOnTower();
            return;
        }

        isAttacking = false;
        SetAnimationState(true, false);
    }

    protected override void PerformAttack(Collider2D targetCollider)
    {
        if (isDead || targetCollider == null) return;

        Unit targetUnit = targetCollider.GetComponent<Unit>();
        if (targetUnit != null && !targetUnit.isDead)
        {
            targetUnit.TakeDamage(attackPoints);
            Debug.Log($"[ATTACK] {name} dealt {attackPoints} damage to {targetUnit.name} " +
                      $"(HP: {targetUnit.CurrentHealth}/{targetUnit.MaxHealth})");

            if (targetUnit.CurrentHealth <= 0)
            {
                currentTarget = null;
                isAttacking = false;
                SetAnimationState(true, false);
            }
        }
    }

    private void PerformAttackOnTower()
    {
        if (isDead || targetTower == null) return;
        int damage = Mathf.RoundToInt(attackPoints);
        targetTower.TakeDamage(damage);
        Debug.Log($"[ATTACK] {name} hitting tower {targetTower.name}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        TowerHitbox towerHB = other.GetComponent<TowerHitbox>();
        if (towerHB != null && towerHB.tower != null)
        {
            Tower tower = towerHB.tower;
            bool isEnemyTower = (UnitTeam == Team.Enemy && tower.owner == Tower.TowerOwner.Player) ||
                                (UnitTeam == Team.Player && tower.owner == Tower.TowerOwner.Enemy);
            if (isEnemyTower)
            {
                targetTower = tower;
                Debug.Log($"[Enemy] {name} detected ENEMY tower: {tower.name}");
                PerformAttackOnTower();
            }
            return;
        }

        Unit target = other.GetComponent<Unit>();
        if (target != null && target.UnitTeam != UnitTeam && !target.isDead && !targetsInRange.Contains(target))
        {
            targetsInRange.Add(target);
            Debug.Log($"[Enemy] {name} detected enemy {target.name} in range. Total enemies: {targetsInRange.Count}");
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (isDead) return;

        Unit target = other.GetComponent<Unit>();
        if (target != null)
        {
            targetsInRange.Remove(target);
            if (currentTarget == target)
            {
                currentTarget = null;
                isAttacking = false;
            }
        }

        Tower tower = other.GetComponent<Tower>();
        if (tower != null && tower == targetTower)
        {
            targetTower = null;
            isAttacking = false;
        }
    }

    // ----------------- DEATH HANDLING -----------------
    public override void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;

        // Play death animation
        SetAnimationState(false, false, true); // 3rd param triggers death anim

        // Stop physics
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }

        // Disable all colliders
        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        // Destroy after animation finishes
        StartCoroutine(DestroyAfterDeath());
    }

    private IEnumerator DestroyAfterDeath()
    {
        if (animator != null)
        {
            float deathAnimLength = animator.GetCurrentAnimatorStateInfo(0).length;
            yield return new WaitForSeconds(deathAnimLength);
        }
        Destroy(gameObject);
    }
}
