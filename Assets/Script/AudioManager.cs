using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    // [Header("Audio Mixer")]
    // public AudioMixer audioMixer; // TODO: Uncomment when AudioMixer is set up

    [Header("BGM Clips")]
    public AudioClip introMusic;     // sound1
    public AudioClip loopMusic;      // loop

    [Header("Audio Sources")]
    public AudioSource musicSource;  // untuk BGM
    public AudioSource sfxSource;    // untuk SFX

    [Header("Volume Settings")]
    [Range(0f, 1f)] public float masterVolume = 1f;
    [Range(0f, 1f)] public float musicVolume = 0.7f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    [Header("SFX Clips")]
    public AudioClip gameOverSFX;
    public AudioClip gameWinSFX;
    public AudioClip hitTowerSFX;
    public AudioClip meleeAttackSFX;
    public AudioClip rangedAttackSFX;
    public AudioClip summonSFX;
    public AudioClip troopDeathSFX;
    public AudioClip upgradeSFX;

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
        PlayIntroThenLoop();
    }

    // ==========================
    // PLAY INTRO THEN LOOP BGM
    // ==========================

    public void PlayIntroThenLoop()
    {
        if (introMusic != null)
        {
            // Play the intro once
            musicSource.clip = introMusic;
            musicSource.loop = false;
            musicSource.Play();

            // After the intro is finished, play the loop
            Invoke(nameof(PlayLoopMusic), introMusic.length);
        }
        else
        {
            // If there is no intro, go straight to looping music
            PlayLoopMusic();
        }
    }

    private void PlayLoopMusic()
    {
        if (loopMusic != null)
        {
            musicSource.clip = loopMusic;
            musicSource.loop = true;
            musicSource.Play();
        }
    }

    // ==========================
    // SFX
    // ==========================
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null) return;
        sfxSource.PlayOneShot(clip);
    }

    // ==========================
    // VOLUME CONTROLS
    // ==========================

    public void SetMasterVolume(float volume)
    {
        masterVolume = volume;
        // Direct AudioSource volume control (more reliable than mixer)
        if (musicSource != null) musicSource.volume = volume * musicVolume;
        if (sfxSource != null) sfxSource.volume = volume * sfxVolume;

        // TODO: Uncomment when AudioMixer is properly set up
        // if (audioMixer != null)
        // {
        //     audioMixer.SetFloat("MasterVolume", volume == 0 ? -80f : Mathf.Log10(volume) * 20f);
        // }

        SaveAudioSettings();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = volume;
        // Direct AudioSource volume control
        if (musicSource != null) musicSource.volume = volume * masterVolume;

        // TODO: Uncomment when AudioMixer is properly set up
        // if (audioMixer != null)
        // {
        //     audioMixer.SetFloat("MusicVolume", volume == 0 ? -80f : Mathf.Log10(volume) * 20f);
        // }

        SaveAudioSettings();
    }

    public void SetSFXVolume(float volume)
    {
        sfxVolume = volume;
        // Direct AudioSource volume control
        if (sfxSource != null) sfxSource.volume = volume * masterVolume;

        // TODO: Uncomment when AudioMixer is properly set up
        // if (audioMixer != null)
        // {
        //     audioMixer.SetFloat("SFXVolume", volume == 0 ? -80f : Mathf.Log10(volume) * 20f);
        // }

        SaveAudioSettings();
    }

    private void ApplyVolumeSettings()
    {
        // Use direct AudioSource volume control for now (more reliable)
        if (musicSource != null) musicSource.volume = masterVolume * musicVolume;
        if (sfxSource != null) sfxSource.volume = masterVolume * sfxVolume;

        // TODO: Uncomment when AudioMixer is properly set up
        // if (audioMixer != null)
        // {
        //     SetMasterVolume(masterVolume);
        //     SetMusicVolume(musicVolume);
        //     SetSFXVolume(sfxVolume);
        // }
    }

    // ==========================
    // SETTINGS PERSISTENCE
    // ==========================

    private void SaveAudioSettings()
    {
        PlayerPrefs.SetFloat("MasterVolume", masterVolume);
        PlayerPrefs.SetFloat("MusicVolume", musicVolume);
        PlayerPrefs.SetFloat("SFXVolume", sfxVolume);
        PlayerPrefs.Save();
    }

    private void LoadAudioSettings()
    {
        masterVolume = PlayerPrefs.GetFloat("MasterVolume", 1f);
        musicVolume = PlayerPrefs.GetFloat("MusicVolume", 0.7f);
        sfxVolume = PlayerPrefs.GetFloat("SFXVolume", 0.8f);
    }

    // ==========================
    // UTILITY METHODS
    // ==========================

    public float GetMasterVolume() => masterVolume;
    public float GetMusicVolume() => musicVolume;
    public float GetSFXVolume() => sfxVolume;
}

