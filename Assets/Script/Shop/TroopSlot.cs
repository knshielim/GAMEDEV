using UnityEngine;
using UnityEngine.UI;

public class TroopSlot : MonoBehaviour
{
    private Image slotImage; 
    private TroopData troopData;
    private ShopManager shopManager;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
    }

    public void Init(TroopData data, ShopManager shop)
    {
        troopData = data;
        shopManager = shop;

        // Syncs the data icon to the sprite you've already set in the Inspector
        if (slotImage != null && slotImage.sprite != null)
        {
            data.icon = slotImage.sprite; 
        }

        // Ensures the instance exists in the manager's dictionary
        shopManager.GetOrCreateInstance(data);
    }

    public void OnClick()
    {
        if (AudioManager.Instance != null) 
            AudioManager.Instance.PlayButtonClick();
            
        // This is the line that triggers the info to show in the right panel
        shopManager.SelectTroop(this, troopData);
    }

    public void SetSelected(bool selected)
    {
        if (slotImage != null)
        {
            // Tints the slot yellow when selected, white when not
            slotImage.color = selected ? Color.yellow : Color.white;
        }
    }

    public void Refresh() { /* Optional logic here */ }
}