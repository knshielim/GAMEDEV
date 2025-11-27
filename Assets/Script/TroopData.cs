using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum TroopRarity
{
    Common,
    Rare,
    Epic,
    Legendary,
    Mythic
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

    [Header("Stats")]
    public int maxHealth;
    public int attack;
    public float moveSpeed;        // jalan ke depan seberapa cepat
    public float attackInterval;   // waktu antar serangan (detik)

    [Header("Prefab Reference")]
    public GameObject prefab;      // prefab unit (Slime_Blue, Warrior_1, dst)
}

