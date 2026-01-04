public static class TroopEconomy
{
    public static bool IsUpgradeable(TroopRarity rarity)
    {
        return rarity != TroopRarity.Boss;
    }

    public static float GetRarityMultiplier(TroopRarity rarity)
    {
        return rarity switch
        {
            TroopRarity.Common => 1f,
            TroopRarity.Rare => 2f,
            TroopRarity.Epic => 4f,
            TroopRarity.Legendary => 8f,
            TroopRarity.Mythic => 15f,
            _ => 1f
        };
    }
}
