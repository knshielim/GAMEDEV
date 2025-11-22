using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Unit))]
public class UnitHealthBarController : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The floating Canvas/Slider prefab to display.")]
    public GameObject HealthBarPrefab;

    [Header("Team Colors")]
    public Color PlayerColor = Color.green;
    public Color EnemyColor = Color.red;

    private Unit targetUnit;
    private Slider healthSlider;
    private GameObject healthBarInstance; // STORE REFERENCE TO THE INSTANTIATED HEALTH BAR
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

        // Instantiate health bar as child of this unit
        healthBarInstance = Instantiate(HealthBarPrefab, transform);
        
        healthBarInstance.transform.localPosition = new Vector3(0, 2f, 0); 
        
        healthSlider = healthBarInstance.GetComponentInChildren<Slider>();

        if (healthSlider == null)
        {
            Debug.LogError("HealthBarPrefab must contain a Slider component.");
            return;
        }

        healthSlider.maxValue = targetUnit.MaxHealth;
        
        Image fillImage = healthSlider.fillRect.GetComponent<Image>();
        
        fillImage.color = (targetUnit.UnitTeam == Team.Player) ? PlayerColor : EnemyColor; 

        mainCameraTransform = Camera.main.transform;

        targetUnit.OnHealthChanged += UpdateHealthBar; 
        UpdateHealthBar(); 
    }

    private void OnDestroy()
    {
        if (targetUnit != null)
        {
            targetUnit.OnHealthChanged -= UpdateHealthBar;
        }
        
        // Only destroy the health bar instance, NOT the root!
        if (healthBarInstance != null)
        {
            Destroy(healthBarInstance);
        }
    }

    private void LateUpdate()
    {
        if (mainCameraTransform != null && healthBarInstance != null)
        {
            // Make health bar face camera
            healthBarInstance.transform.rotation = mainCameraTransform.rotation;
        }
    }

    private void UpdateHealthBar()
    {
        if (healthSlider != null && targetUnit != null)
        {
            healthSlider.value = targetUnit.CurrentHealth;
            
            healthSlider.gameObject.SetActive(targetUnit.CurrentHealth < targetUnit.MaxHealth);
        }
    }
}