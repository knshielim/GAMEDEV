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
    [SerializeField] private float towerRangedDistance = 3f;

    [SerializeField] private float towerStopDistance = 4f;
    [SerializeField] private bool moveRight = false; // Enemy walk to the left
    public float baseAttackRange;
    
    public static List<Enemy> aliveEnemies = new List<Enemy>();

    [Header("Gem Reward Settings")]
    [Tooltip("Percentage chance to drop a gem (0-100).")]
    [SerializeField] private float gemDropChance = 100f; 

    [Tooltip("Amount of gems dropped on success.")]
    [SerializeField] private int gemDropAmount = 1;

    public bool forceGemDrop100 = false;

    protected override void Start()
    {
        UnitTeam = Team.Enemy;
        base.Start();

        if (TryGetComponent<SpriteRenderer>(out var sr))
            sr.flipX = true;

        if (troopData != null)
        {
        attackRange = troopData.attackRange;
        useProjectile = troopData.isRanged;
        projectilePrefab = troopData.projectilePrefab;
        projectileSpeed = troopData.projectileSpeed;
        projectileLifetime = troopData.projectileLifetime;;
        }
        baseAttackRange = attackRange;

        SetupFriendlyCollisionIgnore();

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
        Debug.Log($"[Enemy START] {name} isRanged from TroopData = {troopData.isRanged}, useProjectile = {useProjectile}");
        aliveEnemies.Add(this);
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
                    // Allow movement in both X and Y directions to reach targets
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
            if (targetTower == null)
                Debug.Log($"{name} RANGED but NO TOWER!");
            
            // RANGED TROOPS (useProjectile)
            if (useProjectile)
            {
                float distX = Mathf.Abs(transform.position.x - targetTower.transform.position.x);

                // too far from the shooting range → move forward
                if (distX > towerRangedDistance + 0.1f)
                {
                    float dirSign = moveRight ? 1f : -1f; // player ke kanan, enemy ke kiri
                    Vector2 moveDir = new Vector2(dirSign, 0f);
                    transform.Translate(moveDir * moveSpeed * Time.deltaTime);

                    SetAnimationState(true, false); // jalan
                    isAttacking = false;
                    return;
                }

                // Already within shooting range → stay & shoot
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true); // attack anim
                Debug.Log($"[RANGED ATTACK] {name} STOP distX={distX}, towerRangedDistance={towerRangedDistance}");
                return;
            }

            // MELEE TROOPS (default)
            float dirStop = moveRight ? -1f : 1f;
            float targetX = targetTower.transform.position.x + dirStop * towerStopDistance;

            Vector2 stopPos = new Vector2(targetX, transform.position.y);
            float stopDistX = Mathf.Abs(transform.position.x - stopPos.x);

            if (stopDistX <= 0.05f)
            {
                // Already in front of the tower → attack
                isAttacking = true;
                if (rb != null) rb.velocity = Vector2.zero;
                SetAnimationState(false, true);
                return;
            }
            else
            {
                // Walk to the front of the tower
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
                SetAnimationState(false, true);
                return;
            }
            else
            {
                Vector2 dirToTower = (targetTower.transform.position - transform.position).normalized;
                dirToTower.y = 0f;
                dirToTower = dirToTower.normalized;
                transform.Translate(dirToTower * moveSpeed * Time.deltaTime);
                SetAnimationState(true, false);
                isAttacking = false;
                return;
            }
        }*/

        // PRIORITY 3: Move left if no targets
        transform.Translate(Vector2.left * moveSpeed * Time.deltaTime);
        SetAnimationState(true, false);
        isAttacking = false;
    }

// This method should be called from Animation Event
public void FireProjectile()
{
    if (isDead || !useProjectile) return;
    
    if (currentTarget != null)
    {
        ShootProjectileAtTarget(currentTarget);
    }
    else if (targetTower != null)
    {
        // Shoot left towards tower
        ShootProjectileInDirection(Vector2.left);
    }
}

