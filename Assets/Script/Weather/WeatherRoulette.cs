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

    [Header("BGM Ducking (Roulette)")]
    [Range(0f, 1f)] public float bgmDuckMul = 0.35f;     // BGM jadi 35%
    [Range(0f, 1f)] public float ambientDuckMul = 0.7f;  // ambience jadi 70% (opsional)
    public float bgmReturnDelay = 0.1f;                  // tunggu dikit sebelum balik

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

        /*
        if (debugMode)
            StartCoroutine(ApplyDebugWeatherNextFrame());
        */
        
        StartCoroutine(StartRouletteSafetyCheck());
    }

    private IEnumerator StartRouletteSafetyCheck()
    {
        // Tunggu sebentar (0.5 detik real time) biar DialogManager siap dulu
        yield return new WaitForSecondsRealtime(0.5f);

        // Panggil EnableRoulette
        StartCoroutine(EnableRoulette());
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

        // ====== START DUCK (tanpa mengubah SFX roulette kamu) ======
        AudioManager am = AudioManager.Instance; // ✅ Store reference outside the if block
        AudioClip cachedWheel = null;

        if (am != null)
        {
            // StartRouletteAudio() normalnya PlaySFX(wheelSFX)
            // jadi kita "matikan" wheelSFX sementara supaya tidak dobel dengan PlayWheelTail()
            cachedWheel = am.wheelSFX;
            am.wheelSFX = null;

            am.StartRouletteAudio(bgmDuckMul, ambientDuckMul);

            // balikin lagi (biar sistem audio lain tetap normal)
            am.wheelSFX = cachedWheel;
        }
        // ====== END DUCK START ======

        AudioManager.Instance?.PlayWheelTail(spinDuration, spinSpeed);
        
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
            elapsed += Time.deltaTime;
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
        
        // ✅ FIX: Release pause and ensure game resumes
        GameManager.Instance?.ReleasePause("WeatherRoulette");
        
        // ✅ FIX: Explicitly resume game
        Time.timeScale = 1f;
        Debug.Log("[WeatherRoulette] ▶️ Game resumed - gameplay should start now");

        // ====== UNDUCK (smooth balik) ======
        if (am != null) // ✅ Now 'am' is accessible here
        {
            if (bgmReturnDelay > 0f)
                yield return new WaitForSecondsRealtime(bgmReturnDelay);

            am.EndRouletteAudio();
        }
        // ====== END UNDUCK ======

        isSpinning = false;
    }

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

}
