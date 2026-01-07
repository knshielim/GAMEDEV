using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int totalGem = 0;
    public int maxUnlockedLevel = 1;

    public float masterVolume = 1f;
    public float musicVolume = 0.7f;
    public float sfxVolume = 0.8f;
    
    // 🔥 TAMBAHAN BARU: Tutorial & Dialogues
    public bool isTutorialCompleted = false;
    public List<string> seenDialogues = new List<string>(); 

    // Track completed levels (so they can be selected in level select)
    public List<int> completedLevels = new List<int>();

    // key: troopId, value: level (1-5)
    public Dictionary<string, int> troopLevels = new Dictionary<string, int>();

    public int version = 1;
}