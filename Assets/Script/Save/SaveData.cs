using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int totalGem = 0;
    public int maxUnlockedLevel = 1;

    // key: troopId, value: level (1-5)
    public Dictionary<string, int> troopLevels = new Dictionary<string, int>();

    // versioning (optional tapi bagus)
    public int version = 1;
}
