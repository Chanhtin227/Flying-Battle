
using UnityEngine;
using UnityEngine.UI;
using Fusion;
using System;

public class EquipmentHUD : MonoBehaviour
{
    // =====================================================
    // ITEM ICON
    // =====================================================

    [Serializable]
    public class ItemIcon
    {
        public string itemID;
        public Sprite icon;
    }

    // =====================================================
    // UI REFERENCES
    // =====================================================

    [Header("Helmet UI")]
    public GameObject helmetSlot;
    public Image helmetIcon;

    [Header("Armor UI")]
    public GameObject armorSlot;
    public Image armorIcon;

    // =====================================================
    // ICON DATABASE
    // =====================================================

    [Header("Helmet Icons")]
    public ItemIcon[] helmetIcons;

    [Header("Armor Icons")]
    public ItemIcon[] armorIcons;

    // =====================================================
    // SETTINGS
    // =====================================================

    [Header("Update Settings")]
    public float refreshInterval = 0.2f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    // =====================================================
    // PRIVATE VARIABLES
    // =====================================================

    private PlayerEquipmentFusion localEquipment;

    private float nextRefreshTime;

    private int lastHelmetID = -1;
    private int lastArmorID = -1;

    // =====================================================
    // ON ENABLE
    // =====================================================

    private void OnEnable()
    {
        localEquipment = null;

        lastHelmetID = -1;
        lastArmorID = -1;

        nextRefreshTime = 0f;

        SetHelmet(null);
        SetArmor(null);
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (Time.unscaledTime < nextRefreshTime)
            return;

        nextRefreshTime =
            Time.unscaledTime +
            Mathf.Max(0.05f, refreshInterval);

        RefreshHUD();
    }

    // =====================================================
    // FIND LOCAL PLAYER
    // =====================================================

    private PlayerEquipmentFusion FindLocalPlayerEquipment()
    {
        PlayerEquipmentFusion[] allPlayers =
            FindObjectsByType<PlayerEquipmentFusion>(
                FindObjectsSortMode.None
            );

        foreach (PlayerEquipmentFusion player in allPlayers)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            if (!player.HasInputAuthority)
                continue;

            return player;
        }

        return null;
    }

    // =====================================================
    // REFRESH HUD
    // =====================================================

    public void RefreshHUD()
    {
        if (localEquipment == null ||
            localEquipment.Object == null ||
            !localEquipment.Object.IsValid ||
            !localEquipment.HasInputAuthority)
        {
            localEquipment = FindLocalPlayerEquipment();

            lastHelmetID = -1;
            lastArmorID = -1;
        }

        // Chua spawn Player local.
        if (localEquipment == null)
        {
            SetHelmet(null);
            SetArmor(null);
            return;
        }

        // Lay trang bi da dong bo.
        int helmetID = localEquipment.HelmetID;
        int armorID = localEquipment.ArmorID;

        // Khong cap nhat lai neu khong thay doi.
        if (helmetID == lastHelmetID &&
            armorID == lastArmorID)
        {
            return;
        }

        lastHelmetID = helmetID;
        lastArmorID = armorID;

        // Lay Item ID tu equipmentItems.
        string helmetItemID = GetItemID(helmetID);
        string armorItemID = GetItemID(armorID);

        Sprite helmetSprite =
            FindItemIcon(
                helmetIcons,
                helmetItemID
            );

        Sprite armorSprite =
            FindItemIcon(
                armorIcons,
                armorItemID
            );

        SetHelmet(helmetSprite);
        SetArmor(armorSprite);

        if (showDebugLogs)
        {
            Debug.Log(
                "[EquipmentHUD] HelmetID: " +
                helmetID +
                " | Helmet Item: " +
                helmetItemID +
                " | ArmorID: " +
                armorID +
                " | Armor Item: " +
                armorItemID,
                this
            );
        }
    }

    // =====================================================
    // GET EQUIPMENT ITEM ID
    // =====================================================

    private string GetItemID(int networkID)
    {
        if (localEquipment == null)
            return null;

        if (localEquipment.equipmentItems == null)
            return null;

        if (networkID <= 0)
            return null;

        int index = networkID - 1;

        if (index >= localEquipment.equipmentItems.Length)
            return null;

        var item =
            localEquipment.equipmentItems[index];

        if (item == null)
            return null;

        return item.itemID;
    }

    // =====================================================
    // FIND ICON
    // =====================================================

    private Sprite FindItemIcon(
        ItemIcon[] iconList,
        string targetItemID)
    {
        if (iconList == null)
            return null;

        if (string.IsNullOrEmpty(targetItemID))
            return null;

        foreach (ItemIcon item in iconList)
        {
            if (item == null)
                continue;

            if (string.IsNullOrEmpty(item.itemID))
                continue;

            if (string.Equals(
                item.itemID,
                targetItemID,
                StringComparison.Ordinal))
            {
                return item.icon;
            }
        }

        return null;
    }

    // =====================================================
    // SET HELMET UI
    // =====================================================

    private void SetHelmet(Sprite sprite)
    {
        bool hasHelmet = sprite != null;

        if (helmetIcon != null)
        {
            helmetIcon.sprite = sprite;
            helmetIcon.enabled = hasHelmet;
        }

        if (helmetSlot != null)
        {
            helmetSlot.SetActive(hasHelmet);
        }
    }

    // =====================================================
    // SET ARMOR UI
    // =====================================================

    private void SetArmor(Sprite sprite)
    {
        bool hasArmor = sprite != null;

        if (armorIcon != null)
        {
            armorIcon.sprite = sprite;
            armorIcon.enabled = hasArmor;
        }

        if (armorSlot != null)
        {
            armorSlot.SetActive(hasArmor);
        }
    }
}
