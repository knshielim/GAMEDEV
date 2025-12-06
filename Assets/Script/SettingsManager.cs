using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Main Settings Panel")]
    public GameObject settingsPanel; // Main panel with tabs
    public Button settingsButton;
    public Button closeSettingsButton;

    [Header("Settings Tabs")]
    public Button volumeTabButton;
    public Button troopDirectoryTabButton;
    public Button helpTabButton;

    [Header("Settings Content Panels")]
    public GameObject volumePanel;
    public GameObject troopDirectoryPanel;
    public GameObject helpPanel;

    [Header("Back Buttons")]
    public Button volumeBackButton;
    public Button troopDirectoryBackButton;
    public Button helpBackButton;

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
        // Allow one SettingsManager per scene, not persistent
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // Tab button click handlers
    private void OnVolumeTabClicked()
    {
        ShowVolumePanel();
    }

    private void OnTroopDirectoryTabClicked()
    {
        ShowTroopDirectoryPanel();
    }

    private void OnHelpTabClicked()
    {
        ShowHelpPanel();
    }

    // Back button click handlers
    private void OnVolumeBackClicked()
    {
        ShowMainSettingsPanel();
    }

    private void OnTroopDirectoryBackClicked()
    {
        ShowMainSettingsPanel();
    }

    private void OnHelpBackClicked()
    {
        ShowMainSettingsPanel();
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

        // Setup tab buttons
        if (volumeTabButton != null)
        {
            volumeTabButton.onClick.RemoveAllListeners();
            volumeTabButton.onClick.AddListener(OnVolumeTabClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] VolumeTabButton not found!");
        }

        if (troopDirectoryTabButton != null)
        {
            troopDirectoryTabButton.onClick.RemoveAllListeners();
            troopDirectoryTabButton.onClick.AddListener(OnTroopDirectoryTabClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] TroopDirectoryTabButton not found!");
        }

        if (helpTabButton != null)
        {
            helpTabButton.onClick.RemoveAllListeners();
            helpTabButton.onClick.AddListener(OnHelpTabClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] HelpTabButton not found!");
        }

        // Setup back buttons - all go back to main settings panel
        if (volumeBackButton != null)
        {
            volumeBackButton.onClick.RemoveAllListeners();
            volumeBackButton.onClick.AddListener(OnVolumeBackClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] VolumeBackButton not found!");
        }

        if (troopDirectoryBackButton != null)
        {
            troopDirectoryBackButton.onClick.RemoveAllListeners();
            troopDirectoryBackButton.onClick.AddListener(OnTroopDirectoryBackClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] TroopDirectoryBackButton not found!");
        }

        if (helpBackButton != null)
        {
            helpBackButton.onClick.RemoveAllListeners();
            helpBackButton.onClick.AddListener(OnHelpBackClicked);
        }
        else
        {
            Debug.LogWarning("[SettingsManager] HelpBackButton not found!");
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
        // Setup all UI connections - references should be assigned in Inspector
        SetupButtonListeners();
        SetupSliderListeners();
        UpdateUI();

        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
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
        if (settingsPanel != null)
        {
            bool newState = !settingsPanel.activeSelf;
            settingsPanel.SetActive(newState);

            // Pause/resume game when settings panel opens/closes
            Time.timeScale = newState ? 0f : 1f;

            if (newState)
            {
                UpdateUI();
                // Don't show any panel by default - just show the tabs
                HideAllContentPanels();
                // Hide all back buttons initially
                if (volumeBackButton != null) volumeBackButton.gameObject.SetActive(false);
                if (troopDirectoryBackButton != null) troopDirectoryBackButton.gameObject.SetActive(false);
                if (helpBackButton != null) helpBackButton.gameObject.SetActive(false);
                // Reset tab button states (no tab selected)
                if (volumeTabButton != null) UpdateTabButtonVisual(volumeTabButton, false);
                if (troopDirectoryTabButton != null) UpdateTabButtonVisual(troopDirectoryTabButton, false);
                if (helpTabButton != null) UpdateTabButtonVisual(helpTabButton, false);
            }
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot toggle - settingsPanel is NULL! Assign it in Inspector!");
        }
    }

    public void CloseSettings()
    {
        if (settingsPanel != null)
        {
            settingsPanel.SetActive(false);
            // Resume game when closing settings
            Time.timeScale = 1f;
        }
        else
        {
            Debug.LogError("[SettingsManager] Cannot close - settingsPanel is NULL!");
        }

        // Hide all back buttons when closing settings
        if (volumeBackButton != null)
            volumeBackButton.gameObject.SetActive(false);
        if (troopDirectoryBackButton != null)
            troopDirectoryBackButton.gameObject.SetActive(false);
        if (helpBackButton != null)
            helpBackButton.gameObject.SetActive(false);
    }

    public void ShowVolumePanel()
    {
        // Hide main settings panel and show only volume panel
        if (settingsPanel != null) settingsPanel.SetActive(false);
        HideAllContentPanels();

        if (volumePanel != null)
        {
            volumePanel.SetActive(true);
        }
        else
        {
            Debug.LogError("[SettingsManager] volumePanel is NULL! Assign it in Inspector!");
        }

        // Show volume back button
        if (volumeBackButton != null)
        {
            volumeBackButton.gameObject.SetActive(true);
        }
    }

    public void ShowTroopDirectoryPanel()
    {
        // Hide main settings panel and show only troop directory panel
        if (settingsPanel != null) settingsPanel.SetActive(false);
        HideAllContentPanels();

        if (troopDirectoryPanel != null)
        {
            troopDirectoryPanel.SetActive(true);
            PopulateTroopDirectory();
        }
        else
        {
            Debug.LogError("[SettingsManager] troopDirectoryPanel is NULL! Assign it in Inspector!");
        }

        // Show troop directory back button
        if (troopDirectoryBackButton != null)
        {
            troopDirectoryBackButton.gameObject.SetActive(true);
        }
    }

    public void ShowHelpPanel()
    {
        // Hide main settings panel and show only help panel
        if (settingsPanel != null) settingsPanel.SetActive(false);
        HideAllContentPanels();

        if (helpPanel != null)
        {
            helpPanel.SetActive(true);
            PopulateHelpPanel();
        }
        else
        {
            Debug.LogError("[SettingsManager] helpPanel is NULL! Assign it in Inspector!");
        }

        // Show help back button
        if (helpBackButton != null)
        {
            helpBackButton.gameObject.SetActive(true);
        }
    }

    private void HideAllContentPanels()
    {
        if (volumePanel != null) volumePanel.SetActive(false);
        if (troopDirectoryPanel != null) troopDirectoryPanel.SetActive(false);
        if (helpPanel != null) helpPanel.SetActive(false);
    }

    private void UpdateTabButtonStates(Button activeButton)
    {
        // Reset all tab button colors/styles
        if (volumeTabButton != null) UpdateTabButtonVisual(volumeTabButton, false);
        if (troopDirectoryTabButton != null) UpdateTabButtonVisual(troopDirectoryTabButton, false);
        if (helpTabButton != null) UpdateTabButtonVisual(helpTabButton, false);

        // Highlight active tab
        if (activeButton != null) UpdateTabButtonVisual(activeButton, true);
    }

    private void UpdateTabButtonVisual(Button button, bool isActive)
    {
        // This is a placeholder - you'll need to implement the visual styling
        // For example, change button color, add/remove highlight, etc.
        ColorBlock colors = button.colors;
        if (isActive)
        {
            colors.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f); // Light gray for active
        }
        else
        {
            colors.normalColor = new Color(0.5f, 0.5f, 0.5f, 1f); // Dark gray for inactive
        }
        button.colors = colors;
    }

    public void ShowMainSettingsPanel()
    {
        // Show main settings panel with tabs
        if (settingsPanel != null) settingsPanel.SetActive(true);
        // Hide all content panels
        HideAllContentPanels();
        // Hide all back buttons
        if (volumeBackButton != null) volumeBackButton.gameObject.SetActive(false);
        if (troopDirectoryBackButton != null) troopDirectoryBackButton.gameObject.SetActive(false);
        if (helpBackButton != null) helpBackButton.gameObject.SetActive(false);
        // Reset tab button states (no tab selected)
        if (volumeTabButton != null) UpdateTabButtonVisual(volumeTabButton, false);
        if (troopDirectoryTabButton != null) UpdateTabButtonVisual(troopDirectoryTabButton, false);
        if (helpTabButton != null) UpdateTabButtonVisual(helpTabButton, false);
    }

    // STEP 1: Update ClearTroopDirectory
    private void ClearTroopDirectory()
    {
        if (troopDirectoryPanel != null)
        {
            // Only destroy the scroll view content
            Transform scrollView = troopDirectoryPanel.transform.Find("TroopScrollView");
            if (scrollView != null)
            {
                Destroy(scrollView.gameObject);
            }
        }
    }

    // STEP 2: Rewrite PopulateTroopDirectory to create scrollable horizontal rows
    private void PopulateTroopDirectory()
    {
        Debug.Log("[SettingsManager] PopulateTroopDirectory called");
        
        ClearTroopDirectory();

        if (GachaManager.Instance == null)
        {
            Debug.LogError("[SettingsManager] GachaManager.Instance is NULL!");
            return;
        }
        
        if (troopDirectoryPanel == null)
        {
            Debug.LogError("[SettingsManager] troopDirectoryPanel is NULL!");
            return;
        }

        var allTroops = GachaManager.Instance.GetAllTroopData();
        if (allTroops == null || allTroops.Count == 0)
        {
            Debug.LogWarning("[SettingsManager] No troops found!");
            CreateSimpleTroopDirectoryDisplay();
            return;
        }

        Debug.Log($"[SettingsManager] Found {allTroops.Count} troops, creating scroll view...");
        foreach (var troop in allTroops)
        {
            Debug.Log($"[SettingsManager] Troop: {troop.displayName}, Rarity: {troop.rarity}, HasPrefab: {(troop.playerPrefab != null ? "YES" : "NO")}");
        }

        // Create ScrollView container - centered horizontally and moved down
        GameObject scrollView = new GameObject("TroopScrollView");
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.SetParent(troopDirectoryPanel.transform);
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(0, -100); // Position more bottom center
        scrollRect.sizeDelta = new Vector2(1100, 550); // A bit bigger scroll view
        scrollRect.SetAsFirstSibling(); // Put it behind title and back button

        Debug.Log("[SettingsManager] ScrollView created");

        // Add ScrollRect component
        UnityEngine.UI.ScrollRect scrollComponent = scrollView.AddComponent<UnityEngine.UI.ScrollRect>();
        scrollComponent.horizontal = false;
        scrollComponent.vertical = true;
        scrollComponent.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
        scrollComponent.scrollSensitivity = 20f;

        // Create Viewport - add a visible background for debugging
        GameObject viewport = new GameObject("Viewport");
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.SetParent(scrollRect);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;
        
        UnityEngine.UI.Mask viewportMask = viewport.AddComponent<UnityEngine.UI.Mask>();
        viewportMask.showMaskGraphic = true; // Changed to true to see the viewport
        UnityEngine.UI.Image viewportImage = viewport.AddComponent<UnityEngine.UI.Image>();
        viewportImage.color = new Color(0.05f, 0.05f, 0.05f, 0.63f); // 0D0D0D with 180 alpha

        Debug.Log("[SettingsManager] Viewport created");

        // Create Content container
        GameObject content = new GameObject("Content");
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.SetParent(viewportRect);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        // Add VerticalLayoutGroup to stack rarity rows - with separators
        UnityEngine.UI.VerticalLayoutGroup layoutGroup = content.AddComponent<UnityEngine.UI.VerticalLayoutGroup>();
        layoutGroup.spacing = 15f; // Much closer spacing between sections
        layoutGroup.padding = new RectOffset(5, 5, 40, 40); // Smaller left/right margins, keep top/bottom
        layoutGroup.childAlignment = TextAnchor.UpperLeft;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = true;

        // Add ContentSizeFitter for auto height
        UnityEngine.UI.ContentSizeFitter sizeFitter = content.AddComponent<UnityEngine.UI.ContentSizeFitter>();
        sizeFitter.verticalFit = UnityEngine.UI.ContentSizeFitter.FitMode.PreferredSize;

        // Connect ScrollRect
        scrollComponent.viewport = viewportRect;
        scrollComponent.content = contentRect;

        Debug.Log("[SettingsManager] Content container created");

        // Group troops by rarity
        var troopsByRarity = allTroops.GroupBy(t => t.rarity)
                                    .OrderBy(g => g.Key)
                                    .ToDictionary(g => g.Key, g => g.ToList());

        Debug.Log($"[SettingsManager] Creating {troopsByRarity.Count} rarity rows...");

        int rarityIndex = 0;
        int totalRarities = troopsByRarity.Count;

        foreach (var rarityGroup in troopsByRarity)
        {
            Debug.Log($"[SettingsManager] Creating row for {rarityGroup.Key} with {rarityGroup.Value.Count} troops");
            CreateRarityRowWithSprites(contentRect, rarityGroup.Key, rarityGroup.Value);

            // Add separator line after each rarity section (except the last one)
            rarityIndex++;
            if (rarityIndex < totalRarities)
            {
                CreateRaritySeparator(contentRect);
            }
        }
        
        var fitter = content.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Debug.Log("[SettingsManager] Troop directory populated successfully!");
    }

    // Create a separator line between rarity sections
    private void CreateRaritySeparator(RectTransform parent)
    {
        GameObject separatorObj = new GameObject("RaritySeparator");
        RectTransform separatorRect = separatorObj.AddComponent<RectTransform>();
        separatorRect.SetParent(parent);
        separatorRect.anchorMin = new Vector2(0.5f, 0.5f);
        separatorRect.anchorMax = new Vector2(0.5f, 0.5f);
        separatorRect.pivot = new Vector2(0.5f, 0.5f);
        separatorRect.anchoredPosition = new Vector2(0, -10); // Position closer to troop names
        separatorRect.sizeDelta = new Vector2(200, 3); // Thin horizontal line, fixed width

        // Create the line using an Image component (transparent to hide it)
        UnityEngine.UI.Image separatorImage = separatorObj.AddComponent<UnityEngine.UI.Image>();
        separatorImage.color = new Color(0.7f, 0.7f, 0.7f, 0.0f); // Completely transparent to hide the line
    }

    // STEP 3: Create a row with rarity label and horizontal troop sprites
    private void CreateRarityRowWithSprites(RectTransform parent, TroopRarity rarity, List<TroopData> troops)
    {
        // Create row container
        GameObject rowContainer = new GameObject($"RarityRow_{rarity}");
        RectTransform rowRect = rowContainer.AddComponent<RectTransform>();
        rowRect.SetParent(parent);
        rowRect.sizeDelta = new Vector2(0, 600); // Much bigger to fit large troops

        // Add LayoutElement
        UnityEngine.UI.LayoutElement rowLayoutElement = rowContainer.AddComponent<UnityEngine.UI.LayoutElement>();
        rowLayoutElement.preferredHeight = 140; // Optimized for 3 troops vertical view
        rowLayoutElement.flexibleWidth = 1;

        // Create rarity header text
        GameObject headerObj = new GameObject("RarityHeader");
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.SetParent(rowRect);
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.pivot = new Vector2(0, 1);
        headerRect.anchoredPosition = new Vector2(0, 0);
        headerRect.sizeDelta = new Vector2(0, 20); // Smaller header to close gap

        TextMeshProUGUI headerText = headerObj.AddComponent<TextMeshProUGUI>();
        headerText.fontSize = 72; // 2x bigger (was 36)
        headerText.fontStyle = FontStyles.Bold;
        headerText.alignment = TextAlignmentOptions.Center;

        // Set color based on rarity
        switch (rarity)
        {
            case TroopRarity.Common: 
                headerText.color = new Color(0.7f, 0.7f, 0.7f);
                headerText.text = "COMMON";
                break;
            case TroopRarity.Rare: 
                headerText.color = new Color(0.3f, 0.7f, 1f);
                headerText.text = "RARE";
                break;
            case TroopRarity.Epic: 
                headerText.color = new Color(0.8f, 0.3f, 1f);
                headerText.text = "EPIC";
                break;
            case TroopRarity.Legendary: 
                headerText.color = new Color(1f, 0.7f, 0f);
                headerText.text = "LEGENDARY";
                break;
            case TroopRarity.Mythic: 
                headerText.color = new Color(1f, 0.4f, 0.8f);
                headerText.text = "MYTHIC";
                break;
        }

        // Create horizontal troop container
        GameObject troopContainer = new GameObject("TroopContainer");
        RectTransform troopContainerRect = troopContainer.AddComponent<RectTransform>();
        troopContainerRect.SetParent(rowRect);
        troopContainerRect.anchorMin = new Vector2(0, 0);
        troopContainerRect.anchorMax = new Vector2(1, 1);
        troopContainerRect.pivot = new Vector2(0, 1);
        troopContainerRect.anchoredPosition = new Vector2(0, -20); // Better spacing from header
        troopContainerRect.sizeDelta = new Vector2(0, -40);

        // Add HorizontalLayoutGroup for troops - centered
        UnityEngine.UI.HorizontalLayoutGroup troopLayout = troopContainer.AddComponent<UnityEngine.UI.HorizontalLayoutGroup>();
        troopLayout.spacing = -50f; // 2x closer spacing (double the overlap)
        troopLayout.padding = new RectOffset(0, 0, 0, 0);
        troopLayout.childAlignment = TextAnchor.UpperCenter; // Changed to center
        troopLayout.childControlHeight = false;
        troopLayout.childControlWidth = false;
        troopLayout.childForceExpandHeight = false;

        // Add bottom separator line after the troops
        CreateRaritySeparator(rowRect);
        troopLayout.childForceExpandWidth = false;

        // Create each troop item
        foreach (TroopData troop in troops)
        {
            CreateTroopItemWithSprite(troopContainer.transform, troop);
        }
    }

    // STEP 4: Create individual troop item (sprite + name) - even larger
    private void CreateTroopItemWithSprite(Transform parent, TroopData troop)
    {
        Debug.Log($"[SettingsManager] Creating troop item: {troop.displayName}");
        // Create item container
        GameObject itemContainer = new GameObject($"Troop_{troop.displayName}");
        RectTransform itemRect = itemContainer.AddComponent<RectTransform>();
        itemRect.SetParent(parent);
        itemRect.sizeDelta = new Vector2(350, 350); // More smaller container

        // Create sprite
        GameObject spriteObj = new GameObject("Sprite");
        RectTransform spriteRect = spriteObj.AddComponent<RectTransform>();
        spriteRect.SetParent(itemRect);
        spriteRect.anchorMin = new Vector2(0.5f, 1);
        spriteRect.anchorMax = new Vector2(0.5f, 1);
        spriteRect.pivot = new Vector2(0.5f, 1);
        spriteRect.anchoredPosition = new Vector2(0, 0);
        spriteRect.sizeDelta = new Vector2(280, 280); // More smaller sprite

        Image spriteImage = spriteObj.AddComponent<Image>();
        spriteImage.preserveAspect = true;
        
        // Get sprite from prefab
        if (troop.playerPrefab != null)
        {
            SpriteRenderer sr = troop.playerPrefab.GetComponent<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                spriteImage.sprite = sr.sprite;
            }
            else
            {
                // Fallback: white square if no sprite
                spriteImage.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            }
        }

        // Create name text
        GameObject nameObj = new GameObject("Name");
        RectTransform nameRect = nameObj.AddComponent<RectTransform>();
        nameRect.SetParent(itemRect);
        nameRect.anchorMin = new Vector2(0, 0);
        nameRect.anchorMax = new Vector2(1, 0);
        nameRect.pivot = new Vector2(0.5f, 0);
        nameRect.anchoredPosition = new Vector2(0, 0);
        nameRect.sizeDelta = new Vector2(0, 70); // Slightly smaller text area

        TextMeshProUGUI nameText = nameObj.AddComponent<TextMeshProUGUI>();
        nameText.fontSize = 36; // Slightly smaller font
        nameText.color = Color.white;
        nameText.alignment = TextAlignmentOptions.Center;
        nameText.text = troop.displayName;
        nameText.enableWordWrapping = true;
    }

    private void CreateSimpleTroopDirectoryDisplay()
    {
        Debug.Log("[SettingsManager] Creating fallback simple troop directory display");

        // Create a simple text display as fallback
        GameObject textObj = new GameObject("TroopDirectoryText");
        textObj.transform.SetParent(troopDirectoryPanel.transform);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 18;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.text = "<b>TROOP DIRECTORY</b>\n\nTroop data not available.\nPlease ensure GachaManager is properly set up.";

        // Set up the RectTransform
        RectTransform rectTransform = textObj.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0, 1);
        rectTransform.anchorMax = new Vector2(1, 1);
        rectTransform.pivot = new Vector2(0.5f, 1);
        rectTransform.anchoredPosition = new Vector2(0, -20);
        rectTransform.sizeDelta = new Vector2(-40, 400);
    }

    private void PopulateHelpPanel()
    {
        Debug.Log("[SettingsManager] Creating help panel with scroll view...");

        // Remove old scroll views if they exist
        Transform existing = helpPanel.transform.Find("HelpScrollView");
        if (existing != null)
            Destroy(existing.gameObject);

        // ------------------------
        // CREATE SCROLL VIEW
        // ------------------------
        GameObject scrollView = new GameObject("HelpScrollView");
        RectTransform scrollRect = scrollView.AddComponent<RectTransform>();
        scrollRect.SetParent(helpPanel.transform);
        scrollRect.anchorMin = new Vector2(0.5f, 0.5f);
        scrollRect.anchorMax = new Vector2(0.5f, 0.5f);
        scrollRect.pivot = new Vector2(0.5f, 0.5f);
        scrollRect.anchoredPosition = new Vector2(0, -100);
        scrollRect.sizeDelta = new Vector2(1100, 550);

        ScrollRect scrollComponent = scrollView.AddComponent<ScrollRect>();
        scrollComponent.horizontal = false;
        scrollComponent.vertical = true;
        scrollComponent.movementType = ScrollRect.MovementType.Clamped;
        scrollComponent.scrollSensitivity = 20f;

        // ------------------------
        // CREATE VIEWPORT
        // ------------------------
        GameObject viewport = new GameObject("Viewport");
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.SetParent(scrollRect);
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.anchoredPosition = Vector2.zero;
        viewportRect.sizeDelta = Vector2.zero;

        // Mask + Image
        Mask viewportMask = viewport.AddComponent<Mask>();
        viewportMask.showMaskGraphic = false;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.1f, 0.1f, 0.1f, 0.8f); // Dark background for help panel

        scrollComponent.viewport = viewportRect;

        // ------------------------
        // CREATE CONTENT
        // ------------------------
        GameObject content = new GameObject("Content");
        RectTransform contentRect = content.AddComponent<RectTransform>();
        contentRect.SetParent(viewportRect);
        contentRect.anchorMin = new Vector2(0, 1);
        contentRect.anchorMax = new Vector2(1, 1);
        contentRect.pivot = new Vector2(0.5f, 1);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = new Vector2(0, 0);

        // Content auto-size
        ContentSizeFitter contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup layoutGroup = content.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childControlHeight = true;
        layoutGroup.childControlWidth = true;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        layoutGroup.spacing = 20;
        layoutGroup.padding = new RectOffset(20, 20, 20, 40);

        scrollComponent.content = contentRect;

        // ------------------------
        // CREATE HELP TEXT
        // ------------------------
        GameObject textObj = new GameObject("HelpText");
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.SetParent(contentRect);
        textRect.anchorMin = new Vector2(0, 1);
        textRect.anchorMax = new Vector2(1, 1);
        textRect.pivot = new Vector2(0.5f, 1);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(0, 0);

        TextMeshProUGUI textComponent = textObj.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 32;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.TopLeft;
        textComponent.enableWordWrapping = true;

        // Automatically resize height to fit text
        ContentSizeFitter textFitter = textObj.AddComponent<ContentSizeFitter>();
        textFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // ------------------------
        // HELP TEXT CONTENT
        // ------------------------
        string helpText = "<b>GAME HELP & CONTROLS</b>\n\n";

        helpText += "<b>Basic Gameplay:</b>\n";
        helpText += "• Build your tower by upgrading it with earned coins\n";
        helpText += "• Summon troops to defend against enemy attacks\n";
        helpText += "• Destroy the enemy tower to win the level\n\n";

        helpText += "<b>Controls:</b>\n";
        helpText += "• <b>P</b> - Summon random troop\n";
        helpText += "• <b>O</b> - Deploy troops to slots\n";
        helpText += "• <b>I</b> - Upgrade tower\n";
        helpText += "• <b>U</b> - Upgrade summon rate\n";
        helpText += "• <b>1–0/-/=</b> - Select slot\n";
        helpText += "• <b>M</b> - Merge troops when 3+ in same slot\n";
        helpText += "• <b>Mouse</b> - Interact with UI\n";
        helpText += "• <b>ESC</b> - Close settings\n\n";

        helpText += "<b>Troop System:</b>\n";
        helpText += "• Troops have rarities: Common → Rare → Epic → Mythic\n";
        helpText += "• Merge 3 troops of the same type to increase their rarity\n";
        helpText += "• Mythics require special combinations\n";
        helpText += "• Each troop has unique stats and abilities\n\n";

        helpText += "<b>Strategy Tips:</b>\n";
        helpText += "• Balance your upgrades between tower and troops\n";
        helpText += "• Save coins for clutch moments\n";
        helpText += "• Experiment with different team compositions\n";

        textComponent.text = helpText;

        Debug.Log("[SettingsManager] Help panel successfully populated!");
    }


    private void UpdateUI()
    {
        if (AudioManager.Instance == null) return;

        if (masterVolumeSlider != null && AudioManager.Instance != null)
        {
            masterVolumeSlider.value = AudioManager.Instance.GetMasterVolume();
        }

        if (musicVolumeSlider != null && AudioManager.Instance != null)
        {
            musicVolumeSlider.value = AudioManager.Instance.GetMusicVolume();
        }

        if (sfxVolumeSlider != null && AudioManager.Instance != null)
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