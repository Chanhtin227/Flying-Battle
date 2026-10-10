using UnityEngine;
using TMPro;

/// <summary>Gan vao GameObject UI o scene Main.</summary>
public class MainGoldUI : MonoBehaviour
{
    [Header("Gold Text (TMP)")]
    public TMP_Text goldText;

    [Header("Display")]
    public string prefix = "";

    private void OnEnable()
    {
        RefreshGold();
    }

    public void RefreshGold()
    {
        if (goldText != null)
            goldText.text = prefix + GoldWallet.GetGold().ToString("N0");
    }
}
