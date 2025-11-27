using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Troops : Unit
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
        UnitTeam = Team.Player;
        
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
            // Pick the closest target
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();

            if (currentTarget != null)
            {
                float distance = Vector2.Distance(transform.position, currentTarget.transform.position);

                if (distance <= attackRange)
                {
                    // Inside attack range → stop and attack
                    isAttacking = true;
                    if (rb != null)
                        rb.velocity = Vector2.zero;
                    SetAnimationState(false, true);
                    // Debug.Log($"[Troops] {gameObject.name} is attacking {currentTarget.gameObject.name} at distance {distance:F2} (range: {attackRange})");
                }
                else
                {
                    // Move toward target
                    Vector2 dir = (currentTarget.transform.position - transform.position).normalized;
                    rb.MovePosition(rb.position + dir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false);
                    isAttacking = false;
                    // Debug.Log($"[Troops] {gameObject.name} moving toward {currentTarget.gameObject.name} (distance: {distance:F2}, range: {attackRange})");
                }
            }

            return; // stop further movement
        }

        // PRIORITY 2: If no enemy units, then move toward/attack tower
        if (targetTower != null)
        {
            float towerDistance = Vector2.Distance(transform.position, targetTower.transform.position);

            if (towerDistance <= attackRange)
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

        // PRIORITY 3: No targets at all, move forward
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

    protected override void FindAndPerformAttack()
    {
        // Prefer unit targets
        if (currentTarget == null || currentTarget.isDead)
        {
            // Pick closest target again
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

        // If we still have a unit target, attack it
        if (currentTarget != null)
        {
            PerformAttack(currentTarget.GetComponent<Collider2D>());
            return;
        }

        // Otherwise, if a tower is in range, attack the tower
        if (targetTower != null)
        {
            PerformAttackOnTower();
        }
    }

    protected override void PerformAttack(Collider2D targetCollider)
    {
        if (targetCollider == null) return;

        // Melee attack: apply damage directly
        if (!useProjectile)
        {
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
        else
        {
            // Ranged attack: spawn a projectile towards the target
            if (projectilePrefab == null)
            {
                Debug.LogWarning($"[ATTACK] {gameObject.name} is set to use projectiles but has no projectile prefab assigned.");
                return;
            }

            Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
            Vector3 targetPos = targetCollider.transform.position;
            Vector2 direction = (targetPos - spawnPos).normalized;

            GameObject projObj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
            Projectile projectile = projObj.GetComponent<Projectile>();
            if (projectile != null)
            {
                projectile.Initialize(direction, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
            }
            else
            {
                Debug.LogWarning("[ATTACK] Spawned projectile has no Projectile component.");
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
        // First check for Unit targets (enemy troops)
        Unit target = other.GetComponent<Unit>();
        if (target != null && target.UnitTeam != UnitTeam && !target.isDead)
        {
            if (!targetsInRange.Contains(target))
            {
                targetsInRange.Add(target);
                Debug.Log($"[Troops] {gameObject.name} detected enemy {target.gameObject.name} in range. Total enemies: {targetsInRange.Count}");
            }
            return;
        }

        // Then check for enemy tower
        Tower tower = other.GetComponent<Tower>();
        if (tower != null && tower.owner == Tower.TowerOwner.Enemy)
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
