using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventorySlotUI : MonoBehaviour
{
    public Image itemIcon;
    public TMP_Text amountText;

    public void ClearSlot()
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }

        if (amountText != null)
        {
            amountText.text = "";
        }
    }

    public void SetItem(
        Sprite icon,
        int amount)
    {
        if (itemIcon != null)
        {
            itemIcon.sprite = icon;
            itemIcon.enabled = icon != null;
        }

        if (amountText != null)
        {
            amountText.text =
                "x" +
                Mathf.Max(0, amount);
        }
    }
}