using System.Collections;
using System.Collections.Generic;  
using UnityEngine;



public class WeatherManager : MonoBehaviour
{
    public static WeatherManager Instance;

    public WeatherType CurrentWeather = WeatherType.Sunny;

    [Header("Weather Settings")]
    public float acidRainDamagePerSecond = 5f;
    public float fogSlowPercentage = 0.3f; // optional
    public float activeWeatherTime; // tracks remaining weather duration

    private void Awake()
    {
        Instance = this;
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

        while (elapsed < duration)
        {
            foreach (var troop in Troops.aliveTroops)
            {
                if (!troop.isDead)
                    troop.TakeDamage(acidRainDamagePerSecond * Time.deltaTime);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        CurrentWeather = WeatherType.Sunny;
    }

    public IEnumerator ApplyFog(float duration)
    {
        foreach (Troops troop in Troops.aliveTroops)
        {
            if (troop == null) continue;
            StartCoroutine(troop.ApplyFogTemporary(duration)); // reduces attackRange by 1
        }

        yield return new WaitForSeconds(duration);
        CurrentWeather = WeatherType.Sunny;
    }

    private IEnumerator ClearWeatherAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        CurrentWeather = WeatherType.Sunny;
    }
}
