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
    [Tooltip("Vertical offset above the unit")]
    public float yOffset = 1.5f;

    [Header("Team Colors")]
    public Color PlayerColor = Color.green;
    public Color EnemyColor = Color.red;

    private Unit targetUnit;
    private Slider healthSlider;
    private GameObject healthBarInstance;
    private Transform mainCameraTransform;

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
        // 1. Initialize the health bar UI
        InitializeHealthBar();
        
        // 2. Subscribe to the event
        targetUnit.OnHealthChanged += UpdateHealthBar;
    }

    private void InitializeHealthBar()
    {
        if (HealthBarPrefab == null)
        {
            Debug.LogError($"[HealthBar] HealthBarPrefab is not assigned on {targetUnit.name}!");
            enabled = false;
            return;
        }

        // Instantiate health bar as child of this unit
        // This ensures the health bar moves with the unit
        healthBarInstance = Instantiate(HealthBarPrefab, transform);

        // Set local position with offset
        healthBarInstance.transform.localPosition = new Vector3(0, yOffset, 0);

        // Ensure the rotation is clean (important when parented)
        healthBarInstance.transform.localRotation = Quaternion.identity;
        
        // --- Scale Check ---
        // If your health bar prefab is a World Space Canvas, 
        // you might need to adjust the scale to be small (e.g., 0.01f). 
        // Check your prefab settings.
        
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
        
        // Set color based on team (assuming Unit has a public 'UnitTeam' property/field)
        Image fillImage = healthSlider.fillRect?.GetComponent<Image>();
        if (fillImage != null)
        {
            // Assuming 'targetUnit.UnitTeam' and 'Team.Player' are defined
            fillImage.color = (targetUnit.UnitTeam == Team.Player) ? PlayerColor : EnemyColor;
        }

        mainCameraTransform = Camera.main?.transform;
        
        // Initial visibility check
        UpdateHealthBar();
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
        if (mainCameraTransform != null && healthBarInstance != null)
        {
            // Make health bar face the camera (Billboard effect)
            // Using `mainCameraTransform.rotation` is common for flat billboards
            healthBarInstance.transform.rotation = mainCameraTransform.rotation;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthSlider == null || targetUnit == null)
        {
            return;
        }

        healthSlider.value = targetUnit.CurrentHealth;

        // Show health bar only when damaged or unit is nearly dead
        bool shouldShow = targetUnit.CurrentHealth < targetUnit.MaxHealth && targetUnit.CurrentHealth > 0;

        if (healthBarInstance != null)
        {
            healthBarInstance.SetActive(shouldShow);
        }
    }
}