using UnityEngine;
using UnityEngine.UI;
using System;

public class HealthBarUI : MonoBehaviour
{
    [Header("Dependencies")]
    public Tower targetTower;
    public Slider healthSlider;

    private void Start()
    {
        if (targetTower != null)
        {
            targetTower.OnHealthChanged += OnTowerHealthChanged;

            healthSlider.maxValue = targetTower.maxHealth; 
            healthSlider.value = targetTower.currentHealth; 
        }
        else
        {
             Debug.LogError("targetTower reference is not set in HealthBarUI!", this);
        }
    }

    private void OnTowerHealthChanged(int newHealth)
    {
        healthSlider.value = newHealth;
    }
    
    private void OnDestroy()
    {
        if (targetTower != null)
        {
            targetTower.OnHealthChanged -= OnTowerHealthChanged;
        }
    }
}