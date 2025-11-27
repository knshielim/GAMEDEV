using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Simple AI spawner that deploys enemy units using enemy coins.
/// Attach this to an empty GameObject in the scene and assign the spawn point near the enemy tower.
/// </summary>
public class EnemyDeployManager : MonoBehaviour
{
    [Header("Tower Reference")]
    [SerializeField] private Tower playerTower; 

    [Header("Spawn Settings")]
    [Tooltip("Where enemy troops will appear (usually in front of the enemy tower).")]
    public Transform enemySpawnPoint;

    [Tooltip("List of enemy troop data the AI can spawn.")]
    public List<TroopData> availableEnemyTroops = new List<TroopData>();

    [Tooltip("Seconds between spawn attempts.")]
    public float spawnInterval = 3f;

    [Tooltip("Flat coin cost per spawned troop (simple version).")]
    public int troopCost = 50;

    private float _timer;

    private void Update()
    {
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
            return;

        if (enemySpawnPoint == null || CoinManager.Instance == null || availableEnemyTroops.Count == 0)
            return;

        _timer += Time.deltaTime;
        if (_timer < spawnInterval) return;

        _timer = 0f;
        TrySpawnEnemy();
    }

    private void TrySpawnEnemy()
    {
        if (!CoinManager.Instance.TrySpendEnemyCoins(troopCost))
        {
            // Not enough coins yet
            Debug.Log($"[EnemyDeploy] Not enough coins. Need {troopCost}, have {CoinManager.Instance.enemyCoins}");
            return;
        }

        // Pick a random troop data
        int index = Random.Range(0, availableEnemyTroops.Count);
        TroopData data = availableEnemyTroops[index];
        if (data == null || (data.enemyPrefab == null && data.playerPrefab == null))
        {
            Debug.LogWarning("[EnemyDeploy] Selected TroopData or prefabs are null.");
            return;
        }

        GameObject prefabToUse = data.enemyPrefab != null ? data.enemyPrefab : data.playerPrefab;
        GameObject enemyObj = Instantiate(prefabToUse, enemySpawnPoint.position, Quaternion.identity);
        Enemy enemyUnit = enemyObj.GetComponent<Enemy>();
        if (enemyUnit != null)
        {
            enemyUnit.SetTroopData(data);
            enemyUnit.SetTargetTower(playerTower);
            Debug.Log($"[EnemyDeploy] Successfully spawned {data.displayName}...");
        }
        else
        {
            Debug.LogWarning($"[EnemyDeploy] Spawned '{data.displayName}' but no Enemy component found.");
        }

        /*
        // Swap Troops script to Enemy script (since we use the same prefabs for both)
        Troops troopsScript = enemyObj.GetComponent<Troops>();
        if (troopsScript != null)
        {
            // Remove the Troops component
            Destroy(troopsScript);
            
            // Add the Enemy component
            Enemy enemyUnit = enemyObj.AddComponent<Enemy>();
            enemyUnit.SetTroopData(data);
            Debug.Log($"[EnemyDeploy] Successfully spawned {data.displayName} (converted from Troops to Enemy) at {enemySpawnPoint.position}. Remaining coins: {CoinManager.Instance.enemyCoins}");
        }
        else
        {
            // Check if it already has Enemy (shouldn't happen with current setup, but handle it)
            Enemy enemyUnit = enemyObj.GetComponent<Enemy>();
            if (enemyUnit != null)
            {
                enemyUnit.SetTroopData(data); 
                Debug.Log($"[EnemyDeploy] Successfully spawned {data.displayName} (already has Enemy script) at {enemySpawnPoint.position}. Remaining coins: {CoinManager.Instance.enemyCoins}");
            }
            else
            {
                Debug.LogWarning($"[EnemyDeploy] Spawned prefab '{data.displayName}' has neither Troops nor Enemy component!");
            }
        }
        */
    }
}


