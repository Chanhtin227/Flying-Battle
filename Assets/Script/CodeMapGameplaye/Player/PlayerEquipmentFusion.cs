
using System;
using System.Collections.Generic;
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

        [Header("Balo Only - Explicit Model Slot")]
        [Tooltip("0 = auto (old array order), 1 = Backpack 1, 2 = Backpack 2")]
        [Range(0, 2)] public int baloSlot = 0;

        [Header("3D Model on Player Prefab")]
        public GameObject equipmentObject;

        [Header("Skin Only")]
        public Material skinMaterial;
    }

    // =====================================================
    // INSPECTOR
    // =====================================================

    [Header("Equipment on Player Map")]
    public EquipmentEntry[] equipmentItems;

    [Header("Two Jetpack Models")]
    public GameObject baloModel1;
    public GameObject baloModel2;

    [Header("Skin Body Renderers")]
    public Renderer[] bodyRenderers;

    [Header("3D Equipment Settings")]
    public bool forceEnableEquipmentRenderers = true;
    public bool activateInactiveMeshChildren = false;

    [Header("Debug")]
    public bool showDebugLogs = true;

    // =====================================================
    // NETWORKED EQUIPMENT
    // =====================================================

    [Networked]
    public int HelmetID { get; set; }

    [Networked]
    public int BaloID { get; set; }

    [Networked]
    public int ArmorID { get; set; }

    [Networked]
    public int SkinID { get; set; }

    // =====================================================
    // SHOP PLAYER PREFS
    // =====================================================

    private const string EquipPrefix =
        "FlyingBattle_ShopEquipped_";

    private const string OwnedPrefix =
        "FlyingBattle_ShopOwned_";

    private const string UnequippedValue = "__NONE__";

    // =====================================================
    // CACHE
    // =====================================================

    private Material[][] originalMaterials;

    private int lastHelmet = -1;
    private int lastBalo = -1;
    private int lastArmor = -1;
    private int lastSkin = -1;

    [Header("Backpack Visual Safety")]
    [Tooltip("Periodically restore the correct backpack if another script enables the old model.")]
    public bool enforceBaloVisibility = true;
    [Min(0.1f)] public float baloVisibilityCheckInterval = 0.35f;
    private float nextBaloVisibilityCheck;
    private int lastLoggedBaloSlot = int.MinValue;

    // =====================================================
    // AWAKE
    // =====================================================

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

    // =====================================================
    // FUSION SPAWNED
    // =====================================================

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
        if (showDebugLogs)
            Debug.Log("[Equipment] Shop saved Balo itemID='" +
                PlayerPrefs.GetString(EquipPrefix + EquipmentCategory.Balo, "") +
                "' | Equipment index=" + balo, this);

        int armor = LoadSavedID(
            EquipmentCategory.Armor
        );

        int skin = LoadSavedID(
            EquipmentCategory.Skin
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[Equipment] LOCAL DATA" +
                " | Helmet=" + helmet +
                " | Balo=" + balo +
                " | Armor=" + armor +
                " | Skin=" + skin,
                this
            );
        }

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

    // =====================================================
    // LOAD EQUIPPED ITEM FROM SHOP
    // =====================================================

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

        // Ngan trang bi item chua duoc so huu.
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
            EquipmentEntry entry =
                equipmentItems[i];

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
                        " | Item=" + id +
                        " | Network Index=" + (i + 1),
                        this
                    );
                }

                return i + 1;
            }
        }

        Debug.LogWarning(
            "[Equipment] Khong tim thay Item ID: " +
            id +
            " | Category=" + category +
            ". Kiem tra Equipment Items.",
            this
        );

        return 0;
    }

    // =====================================================
    // RPC
    // =====================================================

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

    // =====================================================
    // SET NETWORK EQUIPMENT
    // =====================================================

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
                " | BaloSlot=" + GetEquippedBaloSlot() +
                " | Armor=" + ArmorID +
                " | Skin=" + SkinID,
                this
            );
        }
    }

    // =====================================================
    // VALIDATE EQUIPMENT INDEX
    // =====================================================

    private int ValidateID(
        int index,
        EquipmentCategory category)
    {
        if (index <= 0 ||
            equipmentItems == null ||
            index > equipmentItems.Length)
        {
            return 0;
        }

        EquipmentEntry entry =
            equipmentItems[index - 1];

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

    // =====================================================
    // NETWORK RENDER
    // =====================================================

    public override void Render()
    {
        if (lastHelmet != HelmetID ||
            lastBalo != BaloID ||
            lastArmor != ArmorID ||
            lastSkin != SkinID)
        {
            ApplyAllEquipment();
        }

        // Another script may re-enable a backpack after our initial Spawned().
        // Recheck only the backpack models, not all equipment/skins.
        if (enforceBaloVisibility && Time.time >= nextBaloVisibilityCheck)
        {
            nextBaloVisibilityCheck = Time.time +
                Mathf.Max(0.1f, baloVisibilityCheckInterval);
            ApplyBackpackModels(false);
        }
    }

    // =====================================================
    // APPLY ALL EQUIPMENT
    // =====================================================

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

    // =====================================================
    // APPLY EQUIPMENT CATEGORY
    // =====================================================

    private void ApplyCategory(
        EquipmentCategory category,
        int selectedID)
    {
        // Only one controller should toggle backpack models.
        // The generic equipment loop must not re-enable an old backpack.
        if (category == EquipmentCategory.Balo)
        {
            ApplyBackpackModels();
            return;
        }

        if (equipmentItems == null)
            return;

        for (int i = 0; i < equipmentItems.Length; i++)
        {
            EquipmentEntry entry = equipmentItems[i];
            if (entry == null || entry.category != category)
                continue;

            bool shouldShow = (i + 1) == selectedID;
            GameObject model = ResolveEquipmentObject(entry, i);

            if (model == null)
            {
                if (shouldShow)
                    Debug.LogWarning("[Equipment] Missing model: " + entry.itemID, this);
                continue;
            }

            model.SetActive(shouldShow);
            if (shouldShow)
                EnableSelectedRenderers(model);
        }
    }

    private void EnableSelectedRenderers(GameObject model)
    {
        if (!forceEnableEquipmentRenderers || model == null)
            return;

        Renderer[] renderers = model.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null) continue;
            if (activateInactiveMeshChildren)
            {
                Transform child = renderer.transform;
                while (child != null && child != model.transform)
                {
                    child.gameObject.SetActive(true);
                    child = child.parent;
                }
            }
            renderer.enabled = true;
        }
    }

    // =====================================================
    // GET EQUIPPED BACKPACK SLOT
    // =====================================================

    // BaloID la index trong toan bo equipmentItems.
    // Ham nay tra ve thu tu Balo: 0 / 1 / 2.
    public int GetEquippedBaloSlot()
    {
        if (BaloID <= 0 || equipmentItems == null ||
            BaloID > equipmentItems.Length)
            return 0;

        int selectedIndex = BaloID - 1;
        EquipmentEntry selected = equipmentItems[selectedIndex];

        if (selected == null || selected.category != EquipmentCategory.Balo)
            return 0;

        // Explicit slot is independent of array order.
        if (selected.baloSlot == 1 || selected.baloSlot == 2)
            return selected.baloSlot;

        // Backward-compatible fallback for older prefab configuration.
        int ordinal = 0;
        for (int i = 0; i <= selectedIndex; i++)
        {
            EquipmentEntry entry = equipmentItems[i];
            if (entry != null && entry.category == EquipmentCategory.Balo)
                ordinal++;
        }
        return ordinal;
    }

    // =====================================================
    // GET EQUIPPED BACKPACK MODEL
    // =====================================================

    public GameObject GetEquippedBaloModel()
    {
        int slot = GetEquippedBaloSlot();
        if (slot == 1 && IsSafeModel(baloModel1)) return baloModel1;
        if (slot == 2 && IsSafeModel(baloModel2)) return baloModel2;
        return null;
    }

    // =====================================================
    // APPLY BACKPACK MODELS - SHOP OBJECTS + JETPACK VISUALS
    // =====================================================

    // A single balo can have two separate scene objects:
    // equipmentItems[].equipmentObject (the original Shop model),
    // and baloModel1/2 (the jetpack model with thrusters).
    // Both must obey the same slot. Never toggle asset prefabs.
    private void ApplyBackpackModels()
    {
        ApplyBackpackModels(true);
    }

    private void ApplyBackpackModels(bool logChanges)
    {
        int selectedSlot = GetEquippedBaloSlot();
        Dictionary<GameObject, int> models = new Dictionary<GameObject, int>();

        // Main models are the objects containing the rocket engines.
        AddBaloModel(models, baloModel1, 1, "Jetpack Balo 1");
        AddBaloModel(models, baloModel2, 2, "Jetpack Balo 2");

        // Equipment Items may point to different visual objects (or prefab assets).
        if (equipmentItems != null)
        {
            for (int i = 0; i < equipmentItems.Length; i++)
            {
                EquipmentEntry entry = equipmentItems[i];
                if (entry == null || entry.category != EquipmentCategory.Balo)
                    continue;

                int slot = GetBaloSlotForEntry(i);
                // ResolveEquipmentObject never directly changes prefab assets.
                AddBaloModel(models, ResolveEquipmentObject(entry, i),
                    slot, "Equipment Item '" + entry.itemID + "'");
            }
        }

        // Avoid disabling a parent shared by models of different slots.
        HashSet<GameObject> sharedParents = new HashSet<GameObject>();
        foreach (KeyValuePair<GameObject, int> parent in models)
        {
            foreach (KeyValuePair<GameObject, int> child in models)
            {
                if (parent.Value == child.Value) continue;
                if (child.Key.transform.IsChildOf(parent.Key.transform))
                    sharedParents.Add(parent.Key);
            }
        }

        // A parent from the inactive slot can contain the selected model.
        // In that case leave the parent active; switch its renderer branches instead.
        List<GameObject> roots = new List<GameObject>(models.Keys);
        roots.Sort((a, b) => GetHierarchyDepth(a.transform)
            .CompareTo(GetHierarchyDepth(b.transform)));

        bool changed = false;
        foreach (GameObject model in roots)
        {
            if (sharedParents.Contains(model)) continue;
            bool visible = selectedSlot != 0 && models[model] == selectedSlot;

            // A container that holds the selected model must stay enabled.
            if (!visible)
            {
                foreach (GameObject possibleChild in roots)
                {
                    if (models[possibleChild] == selectedSlot &&
                        possibleChild != model &&
                        possibleChild.transform.IsChildOf(model.transform))
                    {
                        visible = true;
                        break;
                    }
                }
            }

            if (model.activeSelf != visible)
            {
                model.SetActive(visible);
                changed = true;
            }
            if (visible && models[model] == selectedSlot)
                EnableSelectedRenderers(model);
        }

        if (logChanges || changed || lastLoggedBaloSlot != selectedSlot)
        {
            lastLoggedBaloSlot = selectedSlot;
            if (showDebugLogs)
            {
                string item = BaloID > 0 && equipmentItems != null &&
                    BaloID <= equipmentItems.Length && equipmentItems[BaloID - 1] != null
                    ? equipmentItems[BaloID - 1].itemID : "NONE";
                Debug.Log("[Equipment] APPLY BACKPACK | Item=" + item +
                    " | BaloID=" + BaloID + " | Slot=" + selectedSlot +
                    " | Jetpack1=" + ModelStatus(baloModel1) +
                    " | Jetpack2=" + ModelStatus(baloModel2) +
                    " | Visual1=" + CountBaloModels(models, 1) +
                    " | Visual2=" + CountBaloModels(models, 2), this);
            }
        }
    }

    private string ModelStatus(GameObject model)
    {
        return model == null ? "NULL" :
            model.name + "(self=" + model.activeSelf +
            ",hierarchy=" + model.activeInHierarchy + ")";
    }

    private int GetBaloSlotForEntry(int index)
    {
        if (equipmentItems == null || index < 0 ||
            index >= equipmentItems.Length) return 0;
        EquipmentEntry entry = equipmentItems[index];
        if (entry == null || entry.category != EquipmentCategory.Balo)
            return 0;
        if (entry.baloSlot > 0) return entry.baloSlot;
        int ordinal = 0;
        for (int i = 0; i <= index; i++)
        {
            EquipmentEntry candidate = equipmentItems[i];
            if (candidate != null && candidate.category == EquipmentCategory.Balo)
                ordinal++;
        }
        return ordinal;
    }

    private void AddBaloModel(Dictionary<GameObject, int> models,
        GameObject model, int slot, string source)
    {
        if (model == null || slot <= 0) return;
        if (!IsSafeModel(model))
        {
            if (showDebugLogs)
                Debug.LogWarning("[Equipment] Cannot toggle " + source +
                    " ('" + model.name + "'). Assign a scene child that " +
                    "does not contain JetpackVFX/Player.", this);
            return;
        }
        int existingSlot;
        if (models.TryGetValue(model, out existingSlot))
        {
            if (existingSlot != slot)
                Debug.LogError("[Equipment] SAME model '" + model.name +
                    "' mapped to balo slots " + existingSlot + " and " +
                    slot + " (" + source + "). Fix Inspector references.", this);
            return;
        }
        models.Add(model, slot);
    }

    private int GetHierarchyDepth(Transform t)
    {
        int depth = 0;
        while (t != null) { depth++; t = t.parent; }
        return depth;
    }

    private int CountBaloModels(Dictionary<GameObject, int> models, int slot)
    {
        int count = 0;
        foreach (int value in models.Values)
            if (value == slot) count++;
        return count;
    }

    // =====================================================
    // SAFE MODEL CHECK
    // =====================================================

    private bool IsSafeModel(GameObject obj)
    {
        if (obj == null)
            return false;

        // Chi chap nhan model con cua Player.
        if (obj.transform == transform ||
            !obj.transform.IsChildOf(transform))
        {
            return false;
        }

        // Tuyet doi khong tat GameObject
        // chua JetpackVFX.
        JetpackVFX[] effects =
            GetComponentsInChildren<JetpackVFX>(true);

        foreach (JetpackVFX effect in effects)
        {
            if (effect == null)
                continue;

            if (effect.transform == obj.transform ||
                effect.transform.IsChildOf(obj.transform))
            {
                return false;
            }
        }

        return true;
    }

    // =====================================================
    // RESOLVE EQUIPMENT MODEL
    // =====================================================

    private GameObject ResolveEquipmentObject(
        EquipmentEntry entry,
        int arrayIndex)
    {
        if (entry == null)
            return null;

        if (IsSafeModel(entry.equipmentObject))
        {
            return entry.equipmentObject;
        }

        // Neu reference la prefab ngoai,
        // tim child trung ten trong Player.
        if (entry.equipmentObject != null)
        {
            string wanted =
                entry.equipmentObject.name
                    .Replace("(Clone)", "")
                    .Trim();

            GameObject found = null;

            Transform[] children =
                GetComponentsInChildren<Transform>(true);

            foreach (Transform child in children)
            {
                if (child == transform)
                    continue;

                string childName =
                    child.name
                        .Replace("(Clone)", "")
                        .Trim();

                if (!string.Equals(
                    childName,
                    wanted,
                    StringComparison.Ordinal))
                {
                    continue;
                }

                if (!IsSafeModel(child.gameObject))
                    continue;

                if (found != null)
                {
                    // Khong tu doan neu trung ten.
                    found = null;
                    break;
                }

                found = child.gameObject;
            }

            if (found != null)
                return found;
        }

        // Du phong cho 2 balo dau tien.
        if (entry.category == EquipmentCategory.Balo &&
            equipmentItems != null)
        {
            int slot = GetBaloSlotForEntry(arrayIndex);
            GameObject fallback =
                slot == 1 ? baloModel1 :
                slot == 2 ? baloModel2 :
                null;

            if (IsSafeModel(fallback))
                return fallback;
        }

        return null;
    }

    // =====================================================
    // APPLY SKIN
    // =====================================================

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
                entry.category == EquipmentCategory.Skin)
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

    // =====================================================
    // DEBUG - CHECK SAVED DATA
    // =====================================================

    [ContextMenu(
        "TEST - Check Player Equipment and Saved Data"
    )]
    public void CheckSavedEquipment()
    {
        foreach (
            EquipmentCategory category
            in Enum.GetValues(
                typeof(EquipmentCategory)
            ))
        {
            string id =
                PlayerPrefs.GetString(
                    EquipPrefix + category,
                    ""
                );

            int owned =
                string.IsNullOrEmpty(id)
                ? 0
                : PlayerPrefs.GetInt(
                    OwnedPrefix + id,
                    0
                );

            Debug.Log(
                "[Equipment CHECK] " +
                category +
                " | Saved=" + id +
                " | Owned=" + owned,
                this
            );
        }

        Debug.Log(
            "[Equipment CHECK] NETWORK" +
            " | Helmet=" + HelmetID +
            " | Balo=" + BaloID +
            " | BaloSlot=" + GetEquippedBaloSlot() +
            " | Armor=" + ArmorID +
            " | Skin=" + SkinID,
            this
        );

        CheckEquipmentSetup();
    }

    // =====================================================
    // DEBUG - CHECK INSPECTOR
    // =====================================================

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
                "[Equipment CHECK]" +
                " | Index=" + (i + 1) +
                " | ID=" + entry.itemID +
                " | Category=" + entry.category +
                " | BaloSlot=" + entry.baloSlot +
                " | Object=" +
                (entry.equipmentObject != null
                    ? entry.equipmentObject.name
                    : "NONE"),
                this
            );
        }

        Debug.Log(
            "[Equipment CHECK] BaloModel1=" +
            (baloModel1 != null
                ? baloModel1.name
                : "NONE") +
            " | BaloModel2=" +
            (baloModel2 != null
                ? baloModel2.name
                : "NONE"),
            this
        );
    }
}
