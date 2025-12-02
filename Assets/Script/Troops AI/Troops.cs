using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class Troops : Unit
{
    private Unit currentTarget;
    private List<Unit> targetsInRange = new List<Unit>();
    [SerializeField] private Tower targetTower;

    [Header("Attack Settings")]
    [SerializeField] private float attackRange = 0.5f;

    [Header("Projectile (for ranged troops)")]
    [SerializeField] private bool useProjectile = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 3f;
    //[SerializeField] private float towerRangedDistance = 3f; // stopping distance from tower for ranged

    [SerializeField] private float towerStopDistance = 4f;
    [SerializeField] private bool moveRight = true; // Player = true, Enemy = false

    protected override void Start()
    {
        base.Start();
        UnitTeam = Team.Player;

        if (troopData != null)
        {
            useProjectile = troopData.isRanged;
            projectilePrefab = troopData.projectilePrefab;
            projectileSpeed = troopData.projectileSpeed;
            projectileLifetime = troopData.projectileLifetime;
        }

        SetupFriendlyCollisionIgnore();

        // Align Circle Collider with attack range
        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
        {
            cc.radius = attackRange;
            cc.isTrigger = true;
        }
    }

    protected override void Move()
    {
        if (isDead) return; // STOP moving if dead

        // Remove dead targets
        targetsInRange.RemoveAll(t => t == null || t.isDead);

        // PRIORITY 1: Attack enemy units first
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
                    
                    // Lock movement on the X axis only
                    dir.y = 0f;
                    dir = dir.normalized;
                    
                    transform.Translate(dir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false); // walk anim
                    isAttacking = false;
                }
            }
            return;
        }

        // PRIORITY 2: Attack tower if no enemies
        if (targetTower != null)
        {
            // Hit stop-position instead of center of tower
            float dir = moveRight ? -1f : 1f; 
            float targetX = targetTower.transform.position.x + dir * towerStopDistance;

            Vector2 stopPos = new Vector2(targetX, transform.position.y);
            float distX = Mathf.Abs(transform.position.x - stopPos.x);

            if (distX <= 0.05f) 
            {
                // Stop & attack tower
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true);
                return;
            }
            else
            {
                // Move to the stop position
                Vector2 moveDir = (stopPos - (Vector2)transform.position).normalized;
                transform.Translate(moveDir * moveSpeed * Time.deltaTime);

                SetAnimationState(true, false);
                isAttacking = false;
                return;
            }
        }


        /*
        if (targetTower != null)
        {
            float towerDistance = Vector2.Distance(transform.position, targetTower.transform.position);
            if (towerDistance <= attackRange)
            {
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true); // attack anim
                return;
            }
            else
            {
                Vector2 dirToTower = (targetTower.transform.position - transform.position).normalized;
                // Lock movement on the X axis only
                dirToTower.y = 0f;
                dirToTower = dirToTower.normalized;
                
                transform.Translate(dirToTower * moveSpeed * Time.deltaTime);
                SetAnimationState(true, false);
                isAttacking = false;
                return;
            }
        }*/

        // PRIORITY 3: Move forward if no target
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

    protected override void FindAndPerformAttack()
    {
        if (isDead) return; // Do nothing if dead

        targetsInRange.RemoveAll(t => t == null || t.isDead);

        if (targetsInRange.Count > 0)
        {
            currentTarget = targetsInRange
                .OrderBy(t => Vector2.Distance(transform.position, t.transform.position))
                .FirstOrDefault();
        }
        else
        {
            currentTarget = null;
        }

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
        if (isDead) return; // stop attacking if dead
        if (targetCollider == null) return;

        Unit targetUnit = targetCollider.GetComponent<Unit>();
        if (targetUnit != null && !targetUnit.isDead)
        {
            targetUnit.TakeDamage(attackPoints);

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
        if (isDead) return; // stop if dead
        if (targetTower == null) return;
        int damage = Mathf.RoundToInt(attackPoints);
        targetTower.TakeDamage(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        Unit target = other.GetComponent<Unit>();
        if (target != null && target.UnitTeam != UnitTeam && !target.isDead)
        {
            if (!targetsInRange.Contains(target))
            {
                targetsInRange.Add(target);
            }
        }

        Tower tower = other.GetComponent<Tower>();
        if (tower != null && tower.owner == Tower.TowerOwner.Enemy)
        {
            targetTower = tower;
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

// Add this method to call from Animation Event
public void FireProjectile()
{
    if (isDead || !useProjectile) return;
    
    if (currentTarget != null)
    {
        ShootProjectile(currentTarget);
    }
    else if (targetTower != null)
    {
        // For shooting at tower, create a fake direction
        Vector2 dir = (targetTower.transform.position - projectileSpawnPoint.position).normalized;
        ShootProjectileInDirection(dir);
    }
}

private void ShootProjectile(Unit target)
{
    if (projectilePrefab == null)
    {
        Debug.LogWarning($"[{name}] No projectile prefab assigned!");
        return;
    }

    Vector3 spawnPos = projectileSpawnPoint != null 
        ? projectileSpawnPoint.position 
        : transform.position;

    GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    
    Vector2 dir = (target.transform.position - spawnPos).normalized;
    
    Projectile projectile = proj.GetComponent<Projectile>();
    if (projectile != null)
    {
        projectile.Initialize(dir, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
    }
}

private void ShootProjectileInDirection(Vector2 direction)
{
    if (projectilePrefab == null) return;

    Vector3 spawnPos = projectileSpawnPoint != null 
        ? projectileSpawnPoint.position 
        : transform.position;

    GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
    
    Projectile projectile = proj.GetComponent<Projectile>();
    if (projectile != null)
    {
        projectile.Initialize(direction, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
    }
}

}

