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
    [SerializeField] public float attackRange = 0.5f;

    [Header("Projectile (for ranged troops)")]
    [SerializeField] private bool useProjectile = false;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private Transform projectileSpawnPoint;
    [SerializeField] private float projectileSpeed = 8f;
    [SerializeField] private float projectileLifetime = 3f;
    [SerializeField] private float towerRangedDistance = 3f;
    //[SerializeField] private float towerRangedDistance = 3f; // stopping distance from tower for ranged

    [SerializeField] private float towerStopDistance = 4f;
    [SerializeField] private bool moveRight = true; // Player = true, Enemy = false

    public static List<Troops> aliveTroops = new List<Troops>();

    // ← Fixed: add TroopInstance directly here
    public TroopInstance instance;
    public float baseAttackRange;
    public float baseHealth;
    public bool isUnderAcidRain = false;


    protected override void Start()
    {
    base.Start();
    aliveTroops.Add(this);

    baseAttackRange = attackRange;  


        if (instance != null)
        {
           InitStatsFromInstance(instance);
            useProjectile = instance.data.isRanged;
            projectilePrefab = instance.data.projectilePrefab;
            projectileSpeed = instance.data.projectileSpeed;
            projectileLifetime = instance.data.projectileLifetime;

            // Apply level-up stats
            currentHealth = instance.currentHealth;
            attackPoints = instance.currentAttack;
            moveSpeed = instance.currentMoveSpeed;
        }
        else if (troopData != null)
        {
            // fallback for base stats
            useProjectile = troopData.isRanged;
            projectilePrefab = troopData.projectilePrefab;
            projectileSpeed = troopData.projectileSpeed;
            projectileLifetime = troopData.projectileLifetime;

            currentHealth = troopData.maxHealth;
            attackPoints = troopData.attack;
            moveSpeed = troopData.moveSpeed;
        }
        baseHealth = currentHealth;
        UnitTeam = Team.Player;
        SetupFriendlyCollisionIgnore();

        // Align Circle Collider with attack range
        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
        {
            float oldRadius = cc.radius;
            cc.radius = attackRange;
            cc.isTrigger = true;
            Debug.Log($"[{name}] CircleCollider2D: old radius {oldRadius:F2} -> new radius {cc.radius:F2} (attackRange: {attackRange:F2})");
        }
        else
        {
            Debug.Log($"[{name}] CircleCollider2D not found!");
        }
        
    }

    public void SetTargetTower(Tower tower)
    {
        targetTower = tower;
        Debug.Log($"[Troop] {name} got target tower: {(tower != null ? tower.name : "NULL")}");
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

                    // Allow movement in both X and Y directions to reach targets
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
            // RANGED TROOPS
            if (useProjectile)
            {
                float distX = Mathf.Abs(transform.position.x - targetTower.transform.position.x);
                if (distX > towerRangedDistance + 0.1f)
                {
                    float dirSign = moveRight ? 1f : -1f;
                    Vector2 moveDir = new Vector2(dirSign, 0f);
                    transform.Translate(moveDir * moveSpeed * Time.deltaTime);
                    SetAnimationState(true, false);
                    isAttacking = false;
                    return;
                }

                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true);
                return;
            }

            // MELEE TROOPS
            float dirStop = moveRight ? -1f : 1f;
            float targetX = targetTower.transform.position.x + dirStop * towerStopDistance;
            Vector2 stopPos = new Vector2(targetX, transform.position.y);
            float stopDistX = Mathf.Abs(transform.position.x - stopPos.x);

            if (stopDistX <= 0.05f)
            {
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true);
                return;
            }
            else
            {
                Vector2 moveDir = (stopPos - (Vector2)transform.position).normalized;
                transform.Translate(moveDir * moveSpeed * Time.deltaTime);
                SetAnimationState(true, false);
                isAttacking = false;
                return;
            }
        }

        // PRIORITY 3: Move forward if no target
        transform.Translate(Vector2.right * moveSpeed * Time.deltaTime);
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
            float distX = Mathf.Abs(transform.position.x - targetTower.transform.position.x);
            float neededDist = useProjectile ? towerRangedDistance + 0.2f : towerStopDistance + 0.2f;
            if (distX <= neededDist)
            {
                PerformAttackOnTower();
                return;
            }
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
            Debug.Log($"From troops.cs: [ATTACK] {name} dealt {attackPoints} damage to {targetUnit.name} " +
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
        if (isDead) return;
        if (targetTower == null) return;
        int damage = Mathf.RoundToInt(attackPoints);
        targetTower.TakeDamage(damage);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isDead) return;

        TowerHitbox towerHB = other.GetComponent<TowerHitbox>();
        if (towerHB != null && towerHB.tower != null)
        {
            Tower tower = towerHB.tower;
            bool isEnemyTower =
                (UnitTeam == Team.Player && tower.owner == Tower.TowerOwner.Enemy) ||
                (UnitTeam == Team.Enemy && tower.owner == Tower.TowerOwner.Player);

            if (isEnemyTower)
            {
                targetTower = tower;
                Debug.Log($"[Troops] {name} detected ENEMY tower: {tower.name}");
            }
            return;
        }

        Unit target = other.GetComponent<Unit>();
        if (target != null && target.UnitTeam != UnitTeam && !target.isDead && !targetsInRange.Contains(target))
        {
            targetsInRange.Add(target);
            Debug.Log($"[Troops] {name} detected enemy unit {target.name}. Total enemies: {targetsInRange.Count}");
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

    public override void Die()
    {
        if (isDead) return;

        isDead = true;
        isAttacking = false;
        SetAnimationState(false, false, true);

        isUnderAcidRain = false;
        StopAllCoroutines();


        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }

        aliveTroops.Remove(this);

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

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

    public void FireProjectile()
    {
        if (isDead || !useProjectile) return;

        if (currentTarget != null)
            ShootProjectile(currentTarget);
        else if (targetTower != null)
        {
            Vector2 dir = (targetTower.transform.position - projectileSpawnPoint.position).normalized;
            ShootProjectileInDirection(dir);
        }
    }

    private void ShootProjectile(Unit target)
    {
        if (projectilePrefab == null) { Debug.LogWarning($"[{name}] No projectile prefab assigned!"); return; }

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Vector2 dir = (target.transform.position - spawnPos).normalized;
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Initialize(dir, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
    }

    private void ShootProjectileInDirection(Vector2 direction)
    {
        if (projectilePrefab == null) return;

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        Projectile projectile = proj.GetComponent<Projectile>();
        if (projectile != null)
            projectile.Initialize(direction, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
    }
    public IEnumerator ApplyFogTemporary(float duration)
    {
        attackRange = Mathf.Max(0.1f, attackRange - 1f);
        Debug.Log($"{name} range reduced to {attackRange} due to Fog");

        yield return new WaitForSeconds(duration);

        attackRange = baseAttackRange;
        Debug.Log($"{name} range restored to {attackRange}");
    }

    // Acid Rain deals damage over time
    public IEnumerator ApplyAcidRainTemporary(float damagePerSecond, float duration)
    {
        Debug.Log($"Starting AcidRain on {name}");
        float elapsed = 0f;
        while (elapsed < duration && !isDead)
        {
            TakeDamage(damagePerSecond * Time.unscaledDeltaTime);
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }


}
