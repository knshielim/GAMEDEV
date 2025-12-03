using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Settings Panel")]
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button closeSettingsButton;

    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Text")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Clean up instance and event subscription
        if (Instance == this)
        {
            Instance = null;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // Called when a new scene is loaded
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SettingsManager] Scene '{scene.name}' loaded, re-establishing connections...");

        // Re-establish UI connections after scene load
        ReconnectUIElements();

        // Make sure settings panel is hidden after scene load
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
    }

    // Re-establish UI connections after scene load
    private void ReconnectUIElements()
    {
        // Only search for elements if they're not already assigned
        // This allows for both inspector assignment and automatic finding
        if (settingsButton == null)
            settingsButton = GameObject.Find("SettingsButton")?.GetComponent<Button>();

        if (closeSettingsButton == null)
            closeSettingsButton = GameObject.Find("CloseButton")?.GetComponent<Button>();

        if (settingsPanel == null)
            settingsPanel = GameObject.Find("SettingsPanel");

        // Find volume sliders
        if (masterVolumeSlider == null)
            masterVolumeSlider = GameObject.Find("MasterSlider")?.GetComponent<Slider>();

        if (musicVolumeSlider == null)
            musicVolumeSlider = GameObject.Find("MusicSlider")?.GetComponent<Slider>();

        if (sfxVolumeSlider == null)
            sfxVolumeSlider = GameObject.Find("SFXSlider")?.GetComponent<Slider>();

        // Find volume texts
        if (masterVolumeText == null)
            masterVolumeText = GameObject.Find("MasterValueText")?.GetComponent<TextMeshProUGUI>();

        if (musicVolumeText == null)
            musicVolumeText = GameObject.Find("MusicValueText")?.GetComponent<TextMeshProUGUI>();

        if (sfxVolumeText == null)
            sfxVolumeText = GameObject.Find("SFXValueText")?.GetComponent<TextMeshProUGUI>();

        // Re-setup listeners
        SetupButtonListeners();
        SetupSliderListeners();

        Debug.Log("[SettingsManager] UI connections re-established");
    }

    // Separate method for setting up button listeners
    private void SetupButtonListeners()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners(); // Clear old listeners
            settingsButton.onClick.AddListener(ToggleSettings);
            Debug.Log("[SettingsManager] Settings button reconnected");
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners(); // Clear old listeners
            closeSettingsButton.onClick.AddListener(CloseSettings);
            Debug.Log("[SettingsManager] Close button reconnected");
        }
    }

    // Separate method for setting up slider listeners
    private void SetupSliderListeners()
    {
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.RemoveAllListeners();
            masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.RemoveAllListeners();
            musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.onValueChanged.RemoveAllListeners();
            sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
        }
    }

    private void Start()
    {
        Debug.Log("[SettingsManager] Initializing...");

        // Setup all UI connections
        SetupButtonListeners();
        SetupSliderListeners();

        // Initialize UI with current values
        UpdateUI();

        // Start with settings panel closed
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsManager] Settings panel initialized and hidden");
        }
        else
        {
            Debug.LogError("[SettingsManager] Settings panel is NULL!");
        }
    }

    private void Update()
    {
        // Allow ESC key to close settings
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel != null && settingsPanel.activeSelf)
        {
            CloseSettings();
        }
    }

    public void ToggleSettings()
    {
        Debug.Log("[SettingsManager] ToggleSettings called!");
        if (settingsPanel != null)
        {
            bool isActive = settingsPanel.activeSelf;
            bool newState = !isActive;
            settingsPanel.SetActive(newState);
            Debug.Log($"[SettingsManager] Settings panel set to: {newState}");

            // Additional debugging
            Debug.Log($"[SettingsManager] Panel active in hierarchy: {settingsPanel.activeInHierarchy}");
            Debug.Log($"[SettingsManager] Panel transform: {settingsPanel.transform.position}");

            if (newState)
            {
                UpdateUI();
                Debug.Log("[SettingsManager] Settings panel should now be VISIBLE!");
            }
            else
            {
                Debug.Log("[SettingsManager] Settings panel should now be HIDDEN!");
            }
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot toggle - settingsPanel is NULL!");
        }
    }

    public void CloseSettings()
    {
        Debug.Log("[SettingsManager] CloseSettings called!");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsManager] Settings panel closed");
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot close - settingsPanel is NULL!");
        }
    }

    // Debug method - call this from button to test if SettingsManager works
    public void DebugTest()
    {
        Debug.Log("[SettingsManager] DebugTest called - SettingsManager is working!");
    }

    // Force show settings panel for debugging
    public void ForceShowSettings()
    {
        Debug.Log("[SettingsManager] Force showing settings panel!");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(true);
            UpdateUI();
            Debug.Log("[SettingsManager] Settings panel FORCED visible!");

            // Additional checks
            var canvas = settingsPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"[SettingsManager] Canvas found: {canvas.name}, RenderMode: {canvas.renderMode}, SortingOrder: {canvas.sortingOrder}");
            }
            else
            {
                Debug.LogError("[SettingsManager] No Canvas found in parent hierarchy!");
            }

            var image = settingsPanel.GetComponent<UnityEngine.UI.Image>();
            if (image != null)
            {
                Debug.Log($"[SettingsManager] Panel Image color: {image.color}, enabled: {image.enabled}");
            }
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot force show - settingsPanel is NULL!");
        }
    }

    private void UpdateUI()
    {
        if (AudioManager.Instance == null) return;

        // Update sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
        }

        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        }

        if (sfxVolumeSlider != null)
        {
            sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();
        }

        // Update text displays
        UpdateVolumeTexts();
    }

    private void UpdateVolumeTexts()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeText != null)
        {
            masterVolumeText.text = $"{Mathf.RoundToInt(AudioManager.Instance.GetMasterVolume() * 100)}%";
        }

        if (musicVolumeText != null)
        {
            musicVolumeText.text = $"{Mathf.RoundToInt(AudioManager.Instance.GetMusicVolume() * 100)}%";
        }

        if (sfxVolumeText != null)
        {
            sfxVolumeText.text = $"{Mathf.RoundToInt(AudioManager.Instance.GetSFXVolume() * 100)}%";
        }
    }

    // Volume change handlers
    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
            UpdateVolumeTexts();
        }
        else
        {
            Debug.LogWarning("[SettingsManager] AudioManager.Instance is NULL in OnMasterVolumeChanged");
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateVolumeTexts();
        }
        else
        {
            Debug.LogWarning("[SettingsManager] AudioManager.Instance is NULL in OnMusicVolumeChanged");
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateVolumeTexts();
        }
        else
        {
            Debug.LogWarning("[SettingsManager] AudioManager.Instance is NULL in OnSFXVolumeChanged");
        }
    }
}