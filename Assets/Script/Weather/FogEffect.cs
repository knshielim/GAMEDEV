using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class FogEffect : MonoBehaviour
{
    public static FogEffect Instance;

    [SerializeField] private Image fogImage;
    [SerializeField] private float fadeDuration = 1.5f;

    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        SetAlpha(0f); // start invisible
    }

    public void FadeIn()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(1f));
    }

    public void FadeOut()
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeTo(0f));
    }

    private IEnumerator FadeTo(float targetAlpha)
    {
        float startAlpha = fogImage.color.a;
        float time = 0f;

        while (time < fadeDuration)
        {
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, time / fadeDuration);
            SetAlpha(alpha);

            time += Time.unscaledDeltaTime; // works while paused
            yield return null;
        }

        SetAlpha(targetAlpha);
    }

    private void SetAlpha(float a)
    {
        Color c = fogImage.color;
        c.a = a;
        fogImage.color = c;
    }
}
