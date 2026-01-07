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
    float spinSpeed = 2f;

    [Header("UI")]
    public GameObject roulettePanel;
    public GameObject stopPrompt;

    [Header("Debug/Test")]
    public bool debugMode = false;
    public WeatherType debugWeather = WeatherType.AcidRain;
    public static WeatherRoulette Instance { get; private set; }

    private bool isSpinning = false;
    private float weatherDuration = 300f; // 5 minutes
    public bool locked = true;
    private bool hasShownRouletteThisLevel = false; // ✅ NEW: Track if roulette was shown

    [Header("BGM Ducking (Roulette)")]
    [Range(0f, 1f)] public float bgmDuckMul = 0.35f;
    [Range(0f, 1f)] public float ambientDuckMul = 0.7f;
    public float bgmReturnDelay = 0.1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        debugMode = false;
        locked = true;
        hasShownRouletteThisLevel = false; // ✅ Initialize flag
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
        
        StartCoroutine(StartRouletteSafetyCheck());
    }

    private IEnumerator StartRouletteSafetyCheck()
    {
        // Wait a bit for DialogManager to be ready
        yield return new WaitForSecondsRealtime(0.5f);

        // Call EnableRoulette
        StartCoroutine(EnableRoulette());
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
        Debug.Log($"[WeatherRoulette] SpinWheel pressed | locked={locked} | isSpinning={isSpinning}");

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

        // Duck audio
        AudioManager am = AudioManager.Instance;
        AudioClip cachedWheel = null;

        if (am != null)
        {
            cachedWheel = am.wheelSFX;
            am.wheelSFX = null;
            am.StartRouletteAudio(bgmDuckMul, ambientDuckMul);
            am.wheelSFX = cachedWheel;
        }

        AudioManager.Instance?.PlayWheelTail(spinDuration, spinSpeed);
        
        // Decide result FIRST
        WeatherType selectedWeather = weathers[Random.Range(0, weathers.Length)];
        int index = System.Array.IndexOf(weathers, selectedWeather);

        // Slice math for top pointer 
        float segmentAngle = 360f / weathers.Length;

        float targetAngle =
            index * segmentAngle +
            segmentAngle / 2f +
            90f;

        // Rotation setup
        float startZ = transform.eulerAngles.z;
        float finalZ = startZ + (360f * 4) - targetAngle;

        float elapsed = 0f;

        // Smooth spin
        while (elapsed < spinDuration)
        {
            elapsed += Time.unscaledDeltaTime; 
            float t = elapsed / spinDuration;
            float eased = Mathf.SmoothStep(0f, 1f, t);

            float z = Mathf.Lerp(startZ, finalZ, eased);
            transform.eulerAngles = new Vector3(0, 0, z);

            yield return null;
        }

        // Snap exactly to target
        transform.eulerAngles = new Vector3(0, 0, finalZ);

        Debug.Log("🎯 Selected Weather: " + selectedWeather);
        WeatherManager.Instance.StartWeather(selectedWeather, weatherDuration);

        roulettePanel?.SetActive(false);
        
        GameManager.Instance?.ReleasePause("WeatherRoulette");
    
        Time.timeScale = 1f;
        Debug.Log("[WeatherRoulette] ▶️ Game resumed - gameplay should start now");

        // Unduck audio
        if (am != null)
        {
            if (bgmReturnDelay > 0f)
                yield return new WaitForSecondsRealtime(bgmReturnDelay);

            am.EndRouletteAudio();
        }

        isSpinning = false;
    }

    public IEnumerator EnableRoulette()
    {
        yield return new WaitForEndOfFrame();

        // ✅ NEW: Check if roulette was already shown this level
        if (hasShownRouletteThisLevel)
        {
            Debug.Log("[WeatherRoulette] ⏭️ Roulette already shown this level - skipping");
            yield break;
        }

        // ✅ NEW: Check if game is over (victory/defeat)
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("[WeatherRoulette] 🏁 Game is over - not showing roulette");
            yield break;
        }

        // Wait for dialogue to complete
        if (DialogueManager.Instance != null)
        {
            yield return new WaitUntil(() => !DialogueManager.Instance.ShouldGameBePaused());
        }

        // ✅ NEW: Double-check game isn't over after dialogue
        if (GameManager.Instance != null && GameManager.Instance.IsGameOver())
        {
            Debug.Log("[WeatherRoulette] 🏁 Game ended during dialogue - not showing roulette");
            yield break;
        }

        // Wait for tutorial to complete
        TutorialManager tm = FindObjectOfType<TutorialManager>();
        if (tm != null && tm.enabled && tm.tutorialActive)
        {
            yield break; 
        }

        // ✅ NEW: Mark that roulette has been shown
        hasShownRouletteThisLevel = true;
        
        // Pause Game
        GameManager.Instance?.RequestPause("WeatherRoulette");

        locked = false;

        if (roulettePanel != null)
        {
            roulettePanel.SetActive(true);
            roulettePanel.transform.SetAsLastSibling(); 
        }

        Debug.Log("[WeatherRoulette] ✅ Roulette enabled and shown!");
    }

    // ✅ NEW: Public method to reset the flag when level restarts
    public void ResetRouletteFlag()
    {
        hasShownRouletteThisLevel = false;
        locked = true;
        Debug.Log("[WeatherRoulette] 🔄 Roulette flag reset for new level");
    }
}



    /*
    public IEnumerator EnableRoulette()
    {
        // Tunggu sampai frame selesai (biar aman)
        yield return new WaitForEndOfFrame();


        // TUNGGU jika ada Dialog sedang aktif
        if (DialogueManager.Instance != null)
        {
            // Kita tunggu sampai Dialogue bilang "Game boleh jalan (tidak dipause)"
            // Logika: WaitUntil(TRUE) akan nunggu. Jadi WaitUntil(DialogueActive)
            yield return new WaitUntil(() => !DialogueManager.Instance.ShouldGameBePaused());
        }

        // TUNGGU jika Tutorial sedang aktif (Optional, buat jaga-jaga)
        TutorialManager tm = FindObjectOfType<TutorialManager>();
        if (tm != null && tm.enabled && tm.tutorialActive)
        {
             // Kalau tutorial aktif, jangan muncul dulu. Biar TutorialManager yg manggil nanti.
             yield break; 
        }

        
        // Pause Game
        GameManager.Instance?.RequestPause("WeatherRoulette");

        locked = false;

        if (roulettePanel != null)
        {
            roulettePanel.SetActive(true);
            // Pastikan panel muncul paling depan (di atas UI lain)
            roulettePanel.transform.SetAsLastSibling(); 
        }

        Debug.Log("[WeatherRoulette] ✅ Roulette enabled and shown!");
    }
    */

    /*
    public IEnumerator EnableRoulette()
    {
        yield return new WaitForEndOfFrame();
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
    */
