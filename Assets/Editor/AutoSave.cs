using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public class AutoSaveScene
{
    static double lastSaveTime;
    static double saveInterval = 300; // 5 menit

    static AutoSaveScene()
    {
        EditorApplication.update += OnUpdate;
    }

    static void OnUpdate()
    {
        if (EditorApplication.timeSinceStartup - lastSaveTime > saveInterval)
        {
            if (!EditorApplication.isPlaying)
            {
                EditorSceneManager.SaveOpenScenes();
                lastSaveTime = EditorApplication.timeSinceStartup;
                Debug.Log("Scene autosaved");
            }
        }
    }
}
