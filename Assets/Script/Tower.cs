using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public enum TowerOwner { Player, Team }
    public TowerOwner owner;

    public int maxHealth = 100;
    public int currentHealth;

    private void Start()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        // Cek kalau tower sudah hancur
        if (currentHealth <= 0)
        {
            OnTowerDestroyed();
        }
    }

    void OnTowerDestroyed()
    {
        // Beritahu GameManager
        GameManager.Instance.TowerDestroyed(this);
    }
}
