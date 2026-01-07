using UnityEngine;

public class WipeSaveDebug : MonoBehaviour
{
    [ContextMenu("WIPE SAVE FILE")]
    public void WipeSave()
    {
        SaveSystem.DeleteSave();
        Debug.Log("SAVE FILE DELETED");
    }
}
