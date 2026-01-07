using TMPro;
using UnityEngine;

public class ShopGemDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text gemText;

    private void OnEnable()
    {
        Refresh();
        // Subscribe to gem changes for real-time updates
        if (GemManager.Instance != null)
        {
            GemManager.OnTotalGemChanged += OnGemChanged;
        }
    }

    private void OnDisable()
    {
        // Unsubscribe when disabled
        GemManager.OnTotalGemChanged -= OnGemChanged;
    }

    private void OnGemChanged(int newTotal)
    {
        Refresh();
    }

    public void Refresh()
    {
        int total = 0;

        // Kalau GemManager sudah hidup (mis. masuk Shop dari Level 1)
        if (GemManager.Instance != null)
        {
            total = GemManager.Instance.totalGem; // <-- sesuai property di GemManager.cs
        }
        // Kalau GemManager belum ada, ambil dari save data
        else if (PersistenceManager.Instance != null && PersistenceManager.Instance.GetData() != null)
        {
            total = PersistenceManager.Instance.GetData().totalGem; // <-- sesuai LoadTotalGem kamu
        }

        if (gemText != null)
            gemText.text = total.ToString();
    }
}