using TMPro;
using UnityEngine;

public class DamagePopup : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Motion")]
    [SerializeField] private float floatUpSpeed = 1.5f;
    [SerializeField] private float duration = 0.8f;
    [SerializeField] private float fadeStart = 0.2f;

    [Header("Style")]
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color critColor = Color.red;
    [SerializeField] private float normalScale = 1.0f;
    [SerializeField] private float critScale = 1.4f;

    private float t;

    private void Awake()
    {
        if (!text) text = GetComponentInChildren<TMP_Text>();
        if (!canvasGroup) canvasGroup = GetComponentInChildren<CanvasGroup>();
        if (canvasGroup) canvasGroup.alpha = 1f;
    }

    public void Setup(int damage, bool isCrit)
    {
        if (!text) return;

        text.text = damage.ToString();

        if (isCrit)
        {
            text.color = critColor;
            transform.localScale *= critScale;
        }
        else
        {
            text.color = normalColor;
            transform.localScale *= normalScale;
        }
    }

    private void Update()
    {
        t += Time.deltaTime;

        // move up
        transform.position += Vector3.up * floatUpSpeed * Time.deltaTime;

        // fade
        if (canvasGroup && t >= fadeStart)
        {
            float fadeT = Mathf.InverseLerp(fadeStart, duration, t);
            canvasGroup.alpha = 1f - fadeT;
        }

        if (t >= duration)
            Destroy(gameObject);
    }
}
