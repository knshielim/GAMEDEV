using System.Collections;
using UnityEngine;
using System.Linq;

public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    [Header("Weather Settings")]
    public float acidRainDamagePerSecond = 5f;
    public float fogSlowPercentage = 0.3f; 
    public float activeWeatherTime; 
    public WeatherType CurrentWeather = WeatherType.Sunny;
    public float WeatherEndTime;

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
        StopAllCoroutines(); // stop any previous weather
        CurrentWeather = type;
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
        float elapsed = 0f;

        Debug.Log("Acid Rain STARTED");
        Debug.Log("All troops will take damage continously"); 

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
    }


   public IEnumerator ApplyFog(float duration)
{
    Debug.Log("Fog STARTED");
    Debug.Log("All battlefield troops will have range reduced");

    FogEffect.Instance?.FadeIn();

    // Troops 
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

    // Restore range
    foreach (Troops troop in Troops.aliveTroops.ToList())
    {
        if (troop == null || troop.isDead) continue;

        troop.attackRange = troop.baseAttackRange;

        CircleCollider2D cc = troop.GetComponent<CircleCollider2D>();
        if (cc != null)
            cc.radius = troop.baseAttackRange;
    }

    // ---- RESTORE ENEMIES ----
    foreach (Enemy enemy in Enemy.aliveEnemies.ToList())
    {
        if (enemy == null || enemy.isDead) continue;
        enemy.RestoreRange();
    }

    CurrentWeather = WeatherType.Sunny;
    FogEffect.Instance?.FadeOut();
    Debug.Log("Fog ENDED");
    }


    private IEnumerator ClearWeatherAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        CurrentWeather = WeatherType.Sunny;
    }
}
