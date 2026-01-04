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
    public WeatherType[] weathers;
    public float spinDuration = 3f;

    [Header("UI")]
    public GameObject roulettePanel;
    public GameObject stopPrompt;

    [Header("Debug/Test")]
    public bool debugMode = false;
    public WeatherType debugWeather = WeatherType.AcidRain;
    public static WeatherRoulette Instance { get; private set; }

    private bool isSpinning = false;
    private float weatherDuration = 90f;
    public bool locked = true;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        locked = true;
    }

    private void Start()
    {
        if (roulettePanel != null) {
        roulettePanel.SetActive(false); 

        locked = true;
        }
        
        if (WeatherManager.Instance == null)
        {
            Debug.LogError("❌ WeatherManager.Instance is NULL!");
            return;
        }

        // Start with sunny
        WeatherManager.Instance.StartWeather(WeatherType.Sunny, weatherDuration);

        if (debugMode)
            StartCoroutine(ApplyDebugWeatherNextFrame());
    }

    private IEnumerator ApplyDebugWeatherNextFrame()
    {
        yield return null;
        Debug.Log(" Debug Weather Applied: " + debugWeather);
        WeatherManager.Instance.StartWeather(debugWeather, weatherDuration);
    }

    private void Update()
    {
        if (WeatherManager.Instance == null) return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            WeatherManager.Instance.StartWeather(WeatherType.Sunny, weatherDuration);

        if (Input.GetKeyDown(KeyCode.Alpha2))
            WeatherManager.Instance.StartWeather(WeatherType.Fog, weatherDuration);

        if (Input.GetKeyDown(KeyCode.Alpha3))
            WeatherManager.Instance.StartWeather(WeatherType.AcidRain, weatherDuration);
    }

    public void SpinWheel()
    {
        if (locked)
        {
            Debug.Log("[WeatherRoulette] 🚫 Spin blocked (locked)");
            return;
        }

        if (!isSpinning)
            StartCoroutine(Spin());
    }

    private IEnumerator Spin()
    {
        isSpinning = true;

        // 1️⃣ Decide result FIRST
        WeatherType selectedWeather = weathers[Random.Range(0, weathers.Length)];
        int index = System.Array.IndexOf(weathers, selectedWeather);

        // 2️⃣ Slice math
        float segmentAngle = 360f / weathers.Length; // 120°
        float targetAngle = index * segmentAngle + segmentAngle / 2f;

        // 3️⃣ Rotation setup
        float startZ = transform.eulerAngles.z;
        float finalZ = startZ + (360f * 4) - targetAngle; // 4 spins

        float elapsed = 0f;

        // 4️⃣ Smooth spin
        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / spinDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            float z = Mathf.Lerp(startZ, finalZ, eased);
            transform.eulerAngles = new Vector3(0, 0, z);

            yield return null;
        }

        // 5️⃣ Snap exactly to target
        transform.eulerAngles = new Vector3(0, 0, finalZ);

        Debug.Log("🎯 Selected Weather: " + selectedWeather);
        WeatherManager.Instance.StartWeather(selectedWeather, weatherDuration);

        roulettePanel?.SetActive(false);
        GameManager.Instance?.ReleasePause("WeatherRoulette");

        isSpinning = false;
    }

    public IEnumerator EnableRoulette()
    {
        locked = false;
        GameManager.Instance?.RequestPause("WeatherRoulette");

        if (DialogueManager.Instance != null)
        {
            yield return new WaitUntil(() =>
                !DialogueManager.Instance.ShouldGameBePaused()
            );
        }

        if (roulettePanel != null)
            roulettePanel.SetActive(true);

        Debug.Log("[WeatherRoulette] ✅ Roulette enabled");
    }

}
