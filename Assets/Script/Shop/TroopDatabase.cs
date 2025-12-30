using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopDatabase : MonoBehaviour
{
    public static TroopDatabase Instance;
    public TroopData[] allTroops; // assign in inspector
    public Dictionary<string, TroopInstance> troopInstances = new Dictionary<string, TroopInstance>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }

        // Create instances for all troops
        foreach (var t in allTroops)
        {
            if (t != null && !troopInstances.ContainsKey(t.id))
                troopInstances[t.id] = new TroopInstance(t);
        }
    }

    public TroopInstance GetTroopInstance(string id)
    {
        if (troopInstances.ContainsKey(id))
            return troopInstances[id];
        return null;
    }
}
