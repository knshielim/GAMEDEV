using UnityEngine;
using UnityEngine.UI;
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
        Debug.Log($"[SettingsManager] Awake called on {gameObject.name}, Instance is currently: {(Instance == null ? "NULL" : Instance.gameObject.name)}");
        
        // Allow one SettingsManager per scene, not persistent
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[SettingsManager] This instance ({gameObject.name}) is now THE Instance");
            // REMOVED DontDestroyOnLoad - SettingsManager recreated each scene
        }
        else
        {
            Debug.LogWarning($"[SettingsManager] Destroying duplicate instance on {gameObject.name}, keeping {Instance.gameObject.name}");
            Destroy(gameObject);
            return;
        }
    }

    private void OnDestroy()
    {
        // Clean up instance
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SetupButtonListeners()
    {
        if (settingsButton != null)
        {
            settingsButton.onClick.RemoveAllListeners();
            settingsButton.onClick.AddListener(ToggleSettings);
            Debug.Log("[SettingsManager] Settings button listener added");
        }
        else
        {
            Debug.LogWarning("[SettingsManager] SettingsButton not found!");
        }

        if (closeSettingsButton != null)
        {
            closeSettingsButton.onClick.RemoveAllListeners();
            closeSettingsButton.onClick.AddListener(CloseSettings);
            Debug.Log("[SettingsManager] Close button listener added");
        }
        else
        {
            Debug.LogWarning("[SettingsManager] CloseButton not found!");
        }
    }

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
        Debug.Log($"[SettingsManager] Start called on {gameObject.name}");
        Debug.Log($"[SettingsManager] settingsPanel = {(settingsPanel == null ? "NULL" : settingsPanel.name)}");
        Debug.Log($"[SettingsManager] settingsButton = {(settingsButton == null ? "NULL" : settingsButton.name)}");

        // Setup all UI connections - references should be assigned in Inspector
        SetupButtonListeners();
        SetupSliderListeners();
        UpdateUI();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            Debug.Log("[SettingsManager] Settings panel initialized and hidden");
        }
        else
        {
            Debug.LogError("[SettingsManager] Settings panel is NULL in Start! Make sure to assign it in Inspector!");
        }
    }

    private void Update()
    {
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
            bool newState = !settingsPanel.activeSelf;
            settingsPanel.SetActive(newState);
            Debug.Log($"[SettingsManager] Settings panel toggled to: {newState}");

            if (newState)
            {
                UpdateUI();
            }
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot toggle - settingsPanel is NULL! Assign it in Inspector!");
        }
    }

    public void CloseSettings()
    {
        Debug.Log("[SettingsManager] CloseSettings called!");
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot close - settingsPanel is NULL!");
        }
    }

    private void UpdateUI()
    {
        if (AudioManager.Instance == null) return;

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

    private void OnMasterVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMasterVolume(value);
            UpdateVolumeTexts();
        }
    }

    private void OnMusicVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetMusicVolume(value);
            UpdateVolumeTexts();
        }
    }

    private void OnSFXVolumeChanged(float value)
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.SetSFXVolume(value);
            UpdateVolumeTexts();
        }
    }
}