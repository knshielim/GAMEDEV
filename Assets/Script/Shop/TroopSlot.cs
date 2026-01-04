using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TroopSlot : MonoBehaviour
{
    public Image background;
    public Image iconImage;

    private TroopData troopData;
    private ShopManager shopManager;

    private Color normalColor = Color.gray;
    private Color selectedColor = Color.yellow;
    public TextMeshProUGUI priceText;

    

   public void Init(TroopData data, ShopManager shop)
    {
        troopData = data;
        shopManager = shop;

        iconImage.sprite = data.icon;

        // Make sure a TroopInstance exists
        TroopInstance instance = shopManager.GetOrCreateInstance(data);

        // Set price text
        UpdatePriceText(instance);
    }

    public void OnClick()
    {
        shopManager.SelectTroop(this, troopData);
    }

    public void SetSelected(bool selected)
    {
        background.color = selected ? selectedColor : normalColor;
    }
    private void UpdatePriceText(TroopInstance instance)
    {
        int cost = instance.GetUpgradeCost();
        if (cost < 0) // Max level or Boss
            priceText.text = "MAX";
        else
            priceText.text = cost.ToString();
    }

}
