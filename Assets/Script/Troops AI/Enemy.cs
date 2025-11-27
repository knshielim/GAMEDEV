using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Enemy : Unit
{
    private Unit currentTarget;
    private List<Unit> targetsInRange = new List<Unit>();
    private Tower targetTower;

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
    }

    protected override void Move()
    {
        // Remove dead targets
        targetsInRange.RemoveAll(t => t == null || t.isDead);

        // PRIORITY 1: Attack enemy units first (must clear enemies before attacking tower)
        if (targetsInRange.Count > 0)
        {
            // Pick closest target
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget != null)
            {
                float distance = Vector2.Distance(transform.position, currentTarget.transform.position);

                if (distance <= 0.5f) // attack range
                {
                    isAttacking = true;
                    rb.velocity = Vector2.zero;
                    SetAnimationState(false, true);
                }
                else
                {
                    // Move toward target
                    Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
                    transform.Translate(dir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false);
                    isAttacking = false;
                }
            }

            return;
        }

        // PRIORITY 2: If no enemy units, then move toward/attack tower
        if (targetTower != null)
        {
            float towerDistance = Vector2.Distance(transform.position, targetTower.transform.position);

            if (towerDistance <= 0.5f)
            {
                isAttacking = true;
                if (rb != null)
                    rb.velocity = Vector2.zero;
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

        // PRIORITY 3: No targets at all, move left
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

    protected override void FindAndPerformAttack()
    {
        if (currentTarget == null || currentTarget.isDead)
        {
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget == null)
            {
                isAttacking = false;
                SetAnimationState(true, false);
                return;
            }
        }

        if (currentTarget != null)
        {
            PerformAttack(currentTarget.GetComponent<Collider2D>());
            return;
        }

        if (targetTower != null)
        {
            PerformAttackOnTower();
        }
    }

    protected override void PerformAttack(Collider2D targetCollider)
    {
        if (targetCollider == null) return;

        Unit targetUnit = targetCollider.GetComponent<Unit>();
        if (targetUnit != null && !targetUnit.isDead)
        {
            targetUnit.TakeDamage(attackPoints);
            Debug.Log($"[ATTACK] {gameObject.name} dealt {attackPoints} damage to {targetUnit.gameObject.name} " +
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
        if (targetTower == null) return;

        targetTower.TakeDamage((int)attackPoints);
        Debug.Log($"[ATTACK] {gameObject.name} dealt {attackPoints} damage to {targetTower.owner} tower " +
                  $"(HP: {targetTower.currentHealth}/{targetTower.maxHealth})");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Unit target = other.GetComponent<Unit>();
        if (target != null && target.UnitTeam != UnitTeam && !target.isDead)
        {
            if (!targetsInRange.Contains(target))
            {
                targetsInRange.Add(target);
                Debug.Log($"[Enemy] {gameObject.name} detected enemy {target.gameObject.name} in range. Total enemies: {targetsInRange.Count}");
            }
            return;
        }

        Tower tower = other.GetComponent<Tower>();
        if (tower != null && tower.owner == Tower.TowerOwner.Player)
        {
            targetTower = tower;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
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
}
