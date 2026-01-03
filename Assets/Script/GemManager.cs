public class GemManager : MonoBehaviour
{
    public static GemManager Instance;

    public int levelGem;  
    public int totalGem;   // gem permanent (shop)

    [SerializeField] private TextMeshProUGUI gemText;

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
        }
    }

    private void Start()
    {
        LoadTotalGem();
        ResetLevelGem();
    }

    // ===== LEVEL GEM =====
    public void ResetLevelGem()
    {
        levelGem = 0;
        UpdateUI();
    }

    public void AddLevelGem(int amount)
    {
        levelGem += amount;
        UpdateUI();
    }

    // ===== TOTAL GEM =====
    public void SaveLevelGemToTotal()
    {
        totalGem += levelGem;
        PlayerPrefs.SetInt("TotalGem", totalGem);
        PlayerPrefs.Save();

        Debug.Log($"[GemManager] Saved {levelGem} → TotalGem = {totalGem}");
    }

    private void LoadTotalGem()
    {
        totalGem = PlayerPrefs.GetInt("TotalGem", 0);
    }

    // ===== UI =====
    private void UpdateUI()
    {
        if (gemText != null)
            gemText.text = levelGem.ToString(); 
    }
}
