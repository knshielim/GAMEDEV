using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    // ==================== MAIN SETTINGS ====================
    [Header("Main Settings Panel")]
    public GameObject settingsPanel;
    public Button settingsButton;
    public Button closeSettingsButton;

    // ==================== TABS ====================
    [Header("Settings Tabs")]
    public Button volumeTabButton;
    public Button troopDirectoryTabButton;
    public Button helpTabButton;

    // ==================== CONTENT PANELS ====================
    [Header("Settings Content Panels")]
    public GameObject volumePanel;
    public GameObject troopDirectoryPanel;
    public GameObject helpPanel;

    // ==================== BACK BUTTONS ====================
    [Header("Back Buttons")]
    public Button volumeBackButton;
    public Button troopDirectoryBackButton;
    public Button helpBackButton;

    // ==================== VOLUME ====================
    [Header("Volume Sliders")]
    public Slider masterVolumeSlider;
    public Slider musicVolumeSlider;
    public Slider sfxVolumeSlider;

    [Header("Volume Text")]
    public TextMeshProUGUI masterVolumeText;
    public TextMeshProUGUI musicVolumeText;
    public TextMeshProUGUI sfxVolumeText;

    // ==================== TROOP DIRECTORY ====================
    [Header("Troop Directory Buttons")]
    public Button commonButton;
    public Button rareButton;
    public Button epicButton;
    public Button legendaryButton;
    public Button mythicButton;

    [Header("Troop Display")]
    public GameObject troopDisplayPanel;

    [Header("Troop Sprites")]
    public Sprite commonTroopSprite;
    public Sprite rareTroopSprite;
    public Sprite epicTroopSprite;
    public Sprite legendaryTroopSprite;
    public Sprite mythicTroopSprite;

    private Image troopDisplayImage;

    // ==================== UNITY LIFECYCLE ====================

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void Start()
    {
        if (troopDisplayPanel != null)
            troopDisplayImage = troopDisplayPanel.GetComponent<Image>();

        SetupButtons();
        SetupSliders();
        UpdateVolumeUI();

        settingsPanel.SetActive(false);
        HideAllPanels();
        HideAllBackButtons();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && settingsPanel.activeSelf)
            CloseSettings();
    }

    // ==================== BUTTON SETUP ====================

    private void SetupButtons()
    {
        settingsButton?.onClick.AddListener(ToggleSettings);
        closeSettingsButton?.onClick.AddListener(CloseSettings);

        volumeTabButton?.onClick.AddListener(ShowVolumePanel);
        troopDirectoryTabButton?.onClick.AddListener(ShowTroopDirectoryPanel);
        helpTabButton?.onClick.AddListener(ShowHelpPanel);

        volumeBackButton?.onClick.AddListener(ShowMainSettings);
        troopDirectoryBackButton?.onClick.AddListener(ShowMainSettings);
        helpBackButton?.onClick.AddListener(ShowMainSettings);

        commonButton?.onClick.AddListener(() => ShowTroop(TroopRarity.Common));
        rareButton?.onClick.AddListener(() => ShowTroop(TroopRarity.Rare));
        epicButton?.onClick.AddListener(() => ShowTroop(TroopRarity.Epic));
        legendaryButton?.onClick.AddListener(() => ShowTroop(TroopRarity.Legendary));
        mythicButton?.onClick.AddListener(() => ShowTroop(TroopRarity.Mythic));
    }

    private void SetupSliders()
    {
        masterVolumeSlider?.onValueChanged.AddListener(OnMasterVolumeChanged);
        musicVolumeSlider?.onValueChanged.AddListener(OnMusicVolumeChanged);
        sfxVolumeSlider?.onValueChanged.AddListener(OnSFXVolumeChanged);
    }

    // ==================== SETTINGS NAVIGATION ====================

    private void ToggleSettings()
    {
        AudioManager.Instance?.PlayButtonClick();

        bool isOpen = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpen);
        Time.timeScale = isOpen ? 0f : 1f;

        if (isOpen)
        {
            HideAllPanels();
            HideAllBackButtons();
            UpdateVolumeUI();
        }
    }

    private void CloseSettings()
    {
        AudioManager.Instance?.PlayButtonClick();

        settingsPanel.SetActive(false);
        Time.timeScale = 1f;
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SaveGame();
            Debug.Log("[SettingsManager] Settings saved on Close.");
        }
        HideAllPanels();
        HideAllBackButtons();
    }

    private void ShowVolumePanel()
    {
        AudioManager.Instance?.PlayButtonClick();

        settingsPanel.SetActive(false);
        HideAllPanels();
        HideAllBackButtons();

        volumePanel.SetActive(true);
        volumeBackButton.gameObject.SetActive(true);
    }

    private void ShowTroopDirectoryPanel()
    {
        AudioManager.Instance?.PlayButtonClick();

        settingsPanel.SetActive(false);
        HideAllPanels();
        HideAllBackButtons();

        troopDirectoryPanel.SetActive(true);
        troopDirectoryBackButton.gameObject.SetActive(true);
    }

    private void ShowHelpPanel()
    {
        AudioManager.Instance?.PlayButtonClick();

        settingsPanel.SetActive(false);
        HideAllPanels();
        HideAllBackButtons();

        helpPanel.SetActive(true);
        helpBackButton.gameObject.SetActive(true);
    }

    private void ShowMainSettings()
    {
        AudioManager.Instance?.PlayButtonClick();

        settingsPanel.SetActive(true);
        if (PersistenceManager.Instance != null)
        {
            PersistenceManager.Instance.SaveGame();
            Debug.Log("[SettingsManager] Audio settings saved on Back.");
        }
        HideAllPanels();
        HideAllBackButtons();
    }

    private void HideAllPanels()
    {
        volumePanel.SetActive(false);
        troopDirectoryPanel.SetActive(false);
        helpPanel.SetActive(false);
    }

    private void HideAllBackButtons()
    {
        volumeBackButton.gameObject.SetActive(false);
        troopDirectoryBackButton.gameObject.SetActive(false);
        helpBackButton.gameObject.SetActive(false);
    }

    // ==================== TROOP DISPLAY LOGIC ====================

    private void ShowTroop(TroopRarity rarity)
    {
        AudioManager.Instance?.PlayButtonClick();
        
        if (troopDisplayImage == null) return;

        switch (rarity)
        {
            case TroopRarity.Common:
                troopDisplayImage.sprite = commonTroopSprite;
                break;
            case TroopRarity.Rare:
                troopDisplayImage.sprite = rareTroopSprite;
                break;
            case TroopRarity.Epic:
                troopDisplayImage.sprite = epicTroopSprite;
                break;
            case TroopRarity.Legendary:
                troopDisplayImage.sprite = legendaryTroopSprite;
                break;
            case TroopRarity.Mythic:
                troopDisplayImage.sprite = mythicTroopSprite;
                break;
        }

        troopDisplayImage.enabled = troopDisplayImage.sprite != null;
    }

    // ==================== VOLUME ====================

    private void UpdateVolumeUI()
    {
        if (AudioManager.Instance == null) return;

        masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
        musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        sfxVolumeSlider.value = AudioManager.Instance.GetSFXVolume();

        UpdateVolumeText();
    }

    private void UpdateVolumeText()
    {
        masterVolumeText.text = $"{Mathf.RoundToInt(masterVolumeSlider.value * 100)}%";
        musicVolumeText.text = $"{Mathf.RoundToInt(musicVolumeSlider.value * 100)}%";
        sfxVolumeText.text = $"{Mathf.RoundToInt(sfxVolumeSlider.value * 100)}%";
    }

    private void OnMasterVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMasterVolume(value);
        UpdateVolumeText();
    }

    private void OnMusicVolumeChanged(float value)
    {
        AudioManager.Instance?.SetMusicVolume(value);
        UpdateVolumeText();
    }

    private void OnSFXVolumeChanged(float value)
    {
        AudioManager.Instance?.SetSFXVolume(value);
        UpdateVolumeText();
    }
}
