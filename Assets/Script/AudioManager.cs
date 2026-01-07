using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Clips - MAIN MENU")]
    public AudioClip mainMenuIntroMusic;
    public AudioClip mainMenuLoopMusic;

    [Header("BGM Clips - GAMEPLAY")]
    public AudioClip gameplayIntroMusic;
    public AudioClip gameplayLoopMusic;

    [Header("Audio Sources")]
    public AudioSource musicSource;   // BGM
    public AudioSource sfxSource;     // one-shot SFX
    public AudioSource ambientSource; // loop ambience (weather)
    public AudioSource waveSource; // loop untuk wave (biar tidak tabrakan dengan weather)


    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("SFX Clips - Existing")]
    public AudioClip gameOverSFX;
    public AudioClip gameWinSFX;
    public AudioClip hitTowerSFX;
    public AudioClip meleeAttackSFX;
    public AudioClip rangedAttackSFX;
    public AudioClip summonSFX;
    public AudioClip troopDeathSFX;
    public AudioClip upgradeSFX; // upgrade umum
    public AudioClip mythicSFX;

    [Header("SFX Clips - New Additions")]
    public AudioClip buttonClickSFX;
    public AudioClip gemDropSFX;
    public AudioClip wheelSFX;          // roulette spin (one-shot)
    public AudioClip upgradeTroopSFX;   // upgrade troop (shop)

    [Header("Weather Ambience Loops")]
    public AudioClip fogAmbienceLoop;   // loop
    public AudioClip acidRainLoop;      // loop
    [Range(0f, 1f)]
    public float weatherAmbienceMul = 0.3f; // 30% (nanti kamu adjust)
    [Range(0f, 2f)]
    public float acidRainCutTailSeconds = 0f;
    [Range(0f, 1f)] 
    public float waveVolumeMul = 0.5f;

    private Coroutine acidRainLoopCo;

    [Header("Wave & Boss Audio")]
    public AudioClip bossSummonedSFX;   // one-shot saat boss spawn
    public AudioClip waveOngoingLoop;   // loop selama wave berlangsung


    

    [Header("Ducking Settings (Roulette)")]
    public float duckFadeDuration = 0.15f;

    [Header("Startup Behavior")]
    public bool autoPlayOnStart = true;

    public enum BGMMode { MainMenu, Gameplay }

    
    [Header("Fade Settings")]
    public float ambienceFadeDuration = 0.25f;

    private Coroutine introRoutine;
    private Coroutine duckRoutine;
    private Coroutine ambienceFadeRoutine;
    private IEnumerator FadeAudioSourceVolume(AudioSource src, float target, float duration)
    {
        if (src == null) yield break;

        float start = src.volume;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);
            src.volume = Mathf.Lerp(start, target, k);
            yield return null;
        }

        src.volume = target;
    }


    private BGMMode currentBgmMode = BGMMode.Gameplay;
    private WeatherType currentWeather = WeatherType.Sunny;

    // Duck multipliers (biar ApplyVolumeSettings aman)
    private float musicDuckMul = 1f;
    private float ambientDuckMul = 1f;

    // --- TAMBAHAN BARU: Deteksi Pindah Scene ---
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Level1 - Level5 (buildIndex 2 - 6)
        bool isGameplayLevel = scene.buildIndex >= 2 && scene.buildIndex <= 6;

        if (isGameplayLevel)
        {
            // Gameplay BGM, jangan restart kalau masih gameplay
            PlayBGM(BGMMode.Gameplay, restartIfSame: false);
        }
        else
        {
            // Semua selain level = MainMenu BGM
            PlayBGM(BGMMode.MainMenu, restartIfSame: false);
        }
    }



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadAudioSettings();
        ApplyVolumeSettings();

        if (autoPlayOnStart)
        {
            PlayBGM(BGMMode.MainMenu, restartIfSame: false);
        }
    }

    // ==========================
    // BGM (INTRO THEN LOOP)
    // ==========================
    public void PlayBGM(BGMMode mode, bool restartIfSame = false)
    {
        if (musicSource == null) return;

        if (!restartIfSame && currentBgmMode == mode && musicSource.isPlaying)
            return;

        currentBgmMode = mode;

        AudioClip intro = null;
        AudioClip loop = null;

        if (mode == BGMMode.MainMenu)
        {
            intro = mainMenuIntroMusic;
            loop = mainMenuLoopMusic;
        }
        else
        {
            intro = gameplayIntroMusic;
            loop = gameplayLoopMusic;
        }

        if (introRoutine != null) StopCoroutine(introRoutine);
        introRoutine = StartCoroutine(PlayIntroThenLoopRoutine(intro, loop));
    }

    private IEnumerator PlayIntroThenLoopRoutine(AudioClip intro, AudioClip loop)
    {
        if (musicSource == null) yield break;

        musicSource.Stop();
        musicSource.loop = false;

        if (intro != null)
        {
            musicSource.clip = intro;
            musicSource.Play();
            yield return new WaitForSecondsRealtime(intro.length);
        }

        if (loop != null)
        {
            musicSource.clip = loop;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // Backward-compatible
    public void PlayIntroThenLoop() => PlayBGM(BGMMode.Gameplay, restartIfSame: true);

    public void StopMusic()
    {
        if (introRoutine != null) StopCoroutine(introRoutine);
        if (musicSource != null) musicSource.Stop();
    }

    // ==========================
    // SFX (One Shot)
    // ==========================
    public void PlaySFX(AudioClip clip, float volume = 1f)
    {
        if (sfxSource == null)
        {
            Debug.LogWarning("SFX Source is NULL");
            return;
        }

        if (!sfxSource.enabled || !sfxSource.gameObject.activeInHierarchy)
        {
            Debug.LogWarning("SFX Source is disabled");
            return;
        }

        sfxSource.PlayOneShot(clip, volume);
    }


    public void PlayButtonClick() => PlaySFX(buttonClickSFX);
    public void PlayGemDrop() => PlaySFX(gemDropSFX);
    public void PlayUpgradeTroop() => PlaySFX(upgradeTroopSFX);

    private IEnumerator DuckRoutine(float targetMusicMul, float targetAmbientMul, float duration)
    {
        float startMusic = musicDuckMul;
        float startAmbient = ambientDuckMul;

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.Clamp01(t / duration);

            musicDuckMul = Mathf.Lerp(startMusic, targetMusicMul, k);
            ambientDuckMul = Mathf.Lerp(startAmbient, targetAmbientMul, k);

            ApplyVolumeSettings();
            yield return null;
        }

        musicDuckMul = targetMusicMul;
        ambientDuckMul = targetAmbientMul;
        ApplyVolumeSettings();
    }

    // ==========================
    // GAME OVER DRAMATIC DUCK (punya kamu, tetap)
    // ==========================
    public void PlayGameOverDramatic()
    {
        if (gameOverSFX == null)
        {
            Debug.LogWarning("[AudioManager] gameOverSFX is not assigned!");
            return;
        }
        StartCoroutine(GameOverDuckRoutine());
    }

    private IEnumerator GameOverDuckRoutine()
    {
        if (musicSource == null || sfxSource == null)
        {
            PlaySFX(gameOverSFX);
            yield break;
        }

        float originalMusicVol = musicSource.volume;
        float originalSfxVol = sfxSource.volume;

        float targetMusicVol = originalMusicVol * 0.35f;
        float targetSfxVol = Mathf.Min(1f, originalSfxVol * 1.15f);

        float duration = 0.5f;
        float t = 0f;

        PlaySFX(gameOverSFX);

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            musicSource.volume = Mathf.Lerp(originalMusicVol, targetMusicVol, lerp);
            sfxSource.volume = Mathf.Lerp(originalSfxVol, targetSfxVol, lerp);

            yield return null;
        }

        yield return new WaitForSecondsRealtime(gameOverSFX.length);

        t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float lerp = t / duration;

            musicSource.volume = Mathf.Lerp(targetMusicVol, originalMusicVol, lerp);
            sfxSource.volume = Mathf.Lerp(targetSfxVol, originalSfxVol, lerp);

            yield return null;
        }

        musicSource.volume = originalMusicVol;
        sfxSource.volume = originalSfxVol;
    }

    // ==========================
    // VOLUME CONTROLS
    // ==========================
    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        ApplyVolumeSettings();
        // SaveAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        ApplyVolumeSettings();
        // SaveAudioSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        ApplyVolumeSettings();
        // SaveAudioSettings();
    }

    private void OnDisable()
    {
        SaveAudioSettings();
        SceneManager.sceneLoaded -= OnSceneLoaded; // Jangan lupa unsubscribe event
    }

    private void ApplyVolumeSettings()
    {
        float baseMusic = masterVolume * musicVolume;
        float baseSfx = masterVolume * sfxVolume;

        if (musicSource != null) musicSource.volume = baseMusic * musicDuckMul;
        if (sfxSource != null) sfxSource.volume = baseSfx; // one-shot tidak ikut duck
        if (ambientSource != null) ambientSource.volume = baseSfx * ambientDuckMul;
    }

    // ==========================
    // SETTINGS PERSISTENCE
    // ==========================
    private void SaveAudioSettings()
    {
        if (PersistenceManager.Instance != null)
            PersistenceManager.Instance.SaveAudioSettings(masterVolume, musicVolume, sfxVolume);
    }

    private void LoadAudioSettings()
    {
        if (PersistenceManager.Instance != null)
        {
            masterVolume = PersistenceManager.Instance.GetMasterVolume();
            musicVolume = PersistenceManager.Instance.GetMusicVolume();
            sfxVolume = PersistenceManager.Instance.GetSFXVolume();
        }
        else
        {
            masterVolume = 1f;
            musicVolume = 0.7f;
            sfxVolume = 0.8f;
        }
    }

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;

    // Dipanggil RouletteManager saat spin mulai
    public void StartRouletteAudio(float musicDuckMul, float ambientDuckMul)
    {
        if (wheelSFX != null) PlaySFX(wheelSFX);

        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckRoutine(
            targetMusicMul: musicDuckMul,
            targetAmbientMul: ambientDuckMul,
            duration: duckFadeDuration
        ));
    }

    // Dipanggil RouletteManager saat spin selesai
    public void EndRouletteAudio()
    {
        if (duckRoutine != null) StopCoroutine(duckRoutine);
        duckRoutine = StartCoroutine(DuckRoutine(
            targetMusicMul: 1f,
            targetAmbientMul: 1f,
            duration: duckFadeDuration
        ));
    }

    public void StartWeatherAmbience(WeatherType weather)
    {
        currentWeather = weather;

        // stop loop coroutine lama kalau ada
        if (acidRainLoopCo != null)
        {
            StopCoroutine(acidRainLoopCo);
            acidRainLoopCo = null;
        }


        if (weather == WeatherType.Sunny)
        {
            StopWeatherAmbience();
            return;
        }

        AudioClip loopClip = (weather == WeatherType.Fog) ? fogAmbienceLoop : acidRainLoop;
        if (loopClip == null || ambientSource == null) return;

        if (ambientSource.isPlaying && ambientSource.clip == loopClip)
        {
            // kalau acid rain lagi main, pastiin coroutine pemotong tail tetap aktif
            if (weather == WeatherType.AcidRain && acidRainLoopCo == null)
                acidRainLoopCo = StartCoroutine(AcidRainLoopWithoutTail()); // atau nama coroutine kamu

            return;
        }


        ambientSource.Stop();
        ambientSource.clip = loopClip;
        ambientSource.loop = true;

        // Set volume target sesuai settings + duck
        ApplyVolumeSettings();

        // Set volume target (30% dari base SFX), tetap ikut duck kalau kamu pakai ducking
        float targetVol = (masterVolume * sfxVolume) * weatherAmbienceMul * ambientDuckMul;
        // Mulai dari 0 biar fade-in mulus
        ambientSource.volume = 0f;
        ambientSource.Play();

        if (ambienceFadeRoutine != null) StopCoroutine(ambienceFadeRoutine);
        ambienceFadeRoutine = StartCoroutine(FadeAudioSourceVolume(ambientSource, targetVol, ambienceFadeDuration));

        // ===== ACID RAIN: potong 1 detik tail (awal & loop) =====
        if (weather == WeatherType.AcidRain)
        {
            acidRainLoopCo = StartCoroutine(AcidRainLoopWithoutTail());
        }
    }
    
    private IEnumerator AcidRainLoopWithoutTail()
    {
        if (ambientSource == null || ambientSource.clip == null) yield break;

        float len = ambientSource.clip.length;
        float loopEnd = Mathf.Max(0.01f, len - acidRainCutTailSeconds);

        while (ambientSource != null && ambientSource.isPlaying && ambientSource.clip == acidRainLoop)
        {
            // Begitu mendekati "loopEnd", langsung balik ke 0
            if (ambientSource.time >= loopEnd)
            {
                ambientSource.time = 0f;
            }
            yield return null;
        }
    }


    public void StopWeatherAmbience()
    {
        currentWeather = WeatherType.Sunny;

        if (acidRainLoopCo != null)
        {
            StopCoroutine(acidRainLoopCo);
            acidRainLoopCo = null;
        }

        if (ambientSource == null) return;

        if (ambienceFadeRoutine != null) StopCoroutine(ambienceFadeRoutine);
        ambienceFadeRoutine = StartCoroutine(StopAmbienceWithFade());
    }

    private IEnumerator StopAmbienceWithFade()
    {
        // Fade ke 0 dulu
        yield return FadeAudioSourceVolume(ambientSource, 0f, ambienceFadeDuration);

        ambientSource.Stop();
        ambientSource.clip = null;
        ambientSource.loop = false;

        // Balikin lagi volume sesuai settings (untuk next time)
        ApplyVolumeSettings();
    }


    // boss setting audio

    private Coroutine waveFadeRoutine;
    public float waveFadeDuration = 0.2f;

    public void PlayBossSummoned()
    {
        if (bossSummonedSFX != null && waveSource != null)
        {
            if (waveFadeRoutine != null) StopCoroutine(waveFadeRoutine);

            waveSource.Stop();
            waveSource.clip = bossSummonedSFX;
            
            // --- UBAH JADI TRUE ---
            waveSource.loop = true; // Agar main terus sampai boss mati
            // ----------------------
            
            waveSource.volume = 0f;
            waveSource.Play();

            float targetVol = masterVolume * sfxVolume;
            // Fade In masuk (2 detik)
            waveFadeRoutine = StartCoroutine(FadeAudioSourceVolume(waveSource, targetVol, 2f));
            
            Debug.Log("[AudioManager] 🔊 Playing Boss Music Loop...");
        }
    }

    // --- FUNGSI BARU: Stop Boss Music dengan Fade Out ---
    public void StopBossMusic()
    {
        if (waveSource == null) return;
        
        if (waveFadeRoutine != null) StopCoroutine(waveFadeRoutine);
        
        // Fade Out selama 2 detik (bisa diubah angkanya)
        waveFadeRoutine = StartCoroutine(StopBossWithFade(2f));
    }

    private IEnumerator StopBossWithFade(float duration)
    {
        // Turunkan volume ke 0 secara perlahan
        yield return FadeAudioSourceVolume(waveSource, 0f, duration);
        
        waveSource.Stop();
        waveSource.clip = null;
        waveSource.loop = false;
        
        // Kembalikan volume source ke normal untuk penggunaan berikutnya
        waveSource.volume = masterVolume * sfxVolume;
        Debug.Log("[AudioManager] 🔇 Boss Music Faded Out.");
    }

    public void StartWaveOngoing()
    {
        if (waveSource == null || waveOngoingLoop == null) return;
        if (waveSource.isPlaying && waveSource.clip == waveOngoingLoop) return;

        waveSource.Stop();
        waveSource.clip = waveOngoingLoop;
        waveSource.loop = true;

        float targetVol = masterVolume * sfxVolume;
        waveSource.volume = 0f;
        waveSource.Play();

        if (waveFadeRoutine != null) StopCoroutine(waveFadeRoutine);
        waveFadeRoutine = StartCoroutine(FadeAudioSourceVolume(waveSource, targetVol, waveFadeDuration));
    }


    public void StopWaveOngoing()
    {
        if (waveSource == null) return;

        if (waveFadeRoutine != null) StopCoroutine(waveFadeRoutine);
        waveFadeRoutine = StartCoroutine(StopWaveWithFade());
    }

    private IEnumerator StopWaveWithFade()
    {
        yield return FadeAudioSourceVolume(waveSource, 0f, waveFadeDuration);
        waveSource.Stop();
        waveSource.clip = null;
        waveSource.loop = false;
        waveSource.volume = masterVolume * sfxVolume;
    }

    public void PlayWheelTail(float duration, float speedMultiplier = 1f)
    {
        if (wheelSFX == null || sfxSource == null)
            return;

        sfxSource.Stop();

        sfxSource.clip = wheelSFX;
        sfxSource.loop = false;

        // Pitch = speed
        sfxSource.pitch = speedMultiplier;

        float clipLength = wheelSFX.length;

        // Durasi audio efektif berubah karena pitch
        float effectiveDuration = duration * speedMultiplier;

        float startTime = Mathf.Max(0f, clipLength - effectiveDuration);
        sfxSource.time = startTime;

        sfxSource.Play();
    }


    // Tambahkan method bawaan Unity ini
    private void OnApplicationQuit()
    {
        // Pastikan PersistenceManager masih ada sebelum menyimpan
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SaveGame();
            Debug.Log("[AudioManager] Saving settings on exit.");
        }
    }

}
