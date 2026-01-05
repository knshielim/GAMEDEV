using UnityEngine;

public class Bootstrap : MonoBehaviour
{
    private static Bootstrap _instance;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject); // Hancurkan duplikat Bootstrap
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject); // Ini akan menyelamatkan semua child (termasuk GameDebugConfig)
    }
}