using UnityEngine;
using System.Collections;

public enum WeatherType
{
    Sunny,
    Fog,
    AcidRain
}

public class WeatherRoulette : MonoBehaviour
{
    [Header("Wheel Settings")]
    public WeatherType[] weathers;      // Array of possible weathers
    public float spinDuration = 3f;     // Duration of the spin
    public float spinSpeed = 500f;      // Starting spin speed

    [Header("UI")]
    public GameObject roulettePanel;
    public GameObject stopPrompt;

    [Header("Debug/Test")]
    public bool debugMode = true;                
    public WeatherType debugWeather = WeatherType.AcidRain;  // Weather to test

    private bool isSpinning = false;

    // Reference to the manager
    public WeatherManager weatherManager;

    private float weatherDuration = 90f; // 90 seconds for all weather

    private void Start()
    {
        if (WeatherManager.Instance != null)
            WeatherManager.Instance.StartWeather(WeatherType.Sunny, weatherDuration);

        if (debugMode && weatherManager != null)
        {
            // Apply debug weather after 1 frame to ensure all troops spawned
            StartCoroutine(ApplyDebugWeatherNextFrame());
        }
    }

    private IEnumerator ApplyDebugWeatherNextFrame()
    {
        yield return null; // wait 1 frame
        Debug.Log("Debug weather: " + debugWeather);
        weatherManager.StartWeather(debugWeather, weatherDuration);
        StartCoroutine(DebugStopPrompt());
    }

    private void Update()
    {
        // Optional hotkeys for testing
        if (Input.GetKeyDown(KeyCode.Alpha1)) weatherManager.StartWeather(WeatherType.Sunny, weatherDuration);
        if (Input.GetKeyDown(KeyCode.Alpha2)) weatherManager.StartWeather(WeatherType.Fog, weatherDuration);
        if (Input.GetKeyDown(KeyCode.Alpha3)) weatherManager.StartWeather(WeatherType.AcidRain, weatherDuration);
    }

    private IEnumerator DebugStopPrompt()
    {
        yield return new WaitForSeconds(weatherDuration);

        if (stopPrompt != null)
            stopPrompt.SetActive(true);

        yield return new WaitForSeconds(5f);

        if (stopPrompt != null)
            stopPrompt.SetActive(false);
    }

    // Only spins when player triggers
    public void SpinWheel()
    {
        if (!isSpinning)
            StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        isSpinning = true;
        float time = 0f;
        float currentSpeed = spinSpeed;

        while (time < spinDuration)
        {
            transform.Rotate(0, 0, currentSpeed * Time.deltaTime);
            currentSpeed = Mathf.Lerp(spinSpeed, 0, time / spinDuration);
            time += Time.deltaTime;
            yield return null;
        }

        // Pick weather randomly
        int selectedIndex = Random.Range(0, weathers.Length);
        WeatherType selectedWeather = weathers[selectedIndex];

        Debug.Log("Selected Weather: " + selectedWeather);

        if (weatherManager != null)
            weatherManager.StartWeather(selectedWeather, weatherDuration);

        isSpinning = false;

        // Hide the roulette panel after spin
        if (roulettePanel != null)
            roulettePanel.SetActive(false);
    }
}
