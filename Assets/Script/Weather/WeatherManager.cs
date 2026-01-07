using System.Collections;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Weather Settings")]
    public float acidRainDamagePerSecond = 5f;
    public float fogSlowPercentage = 0.3f; 
    public float activeWeatherTime; 
    public WeatherType CurrentWeather = WeatherType.Sunny;
    public float WeatherEndTime;
    [SerializeField] private float acidPopupInterval = 0.5f;


    // ================= ADDITION =================
    [Header("Weather VFX")]
    [SerializeField] private ParticleSystem acidRainParticles;
    // ============================================

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // Start a weather event
    public void StartWeather(WeatherType type, float duration)
    {
        StopAllCoroutines(); 

        // ================= ADDITION =================
        StopAllWeatherVFX();
        // ============================================

        CurrentWeather = type;
        AudioManager.Instance?.StartWeatherAmbience(type);
        activeWeatherTime = duration;

        switch (type)
        {
            case WeatherType.AcidRain:
                StartCoroutine(ApplyAcidRain(duration));
                break;
            case WeatherType.Fog:
                StartCoroutine(ApplyFog(duration));
                break;
            case WeatherType.Sunny:
                StartCoroutine(ClearWeatherAfter(duration));
                break;
        }
    }

    private IEnumerator ApplyAcidRain(float duration)
    {
        // === popup interval settings ===
        const float popupYOffset = 0.8f;    // biar popup muncul di atas unit

        float elapsed = 0f;
        float popupTimer = 0f;

        // Akumulasi damage per target supaya popup nggak spam tiap frame
        var troopAccum = new Dictionary<Troops, float>();
        var enemyAccum = new Dictionary<Enemy, float>();

        Debug.Log("Acid Rain STARTED");
        Debug.Log("All troops will take damage continously");

        // VFX
        if (acidRainParticles != null)
        {
            acidRainParticles.gameObject.SetActive(true);
            acidRainParticles.Play();
        }

        while (elapsed < duration)
        {
            float dt = Time.unscaledDeltaTime;
            float dmgThisFrame = acidRainDamagePerSecond * dt;

            // === Apply damage to troops ===
            foreach (var troop in Troops.aliveTroops.ToList())
            {
                if (troop == null || troop.isDead) continue;

                troop.TakeDamage(dmgThisFrame, showPopup: false);

                if (troopAccum.ContainsKey(troop))
                    troopAccum[troop] += dmgThisFrame;
                else
                    troopAccum.Add(troop, dmgThisFrame);
            }

            // === Apply damage to enemies ===
            foreach (var enemy in Enemy.aliveEnemies.ToList())
            {
                if (enemy == null || enemy.isDead) continue;

                enemy.TakeDamage(dmgThisFrame, showPopup: false);

                if (enemyAccum.ContainsKey(enemy))
                    enemyAccum[enemy] += dmgThisFrame;
                else
                    enemyAccum.Add(enemy, dmgThisFrame);
            }

            // === Spawn popups per interval ===
            popupTimer += dt;
            if (popupTimer >= acidPopupInterval)
            {
                popupTimer = 0f;

                // Troop popups
                foreach (var kvp in troopAccum.ToList())
                {
                    var troop = kvp.Key;
                    if (troop == null || troop.isDead) continue;

                    float total = kvp.Value;
                    if (total > 0.0001f)
                    {
                        Vector3 pos = troop.transform.position + Vector3.up * popupYOffset;
                        DamagePopupSpawner.Instance?.Spawn(total, false, pos);
                    }
                }

                // Enemy popups
                foreach (var kvp in enemyAccum.ToList())
                {
                    var enemy = kvp.Key;
                    if (enemy == null || enemy.isDead) continue;

                    float total = kvp.Value;
                    if (total > 0.0001f)
                    {
                        Vector3 pos = enemy.transform.position + Vector3.up * popupYOffset;
                        DamagePopupSpawner.Instance?.Spawn(total, false, pos);
                    }
                }

                troopAccum.Clear();
                enemyAccum.Clear();
            }

            elapsed += dt;
            yield return null;
        }

        // Optional: flush sisa akumulasi biar nggak “hilang” kalau durasi habis pas tengah interval
        foreach (var kvp in troopAccum)
        {
            var troop = kvp.Key;
            if (troop == null || troop.isDead) continue;

            float total = kvp.Value;
            if (total > 0.0001f)
                DamagePopupSpawner.Instance?.Spawn(total, false, troop.transform.position + Vector3.up * popupYOffset);
        }

        foreach (var kvp in enemyAccum)
        {
            var enemy = kvp.Key;
            if (enemy == null || enemy.isDead) continue;

            float total = kvp.Value;
            if (total > 0.0001f)
                DamagePopupSpawner.Instance?.Spawn(total, false, enemy.transform.position + Vector3.up * popupYOffset);
        }

        Debug.Log("Acid Rain ENDED");

        // Stop VFX
        if (acidRainParticles != null)
        {
            acidRainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            acidRainParticles.gameObject.SetActive(false);
        }

        CurrentWeather = WeatherType.Sunny;
        AudioManager.Instance?.StopWeatherAmbience();
    }

    /*
    private IEnumerator ApplyAcidRain(float duration)
    {
        float elapsed = 0f;

        Debug.Log("Acid Rain STARTED");
        Debug.Log("All troops will take damage continously"); 

        // ================= ADDITION =================
        if (acidRainParticles != null)
        {
            acidRainParticles.gameObject.SetActive(true);
            acidRainParticles.Play();
        }
        // ============================================

        while (elapsed < duration)
        {
            foreach (var troop in Troops.aliveTroops.ToList())
            {
                if (troop != null && !troop.isDead)
                {
                    float dmg = acidRainDamagePerSecond * Time.unscaledDeltaTime;
                    troop.TakeDamage(dmg);

                    Debug.Log($"☠ Acid rain dmg {dmg:F2} to {troop.name}");
                }
            }
            
            foreach (var enemy in Enemy.aliveEnemies.ToList())
            {
                if (enemy != null && !enemy.isDead)
                {
                    float dmg = acidRainDamagePerSecond * Time.unscaledDeltaTime;
                    enemy.TakeDamage(dmg);
                    Debug.Log($"☠ Acid rain dmg {dmg:F2} to ENEMY {enemy.name}");
                }
            }

            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        Debug.Log("Acid Rain ENDED");

        // ================= ADDITION =================
        if (acidRainParticles != null)
        {
            acidRainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            acidRainParticles.gameObject.SetActive(false);
        }
        // ============================================

        CurrentWeather = WeatherType.Sunny;
        AudioManager.Instance?.StopWeatherAmbience();
    }
    */

    public IEnumerator ApplyFog(float duration)
    {
        Debug.Log("Fog STARTED");
        Debug.Log("All battlefield troops will have range reduced");

        FogEffect.Instance?.FadeIn();

        foreach (Troops troop in Troops.aliveTroops.ToList())
        {
            if (troop == null || troop.isDead) continue;

            float newRange = Mathf.Max(0, troop.baseAttackRange - 1.5f);
            troop.attackRange = newRange;

            CircleCollider2D cc = troop.GetComponent<CircleCollider2D>();
            if (cc != null)
                cc.radius = newRange;
        }

        foreach (Enemy enemy in Enemy.aliveEnemies.ToList())
        {
            if (enemy == null || enemy.isDead) continue;
            enemy.ApplyFogRangeReduction(1.5f);
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {   
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        foreach (Troops troop in Troops.aliveTroops.ToList())
        {
            if (troop == null || troop.isDead) continue;

            troop.attackRange = troop.baseAttackRange;

            CircleCollider2D cc = troop.GetComponent<CircleCollider2D>();
            if (cc != null)
                cc.radius = troop.baseAttackRange;
        }

        foreach (Enemy enemy in Enemy.aliveEnemies.ToList())
        {
            if (enemy == null || enemy.isDead) continue;
            enemy.RestoreRange();
        }

        CurrentWeather = WeatherType.Sunny;
        AudioManager.Instance?.StopWeatherAmbience();
        FogEffect.Instance?.FadeOut();
        Debug.Log("Fog ENDED");
    }

    private IEnumerator ClearWeatherAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        CurrentWeather = WeatherType.Sunny;
        AudioManager.Instance?.StopWeatherAmbience();
    }

    // ================= ADDITION =================
    private void StopAllWeatherVFX()
    {
        if (acidRainParticles != null)
        {
            acidRainParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            acidRainParticles.gameObject.SetActive(false);
        }
    }
    // ============================================
}
