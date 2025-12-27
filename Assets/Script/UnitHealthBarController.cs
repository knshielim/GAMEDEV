using UnityEngine;
using UnityEngine.UI;
using System.Collections;

// This script MUST be attached to the Unit GameObject
[RequireComponent(typeof(Unit))]
public class UnitHealthBarController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The floating Canvas/Slider prefab to display.")]
    public GameObject HealthBarPrefab;

    [Header("Offset Settings")]
    [Tooltip("Horizontal offset from the unit center")]
    public float xOffset = 0f;
    [Tooltip("Vertical offset above the unit")]
    public float yOffset = 1.5f;
    
    [Tooltip("Scale of the health bar Canvas (adjust if health bars are too small/large). For 2D orthographic cameras, try 0.03 to 0.1")]
    public float canvasScale = 0.05f;

    [Header("Team Colors")]
    public Color PlayerColor = Color.green;
    public Color EnemyColor = Color.red;

    private Unit targetUnit;
    private Slider healthSlider;
    private GameObject healthBarInstance;
    private Transform mainCameraTransform;
    private Canvas healthBarCanvas;
    private bool isInitialized = false;

    private void Awake()
    {
        targetUnit = GetComponent<Unit>();
        if (targetUnit == null)
        {
            Debug.LogError("UnitHealthBarController requires a Unit component on the same GameObject.");
            enabled = false;
            return;
        }
    }

    private void Start()
    {
        // Use coroutine to ensure team is set (execution order fix)
        StartCoroutine(InitializeHealthBarDelayed());
        
        // Subscribe to the event
        targetUnit.OnHealthChanged += UpdateHealthBar;
    }

    private IEnumerator InitializeHealthBarDelayed()
    {
        // Wait one frame to ensure team is set by Troops/Enemy Start() methods
        yield return null;
        
        // Wait a tiny bit more to be safe
        yield return new WaitForEndOfFrame();
        
        InitializeHealthBar();
    }

    private void InitializeHealthBar()
    {
        if (isInitialized) return;
        
        if (HealthBarPrefab == null)
        {
            Debug.LogError($"[HealthBar] HealthBarPrefab is not assigned on {targetUnit.name}!");
            enabled = false;
            return;
        }

        // Instantiate health bar as child of this unit
        healthBarInstance = Instantiate(HealthBarPrefab, transform);

        // Set local position with offset
        healthBarInstance.transform.localPosition = new Vector3(xOffset, yOffset, 0);

        // Ensure the rotation is clean (important when parented)
        healthBarInstance.transform.localRotation = Quaternion.identity;
        
        // Get Canvas component and ensure it's set up for World Space
        healthBarCanvas = healthBarInstance.GetComponent<Canvas>();
        if (healthBarCanvas != null)
        {
            healthBarCanvas.renderMode = RenderMode.WorldSpace;
            healthBarCanvas.worldCamera = Camera.main;
            healthBarCanvas.sortingOrder = 100; // Ensure it renders on top
            
            // For 2D orthographic cameras, use a larger scale
            RectTransform canvasRect = healthBarCanvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                float finalScale = canvasScale;

                // ✅ REDUCE HEALTH BAR SIZE FOR BOSS UNITS (since they're 5x larger)
                if (targetUnit.GetTroopData() != null && targetUnit.GetTroopData().rarity == TroopRarity.Boss)
                {
                    finalScale *= 0.2f; // Make boss health bars 80% smaller (20% of normal size)
                    Debug.Log($"[HealthBar] 🏰 Boss detected - reducing health bar scale to {finalScale} (was {canvasScale})");
                }

                canvasRect.localScale = new Vector3(finalScale, finalScale, finalScale);
                Debug.Log($"[HealthBar] Set Canvas scale to {finalScale} for {targetUnit.name} (Camera orthographic: {Camera.main?.orthographic})");
            }
        }
        else
        {
            Debug.LogError($"[HealthBar] No Canvas component found on health bar prefab!");
        }
        
        // Get slider component
        healthSlider = healthBarInstance.GetComponentInChildren<Slider>();

        if (healthSlider == null)
        {
            Debug.LogError($"[HealthBar] HealthBarPrefab must contain a Slider component!");
            enabled = false;
            return;
        }

        // Set max value and initial value
        healthSlider.maxValue = targetUnit.MaxHealth;
        healthSlider.value = targetUnit.CurrentHealth;
        
        // Set color based on team
        UpdateHealthBarColor();

        mainCameraTransform = Camera.main?.transform;
        
        // Make sure health bar is visible from the start
        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(true);
            Debug.Log($"[HealthBar] Health bar GameObject active: {healthBarInstance.activeSelf}, position: {healthBarInstance.transform.position}, scale: {healthBarInstance.transform.localScale}");
        }
        if (healthSlider != null)
        {
            healthSlider.gameObject.SetActive(true);
            Debug.Log($"[HealthBar] Slider active: {healthSlider.gameObject.activeSelf}, value: {healthSlider.value}/{healthSlider.maxValue}");
        }
        
        isInitialized = true;
        
        // Initial visibility check
        UpdateHealthBar();
        
        Debug.Log($"[HealthBar] Initialized for {targetUnit.name} (Team: {targetUnit.UnitTeam}, Color: {(targetUnit.UnitTeam == Team.Player ? "Green" : "Red")}, Canvas: {(healthBarCanvas != null ? "Found" : "MISSING")})");
    }

    private void UpdateHealthBarColor()
    {
        if (healthSlider == null || targetUnit == null) return;
        
        Image fillImage = healthSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            Color teamColor = (targetUnit.UnitTeam == Team.Player) ? PlayerColor : EnemyColor;
            fillImage.color = teamColor;
            Debug.Log($"[HealthBar] Set color to {(targetUnit.UnitTeam == Team.Player ? "Green" : "Red")} for {targetUnit.name}");
        }
    }

    private void OnDestroy()
    {
        // IMPORTANT: Unsubscribe from the event to prevent null reference errors
        if (targetUnit != null)
        {
            targetUnit.OnHealthChanged -= UpdateHealthBar;
        }
        
        // Destroy the UI instance when the unit is destroyed
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform != null && healthBarInstance != null && healthBarInstance.activeSelf)
        {
            // For 2D orthographic cameras, keep the health bar facing the camera
            if (Camera.main != null && Camera.main.orthographic)
            {
                // For 2D, rotate to face camera but keep it flat
                Vector3 lookDirection = mainCameraTransform.forward;
                healthBarInstance.transform.rotation = Quaternion.LookRotation(-lookDirection, mainCameraTransform.up);
            }
            else
            {
                // For 3D perspective cameras, use billboard effect
                healthBarInstance.transform.LookAt(mainCameraTransform);
            }
        }
    }

    private void UpdateHealthBar()
    {
        if (healthSlider == null || targetUnit == null || healthBarInstance == null)
        {
            return;
        }

        healthSlider.value = targetUnit.CurrentHealth;

        // Always show health bar (unless unit is dead)
        bool shouldShow = targetUnit.CurrentHealth > 0 && !targetUnit.isDead;

        healthBarInstance.SetActive(shouldShow);
        healthSlider.gameObject.SetActive(shouldShow);
        
        // Update color based on team (in case team changes or wasn't set initially)
        UpdateHealthBarColor();
    }
}
