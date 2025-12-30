using UnityEngine;
using UnityEngine.UI;

public class TroopSlot : MonoBehaviour
{
    public Image background;
    public Image iconImage;

    private TroopData troopData;
    private ShopManager shopManager;

    private Color normalColor = Color.gray;
    private Color selectedColor = Color.yellow;

    public void Init(TroopData data, ShopManager manager)
    {
        troopData = data;
        shopManager = manager;

        iconImage.sprite = troopData.icon;
        SetSelected(false);
    }

    public void OnClick()
    {
        shopManager.SelectTroop(this, troopData);
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
    }
}