private void ShootProjectileAtTarget(Unit target)
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

            // Minimum distance to be allowed to hit the tower
            float neededDist = useProjectile
                ? towerRangedDistance + 0.2f // for ranged troops
                : towerStopDistance + 0.2f;  // for melee troops

            if (distX <= neededDist)
            {
                PerformAttackOnTower();
                return;
            }
        }

        // No valid target: go back to moving / idle
        isAttacking = false;
        SetAnimationState(true, false);
    }

    protected override void PerformAttack(Collider2D targetCollider)
    {
        // 1. Cek validitas
        if (isDead || targetCollider == null) return;

        Unit targetUnit = targetCollider.GetComponent<Unit>();
        
        // Pastikan target ada dan belum mati
        if (targetUnit != null && !targetUnit.isDead)
        {
            // 2. Cek Tipe Serangan: Pukul Langsung atau Tembak?
            if (useProjectile)
            {
                // Kalau Ranged/Pemanah: Tembak peluru
                ShootProjectile(targetUnit);
            }
            else
            {
                // Kalau Melee/Pukul Dekat: Hitung damage & pukul
                
                // Ambil damage dari fungsi Unit.cs (sudah termasuk Critical)
                int finalDamage = CalculateDamage((int)attackPoints); 
                
                // Deal Damage ke Troops player
                targetUnit.TakeDamage(finalDamage);

                // Debug Log biar enak ngeceknya
                /*
                Debug.Log(
                    $"[ENEMY ATTACK] {name} dealt {finalDamage} damage to {targetUnit.name} " +
                    $"(HP: {targetUnit.CurrentHealth}/{targetUnit.MaxHealth})"
                );
                */
            }

            // 3. Reset jika target mati
            if (targetUnit.CurrentHealth <= 0)
            {
                // currentTarget = null; // Aktifkan jika kamu punya variabel ini
                // isAttacking = false;  // Aktifkan jika logic attack kamu butuh ini
                SetAnimationState(true, false); // Jalan lagi (Moving=true, Attacking=false)
            }
        }
    }

    private void ShootProjectile(Unit target)
    {
        if (projectilePrefab == null) 
        { 
            Debug.LogWarning($"[{name}] No projectile prefab assigned!"); 
            return; 
        }

        Vector3 spawnPos = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        GameObject proj = Instantiate(projectilePrefab, spawnPos, Quaternion.identity);
        
        Vector2 dir = (target.transform.position - spawnPos).normalized;
        Projectile projectile = proj.GetComponent<Projectile>();
        
        if (projectile != null)
            projectile.Initialize(dir, attackPoints, UnitTeam, projectileSpeed, projectileLifetime);
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

        // pastikan anim death tetap jalan walau Time.timeScale = 0
        if (animator != null)
            animator.updateMode = AnimatorUpdateMode.UnscaledTime;

        SetAnimationState(false, false, true);

        StopAllCoroutines();

        if (rb != null)
        {
            rb.velocity = Vector2.zero;
            rb.isKinematic = true;
            rb.gravityScale = 0;
        }

        // ✅ Enemy list yang benar
        aliveEnemies.Remove(this);

        foreach (Collider2D col in GetComponents<Collider2D>())
            col.enabled = false;

        // kalau kamu mau drop gem/coin saat mati, panggil di sini (opsional)
        HandleGemDrop();
        // AwardCoinsForKill();

        StartCoroutine(DestroyAfterDeathRealtime());
    }


    private void HandleGemDrop()
    {
        // 1. Cek GemManager
        if (GemManager.Instance == null)
        {
            Debug.LogWarning($"[GemDrop] FAIL: GemManager.Instance NULL on {name}");
            return;
        }

        // 2. Cek Boss & Roll Chance
        bool isBoss = (troopData != null && troopData.rarity == TroopRarity.Boss);
        float roll = Random.Range(0f, 100f);
        
        // 3. Cek Debug Force Drop
        bool forceDrop = false;
        if (GameDebugConfig.Instance != null && 
            GameDebugConfig.Instance.enableDebugging && 
            GameDebugConfig.Instance.forceGemDrop100)
        {
            forceDrop = true;
            Debug.Log($"[GemDrop] 🔧 FORCE DROP ACTIVE for {name}");
        }

        // 4. Logika Drop (Boss ATAU ForceDrop ATAU Roll Berhasil)
        if (isBoss || forceDrop || roll <= gemDropChance)
        {
            int amount = isBoss ? 50 : gemDropAmount; 
            GemManager.Instance.AddLevelGem(amount);
            
            Debug.Log($"[Enemy] 💎 Dropped {amount} Gems! (Boss:{isBoss}, Force:{forceDrop})");

            if (DamagePopupSpawner.Instance != null)
            {
                DamagePopupSpawner.Instance.Spawn(amount, true, transform.position + Vector3.up);
            }
        }
    } // <--- Pastikan ada kurung tutup ini sebelum DestroyAfterDeathRealtime

    private IEnumerator DestroyAfterDeathRealtime()
    {
        yield return null;

        float fallback = 0.8f;

        if (animator != null)
        {
            var clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips != null && clips.Length > 0 && clips[0].clip != null)
                fallback = clips[0].clip.length;
        }

        yield return new WaitForSecondsRealtime(fallback);
        Destroy(gameObject);
    }



    private void AwardCoinsForKill()
    {
        if (troopData == null)
        {
            Debug.LogWarning("[Enemy] Cannot award coins - no troop data found!");
            return;
        }

        int coinsEarned = 0;

        switch (troopData.rarity)
        {
            case TroopRarity.Common:
                coinsEarned = 2;
                break;
            case TroopRarity.Rare:
                coinsEarned = 5;
                break;
            case TroopRarity.Epic:
                coinsEarned = 10;
                break;
            case TroopRarity.Legendary:
                coinsEarned = 15;
                break;
            case TroopRarity.Mythic:
                coinsEarned = 20; // Bonus for mythic enemies
                break;
            case TroopRarity.Boss:
                coinsEarned = 50; // Massive reward for defeating boss
                break;
            default:
                coinsEarned = 1; // Fallback
                break;
        }

        if (CoinManager.Instance != null)
        {
            CoinManager.Instance.AddPlayerCoins(coinsEarned);
            Debug.Log($"[Enemy] 💰 ENEMY DEFEATED! {troopData.displayName} ({troopData.rarity}) → +{coinsEarned} coins awarded!");
            Debug.Log($"[Enemy] 🏆 Kill Reward: {coinsEarned} coins added to player total");
        }
        else
        {
            Debug.LogError("[Enemy] CoinManager not found - cannot award coins for enemy kill!");
        }
    }
    public void ApplyFogRangeReduction(float amount)
    {
        float newRange = Mathf.Max(0, baseAttackRange - amount);
        attackRange = newRange;

        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
            cc.radius = newRange;
    }

    public void RestoreRange()
    {
        attackRange = baseAttackRange;

        CircleCollider2D cc = GetComponent<CircleCollider2D>();
        if (cc != null)
            cc.radius = baseAttackRange;
    }

}
