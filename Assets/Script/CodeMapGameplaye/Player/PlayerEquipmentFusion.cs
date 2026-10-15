
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

        [Header("3D Model on Player Prefab")]
        public GameObject equipmentObject;

        [Header("Skin Only")]
        public Material skinMaterial;
    }

    // ==========================================
    // INSPECTOR
    // ==========================================

    [Header("Equipment on Player Map")]
    public EquipmentEntry[] equipmentItems;

    [Header("Skin Body Renderers")]
    public Renderer[] bodyRenderers;

    [Header("3D Equipment Settings")]
    public bool forceEnableEquipmentRenderers = true;

    [Tooltip("Chi bat neu cac Mesh con cua trang bi bi tat. Khong bat toan bo xương Player.")]
    public bool activateInactiveMeshChildren = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // ==========================================
    // NETWORKED
    // ==========================================

    [Networked] public int HelmetID { get; set; }
    [Networked] public int BaloID { get; set; }
    [Networked] public int ArmorID { get; set; }
    [Networked] public int SkinID { get; set; }

    // ==========================================
    // PLAYER PREFS
    // ==========================================

    private const string EquipPrefix =
        "FlyingBattle_ShopEquipped_";

    private const string OwnedPrefix =
        "FlyingBattle_ShopOwned_";

    private const string UnequippedValue = "__NONE__";

    // ==========================================
    // CACHE
    // ==========================================

    private Material[][] originalMaterials;

    private int lastHelmet = -1;
    private int lastBalo = -1;
    private int lastArmor = -1;
    private int lastSkin = -1;

    // ==========================================
    // UNITY
    // ==========================================

    private void Awake()
    {
        if (bodyRenderers == null)
            bodyRenderers = new Renderer[0];

        originalMaterials =
            new Material[bodyRenderers.Length][];

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
            {
                originalMaterials[i] =
                    bodyRenderers[i].sharedMaterials;
            }
        }
    }

    // ==========================================
    // FUSION SPAWN
    // ==========================================

    public override void Spawned()
    {
        ApplyAllEquipment();

        if (!HasInputAuthority)
            return;

        int helmet = LoadSavedID(
            EquipmentCategory.Helmet
        );

        int balo = LoadSavedID(
            EquipmentCategory.Balo
        );

        int armor = LoadSavedID(
            EquipmentCategory.Armor
        );

        int skin = LoadSavedID(
            EquipmentCategory.Skin
        );

        Debug.Log(
            "[PlayerEquipmentFusion] LOCAL DATA" +
            " | Helmet=" + helmet +
            " | Balo=" + balo +
            " | Armor=" + armor +
            " | Skin=" + skin,
            this
        );

        if (HasStateAuthority)
        {
            SetEquipment(
                helmet,
                balo,
                armor,
                skin
            );
        }
        else
        {
            RPC_SendEquipment(
                helmet,
                balo,
                armor,
                skin
            );
        }
    }

    // ==========================================
    // LOAD SHOP EQUIPMENT
    // ==========================================

    private int LoadSavedID(
        EquipmentCategory category)
    {
        string id = PlayerPrefs.GetString(
            EquipPrefix + category,
            ""
        );

        if (string.IsNullOrEmpty(id) ||
            id == UnequippedValue)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[Equipment] Not equipped: " +
                    category,
                    this
                );
            }

            return 0;
        }

        if (PlayerPrefs.GetInt(
            OwnedPrefix + id, 0) != 1)
        {
            Debug.LogWarning(
                "[Equipment] Item not owned: " + id,
                this
            );

            return 0;
        }

        if (equipmentItems == null)
            return 0;

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry = equipmentItems[i];

            if (entry == null)
                continue;

            if (entry.category == category &&
                entry.itemID == id)
            {
                if (showDebugLogs)
                {
                    Debug.Log(
                        "[Equipment] FOUND " +
                        category +
                        " | ID=" + id +
                        " | Index=" + (i + 1),
                        this
                    );
                }

                return i + 1;
            }
        }

        Debug.LogWarning(
            "[Equipment] KHONG TIM THAY ITEM: " +
            id +
            " | Category=" + category +
            ". Kiem tra Item ID va Category " +
            "trong PlayerEquipmentFusion.",
            this
        );

        return 0;
    }

    // ==========================================
    // RPC
    // ==========================================

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
        SetEquipment(
            helmet,
            balo,
            armor,
            skin
        );
    }

    // ==========================================
    // SET NETWORK EQUIPMENT
    // ==========================================

    private void SetEquipment(
        int helmet,
        int balo,
        int armor,
        int skin)
    {
        if (!HasStateAuthority)
            return;

        HelmetID = ValidateID(
            helmet,
            EquipmentCategory.Helmet
        );

        BaloID = ValidateID(
            balo,
            EquipmentCategory.Balo
        );

        ArmorID = ValidateID(
            armor,
            EquipmentCategory.Armor
        );

        SkinID = ValidateID(
            skin,
            EquipmentCategory.Skin
        );

        ApplyAllEquipment();

        if (showDebugLogs)
        {
            Debug.Log(
                "[Equipment] NETWORK UPDATED" +
                " | Helmet=" + HelmetID +
                " | Balo=" + BaloID +
                " | Armor=" + ArmorID +
                " | Skin=" + SkinID,
                this
            );
        }
    }

    private int ValidateID(
        int index,
        EquipmentCategory category)
    {
        if (index <= 0)
            return 0;

        int arrayIndex = index - 1;

        if (equipmentItems == null ||
            arrayIndex >= equipmentItems.Length)
        {
            return 0;
        }

        EquipmentEntry entry =
            equipmentItems[arrayIndex];

        if (entry == null ||
            entry.category != category)
        {
            Debug.LogWarning(
                "[Equipment] Invalid index " +
                index +
                " for " + category,
                this
            );

            return 0;
        }

        return index;
    }

    // ==========================================
    // NETWORK RENDER
    // ==========================================

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

    // ==========================================
    // APPLY ALL EQUIPMENT
    // ==========================================

    private void ApplyAllEquipment()
    {
        ApplyCategory(
            EquipmentCategory.Helmet,
            HelmetID
        );

        ApplyCategory(
            EquipmentCategory.Balo,
            BaloID
        );

        ApplyCategory(
            EquipmentCategory.Armor,
            ArmorID
        );

        ApplySkin(SkinID);

        lastHelmet = HelmetID;
        lastBalo = BaloID;
        lastArmor = ArmorID;
        lastSkin = SkinID;
    }

    // ==========================================
    // SHOW 3D EQUIPMENT
    // ==========================================

    private void ApplyCategory(EquipmentCategory category, int selectedID)
    {
        if (equipmentItems == null) return;

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry = equipmentItems[i];
            if (entry == null || entry.category != category) continue;

            bool shouldShow = i + 1 == selectedID;
            GameObject model = ResolveEquipmentObject(entry);

            if (model == null)
            {
                if (shouldShow)
                    Debug.LogError("[Equipment] Selected " + entry.itemID +
                        " but its 3D model is missing from spawned Player. " +
                        "Assign a CHILD GameObject on the gameplay Player Prefab.", this);
                continue;
            }

            // Only toggle the individual equipment mesh, never the shared attach point.
            model.SetActive(shouldShow);
            if (!shouldShow) continue;

            Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
            if (forceEnableEquipmentRenderers)
            {
                foreach (Renderer renderer in renderers)
                {
                    if (renderer == null) continue;
                    if (activateInactiveMeshChildren)
                    {
                        Transform t = renderer.transform;
                        while (t != null && t != model.transform)
                        {
                            t.gameObject.SetActive(true);
                            t = t.parent;
                        }
                    }
                    renderer.enabled = true;
                }
            }

            if (!model.activeInHierarchy)
                Debug.LogWarning("[Equipment] " + entry.itemID +
                    " is inactive in hierarchy. Check parent/attach-point active state.", this);
            if (renderers.Length == 0)
                Debug.LogWarning("[Equipment] " + entry.itemID +
                    " has no MeshRenderer/SkinnedMeshRenderer.", this);

            if (showDebugLogs)
                Debug.Log("[Equipment] SHOW " + entry.itemID +
                    " | model=" + model.name +
                    " | active=" + model.activeInHierarchy +
                    " | renderers=" + renderers.Length +
                    " | networkIndex=" + selectedID, this);
        }
    }

    // Do not toggle a prefab asset or a model from SceneMain.
    // The equipped model must be a child of the current spawned Fusion Player.
    private GameObject ResolveEquipmentObject(EquipmentEntry entry)
    {
        if (entry.equipmentObject == null) return null;

        Transform referenced = entry.equipmentObject.transform;
        if (referenced.IsChildOf(transform) && referenced != transform)
            return entry.equipmentObject;

        // Recovery for a reference to a prefab/model outside the spawned player.
        string wanted = entry.equipmentObject.name.Replace("(Clone)", "").Trim();
        Transform[] children = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in children)
        {
            if (child == transform) continue;
            string candidate = child.name.Replace("(Clone)", "").Trim();
            if (string.Equals(candidate, wanted, StringComparison.Ordinal))
            {
                if (showDebugLogs)
                    Debug.LogWarning("[Equipment] " + entry.itemID +
                        " was linked outside the Player. Found child " +
                        child.name + " instead. Fix Inspector reference.", this);
                return child.gameObject;
            }
        }
        return null;
    }

    [ContextMenu("TEST - Check Player Equipment and Saved Data")]
    public void CheckSavedEquipment()
    {
        foreach (EquipmentCategory category in Enum.GetValues(typeof(EquipmentCategory)))
        {
            string id = PlayerPrefs.GetString(EquipPrefix + category, "");
            int owned = string.IsNullOrEmpty(id) ? 0 :
                PlayerPrefs.GetInt(OwnedPrefix + id, 0);
            Debug.Log("[Equipment CHECK] " + category +
                " saved=" + id + " owned=" + owned, this);
        }
        Debug.Log("[Equipment CHECK] network Helmet=" + HelmetID +
            " Balo=" + BaloID + " Armor=" + ArmorID +
            " Skin=" + SkinID + " inputAuthority=" + HasInputAuthority +
            " stateAuthority=" + HasStateAuthority, this);
        CheckEquipmentSetup();
    }

    // ==========================================
    // ==========================================
    // APPLY SKIN
    // ==========================================

    private void ApplySkin(int selectedID)
    {
        Material skin = null;
        int index = selectedID - 1;

        if (equipmentItems != null &&
            index >= 0 &&
            index < equipmentItems.Length)
        {
            EquipmentEntry entry =
                equipmentItems[index];

            if (entry != null &&
                entry.category ==
                EquipmentCategory.Skin)
            {
                skin = entry.skinMaterial;
            }
        }

        if (bodyRenderers == null)
            return;

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            Renderer renderer =
                bodyRenderers[i];

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

            Material[] materials =
                renderer.sharedMaterials;

            for (int j = 0; j < materials.Length; j++)
            {
                materials[j] = skin;
            }

            renderer.sharedMaterials = materials;
        }
    }

    // ==========================================
    // TEST CONFIGURATION
    // ==========================================

    [ContextMenu("TEST - Check Equipment Setup")]
    public void CheckEquipmentSetup()
    {
        if (equipmentItems == null)
        {
            Debug.LogWarning(
                "[Equipment] Equipment Items is NULL!",
                this
            );
            return;
        }

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry =
                equipmentItems[i];

            if (entry == null)
                continue;

            Debug.Log(
                "[Equipment CHECK] " +
                "Index=" + (i + 1) +
                " | ID=" + entry.itemID +
                " | Category=" + entry.category +
                " | Object=" +
                (entry.equipmentObject != null
                    ? entry.equipmentObject.name
                    : "NONE"),
                this
            );
        }
    }
}
