using System.Collections;
using TMPro;
using UnityEngine;

public class GemBorderUI : MonoBehaviour
{
    [SerializeField] private TMP_Text gemText;
    [SerializeField] private bool showTotalGem = true;

    private void OnEnable()
    {
        GemManager.OnTotalGemChanged += HandleTotalChanged;
        GemManager.OnLevelGemChanged += HandleLevelChanged;

        // refresh saat scene baru kebuka
        StartCoroutine(RefreshWhenReady());
    }

    private void OnDisable()
    {
        GemManager.OnTotalGemChanged -= HandleTotalChanged;
        GemManager.OnLevelGemChanged -= HandleLevelChanged;
    }

    private IEnumerator RefreshWhenReady()
    {
        while (GemManager.Instance == null)
            yield return null;

        while (!GemManager.Instance.IsInitialized)
            yield return null;

        Refresh();
    }

    private void HandleTotalChanged(int _)
    {
        if (showTotalGem) Refresh();
    }

    private void HandleLevelChanged(int _)
    {
        if (!showTotalGem) Refresh();
    }

    public void Refresh()
    {
        if (gemText == null) return;
        if (GemManager.Instance == null) return;

        int value = showTotalGem ? GemManager.Instance.totalGem : GemManager.Instance.levelGem;
        gemText.text = value.ToString();
    }
}
