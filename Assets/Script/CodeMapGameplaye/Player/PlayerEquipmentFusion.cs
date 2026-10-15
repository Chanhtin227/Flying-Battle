using System;
using UnityEngine;
using Fusion;

public class PlayerEquipmentFusion : NetworkBehaviour
{
    public enum EquipmentCategory
    {
        Helmet,
        Balo,
        Armor,
        Skin
    }

    [Serializable]
    public class EquipmentEntry
    {
        public string itemID;
        public EquipmentCategory category;

        [Tooltip("Model co san tren Player Map. Khong dung prefab o Project.")]
        public GameObject equipmentObject;

        [Tooltip("Chi su dung cho Skin")]
        public Material skinMaterial;
    }

    [Header("All available equipment on Map Player")]
    public EquipmentEntry[] equipmentItems;

    [Header("Skin - Body Renderers")]
    public Renderer[] bodyRenderers;

    [Networked] public int HelmetID { get; set; }
    [Networked] public int BaloID { get; set; }
    [Networked] public int ArmorID { get; set; }
    [Networked] public int SkinID { get; set; }

    private const string EquipPrefix =
        "FlyingBattle_ShopEquipped_";

    private const string OwnedPrefix =
        "FlyingBattle_ShopOwned_";

    private const string UnequippedValue = "__NONE__";

    private Material[][] originalMaterials;

    private int lastHelmet = -1;
    private int lastBalo = -1;
    private int lastArmor = -1;
    private int lastSkin = -1;

    private void Awake()
    {
        if (bodyRenderers == null)
            bodyRenderers = new Renderer[0];

        originalMaterials = new Material[bodyRenderers.Length][];

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                originalMaterials[i] =
                    bodyRenderers[i].sharedMaterials;
        }
    }

    public override void Spawned()
    {
        // Mac trang bi theo du lieu network hien tai
        ApplyAllEquipment();

        if (HasInputAuthority)
        {
            // Moi may chi doc PlayerPrefs cua Player local.
            int helmet = LoadSavedID(EquipmentCategory.Helmet);
            int balo = LoadSavedID(EquipmentCategory.Balo);
            int armor = LoadSavedID(EquipmentCategory.Armor);
            int skin = LoadSavedID(EquipmentCategory.Skin);

            if (HasStateAuthority)
            {
                SetEquipment(helmet, balo, armor, skin);
            }
            else
            {
                RPC_SendEquipment(helmet, balo, armor, skin);
            }
        }
    }

    private int LoadSavedID(EquipmentCategory category)
    {
        string id = PlayerPrefs.GetString(
            EquipPrefix + category, ""
        );

        if (string.IsNullOrEmpty(id) ||
            id == UnequippedValue)
            return 0;

        if (PlayerPrefs.GetInt(OwnedPrefix + id, 0) != 1)
            return 0;

        if (equipmentItems == null)
            return 0;

        // Index + 1. So 0 nghia la khong trang bi.
        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry = equipmentItems[i];

            if (entry != null &&
                entry.category == category &&
                entry.itemID == id)
            {
                return i + 1;
            }
        }

        Debug.LogWarning(
            "[Equipment] Item ID chua duoc khai bao tren Map: " + id,
            this
        );

        return 0;
    }

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RPC_SendEquipment(
        int helmet,
        int balo,
        int armor,
        int skin)
    {
        SetEquipment(helmet, balo, armor, skin);
    }

    private void SetEquipment(
        int helmet,
        int balo,
        int armor,
        int skin)
    {
        if (!HasStateAuthority)
            return;

        HelmetID = ValidateID(helmet, EquipmentCategory.Helmet);
        BaloID = ValidateID(balo, EquipmentCategory.Balo);
        ArmorID = ValidateID(armor, EquipmentCategory.Armor);
        SkinID = ValidateID(skin, EquipmentCategory.Skin);

        ApplyAllEquipment();
    }

    private int ValidateID(int index, EquipmentCategory category)
    {
        if (index == 0)
            return 0;

        int arrayIndex = index - 1;

        if (equipmentItems == null ||
            arrayIndex < 0 ||
            arrayIndex >= equipmentItems.Length)
            return 0;

        EquipmentEntry entry = equipmentItems[arrayIndex];

        if (entry == null || entry.category != category)
            return 0;

        return index;
    }

    public override void Render()
    {
        if (lastHelmet != HelmetID ||
            lastBalo != BaloID ||
            lastArmor != ArmorID ||
            lastSkin != SkinID)
        {
            ApplyAllEquipment();
        }
    }

    private void ApplyAllEquipment()
    {
        ApplyCategory(EquipmentCategory.Helmet, HelmetID);
        ApplyCategory(EquipmentCategory.Balo, BaloID);
        ApplyCategory(EquipmentCategory.Armor, ArmorID);
        ApplySkin(SkinID);

        lastHelmet = HelmetID;
        lastBalo = BaloID;
        lastArmor = ArmorID;
        lastSkin = SkinID;
    }

    private void ApplyCategory(EquipmentCategory category, int selectedID)
    {
        if (equipmentItems == null)
            return;

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry = equipmentItems[i];

            if (entry == null ||
                entry.category != category ||
                entry.equipmentObject == null)
                continue;

            // Chi hien model duoc chon.
            entry.equipmentObject.SetActive(i + 1 == selectedID);
        }
    }

    private void ApplySkin(int selectedID)
    {
        Material skin = null;
        int index = selectedID - 1;

        if (equipmentItems != null &&
            index >= 0 &&
            index < equipmentItems.Length)
        {
            EquipmentEntry entry = equipmentItems[index];

            if (entry != null &&
                entry.category == EquipmentCategory.Skin)
                skin = entry.skinMaterial;
        }

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer renderer = bodyRenderers[i];

            if (renderer == null)
                continue;

            if (skin == null)
            {
                if (originalMaterials != null &&
                    i < originalMaterials.Length &&
                    originalMaterials[i] != null)
                {
                    renderer.sharedMaterials =
                        originalMaterials[i];
                }

                continue;
            }

            Material[] materials = renderer.sharedMaterials;

            for (int j = 0; j < materials.Length; j++)
                materials[j] = skin;

            renderer.sharedMaterials = materials;
        }
    }
}