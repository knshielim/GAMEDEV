using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Mythic,
    Boss
}

[CreateAssetMenu(
    fileName = "NewTroopData",
    menuName = "Game/Troop Data"
)]
public class TroopData : ScriptableObject
{
    [Header("Basic Info")]
    public string id;              
    public string displayName;     
    public TroopRarity rarity;
    public Sprite icon;

    [Header("Stats")]
    public int maxHealth;
    public int attack;
    public float moveSpeed;        // jalan ke depan seberapa cepat
    public float attackInterval;   // waktu antar serangan (detik)

    [Header("Progression")]
    [Tooltip("This unit's level (1-5). Used for stat scaling.")]
    [Range(1, 5)] public int level = 1;

    [Header("Attack / Skill")]
    [Tooltip("If true, this troop will attack using projectiles instead of melee.")]
    public bool isRanged;

    [Tooltip("How far this troop can attack (used for both melee and ranged).")]
    public float attackRange = 0.5f;

    [Header("Critical Hit")]
    [Tooltip("Chance to deal critical damage (0–1). Example: 0.25 = 25%")]
    [Range(0f, 1f)]
    public float critRate = 0f;

    [Tooltip("Critical damage multiplier. Example: 2 = double damage")]
    public float critDamage = 2.0f;

    [Header("Economy")]
    [Tooltip("Base cost to upgrade this troop from Level 1 → 2 before multipliers")]
    public int baseUpgradeCost = 100;



    [Header("Projectile Settings (for ranged troops)")]
    [Tooltip("Projectile prefab to spawn when this unit attacks at range.")]
    public GameObject projectilePrefab;

    [Tooltip("How fast the projectile travels.")]
    public float projectileSpeed = 8f;

    [Tooltip("How long before the projectile is automatically destroyed.")]
    public float projectileLifetime = 3f;

    [Header("Prefab Reference")]
    [Tooltip("Prefab used by PLAYER (Troops).")]
    public GameObject playerPrefab;

    [Tooltip("Prefab used by ENEMY AI (Enemy).")]
    public GameObject enemyPrefab;
}
