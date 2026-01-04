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
    public float spinSpeed = 500f;

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
        float time = 0f;
        float currentSpeed = spinSpeed;

        while (time < spinDuration)
        {
            transform.Rotate(0, 0, currentSpeed * Time.unscaledDeltaTime);
            currentSpeed = Mathf.Lerp(spinSpeed, 0, time / spinDuration);
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        WeatherType selectedWeather = weathers[Random.Range(0, weathers.Length)];
        Debug.Log("Selected Weather: " + selectedWeather);
        WeatherManager.Instance.StartWeather(selectedWeather, weatherDuration);

        isSpinning = false;

        if (roulettePanel != null)
            roulettePanel.SetActive(false);

        GameManager.Instance?.ReleasePause("WeatherRoulette");
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
