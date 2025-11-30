using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("BGM Clips")]
    public AudioClip introMusic;     // sound1
    public AudioClip loopMusic;      // loop

    [Header("Audio Sources")]
    public AudioSource musicSource;  // untuk BGM
    public AudioSource sfxSource;    // untuk SFX

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
        PlayIntroThenLoop();
    }

    // ==========================
    // PLAY INTRO THEN LOOP BGM
    // ==========================

    public void PlayIntroThenLoop()
    {
        if (introMusic != null)
        {
            // Mainkan intro sekali
            musicSource.clip = introMusic;
            musicSource.loop = false;
            musicSource.Play();

            // Setelah intro selesai, mainkan loop
            Invoke(nameof(PlayLoopMusic), introMusic.length);
        }
        else
        {
            // Kalau tidak ada intro, langsung ke looping music
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
}

