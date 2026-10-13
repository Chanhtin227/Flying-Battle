
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Preview trang bi C01 trong Shop.
/// Helmet -> HeadGear
/// Balo   -> BackPack
/// Armor  -> Belly
/// Khong gan vao Photon Fusion Player.
/// </summary>
public class ShopEquipmentPreview : MonoBehaviour
{
    [Header("C01 Preview")]
    public Transform previewPlayer;
    public Camera previewCamera;
    public bool forceEquipmentToPlayerLayer = true;

    [Header("Equipment Attach Points")]
    public Transform helmetPoint;
    public Transform baloPoint;
    public Transform armorPoint;

    [Header("Original C01 Equipment")]
    public GameObject[] originalHelmets;
    public GameObject[] originalBalos;
    public GameObject[] originalArmors;

    [Header("Settings")]
    [Tooltip("Tu su dung model co san neu ten trung Prefab.")]
    public bool preferExistingEquipment = true;

    [Header("Skin - Body Renderers")]
    public Renderer[] bodyRenderers;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private GameObject currentHelmet;
    private GameObject currentBalo;
    private GameObject currentArmor;

    private Material[][] initialMaterials;

    private class EquipmentState
    {
        public bool active;
        public Transform parent;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;
    }

    private readonly Dictionary<GameObject, EquipmentState>
        initialStates = new Dictionary<GameObject, EquipmentState>();

    private readonly List<GameObject> helmets =
        new List<GameObject>();

    private readonly List<GameObject> balos =
        new List<GameObject>();

    private readonly List<GameObject> armors =
        new List<GameObject>();

    // ==========================================
    // UNITY
    // ==========================================

    private void Awake()
    {
        if (bodyRenderers == null)
            bodyRenderers = new Renderer[0];

        initialMaterials = new Material[bodyRenderers.Length][];

        for (int i = 0; i < bodyRenderers.Length; i++)
        {
            if (bodyRenderers[i] != null)
                initialMaterials[i] =
                    bodyRenderers[i].sharedMaterials;
        }

        RegisterArray(0, originalHelmets);
        RegisterArray(1, originalBalos);
        RegisterArray(2, originalArmors);
    }

    // ==========================================
    // REGISTER
    // ==========================================

    private void RegisterArray(int category, GameObject[] objects)
    {
        if (objects == null)
            return;

        foreach (GameObject obj in objects)
            RegisterExistingEquipment(category, obj);
    }

    public void RegisterExistingEquipment(int category, GameObject obj)
    {
        if (obj == null || category < 0 || category > 2)
            return;

        List<GameObject> group = GetGroup(category);

        if (!group.Contains(obj))
            group.Add(obj);

        if (!initialStates.ContainsKey(obj))
        {
            Transform t = obj.transform;

            initialStates.Add(obj, new EquipmentState
            {
                active = obj.activeSelf,
                parent = t.parent,
                position = t.localPosition,
                rotation = t.localRotation,
                scale = t.localScale
            });
        }
    }

    private List<GameObject> GetGroup(int category)
    {
        switch (category)
        {
            case 0: return helmets;
            case 1: return balos;
            default: return armors;
        }
    }

    // ==========================================
    // HELMET
    // ==========================================

    public void PreviewHelmet(GameObject prefab)
    {
        PreviewHelmet(prefab, null);
    }

    public void PreviewHelmet(GameObject prefab, GameObject existing)
    {
        ShowEquipment(
            0, prefab, existing,
            helmetPoint, ref currentHelmet
        );
    }

    // ==========================================
    // BALO
    // ==========================================

    public void PreviewBalo(GameObject prefab)
    {
        PreviewBalo(prefab, null);
    }

    public void PreviewBalo(GameObject prefab, GameObject existing)
    {
        ShowEquipment(
            1, prefab, existing,
            baloPoint, ref currentBalo
        );
    }

    // ==========================================
    // ARMOR
    // ==========================================

    public void PreviewArmor(GameObject prefab)
    {
        PreviewArmor(prefab, null);
    }

    public void PreviewArmor(GameObject prefab, GameObject existing)
    {
        ShowEquipment(
            2, prefab, existing,
            armorPoint, ref currentArmor
        );
    }

    // ==========================================
    // FIND EXISTING MODEL
    // ==========================================

    private GameObject FindExisting(int category, GameObject prefab)
    {
        if (prefab == null)
            return null;

        foreach (GameObject obj in GetGroup(category))
        {
            if (obj == null)
                continue;

            if (obj.name == prefab.name &&
                IsInsidePreview(obj.transform))
            {
                return obj;
            }
        }

        return null;
    }

    private bool IsInsidePreview(Transform t)
    {
        if (previewPlayer == null || t == null)
            return false;

        return t.IsChildOf(previewPlayer);
    }

    // ==========================================
    // SHOW EQUIPMENT
    // ==========================================

    private void ShowEquipment(
        int category,
        GameObject prefab,
        GameObject existing,
        Transform point,
        ref GameObject current)
    {
        string label =
            category == 0 ? "Helmet" :
            category == 1 ? "Balo" : "Armor";

        if (point == null)
        {
            Debug.LogError(
                "[ShopPreview] Missing " + label + " Point!",
                this
            );
            return;
        }

        // Uu tien trang bi co san tren C01.
        if (existing == null &&
            preferExistingEquipment &&
            prefab != null)
        {
            existing = FindExisting(category, prefab);
        }

        // ======================================
        // EXISTING EQUIPMENT
        // ======================================

        if (existing != null)
        {
            if (!IsInsidePreview(existing.transform))
            {
                Debug.LogError(
                    "[ShopPreview] " + existing.name +
                    " khong nam trong Preview Player C01!",
                    this
                );
                return;
            }

            if (existing.transform == point ||
                point.IsChildOf(existing.transform))
            {
                Debug.LogError(
                    "[ShopPreview] Khong the gan " +
                    existing.name + " vao " + point.name +
                    " vi tao quan he cha con sai.",
                    this
                );
                return;
            }

            RegisterExistingEquipment(category, existing);

            Remove(ref current);

            // Khoi phuc trang bi da thu truoc.
            RestoreGroup(category);

            Transform t = existing.transform;

            // Neu chua nam duoi Attach Point,
            // giu nguyen vi tri the gioi khi chuyen cha.
            // Khong reset Position, Rotation, Scale.
            if (!t.IsChildOf(point))
            {
                t.SetParent(point, true);
            }

            HideGroup(category, existing);

            existing.SetActive(true);

            if (!existing.activeInHierarchy)
            {
                Debug.LogWarning(
                    "[ShopPreview] " + existing.name +
                    " bi an boi GameObject cha.",
                    this
                );
            }

            if (showDebugLogs)
            {
                Debug.Log(
                    "[ShopPreview] SHOW EXISTING " +
                    label + ": " + existing.name +
                    " | Point: " + point.name +
                    " | Position: " + t.localPosition +
                    " | Rotation: " + t.localEulerAngles,
                    this
                );
            }

            return;
        }

        // ======================================
        // PREFAB EQUIPMENT
        // ======================================

        if (prefab == null)
        {
            Remove(ref current);
            RestoreGroup(category);

            Debug.LogWarning(
                "[ShopPreview] Chua gan trang bi " + label,
                this
            );
            return;
        }

        // Khong spawn neu Point nam trong model se bi an.
        foreach (GameObject obj in GetGroup(category))
        {
            if (obj == null)
                continue;

            if (point == obj.transform ||
                point.IsChildOf(obj.transform))
            {
                Debug.LogError(
                    "[ShopPreview] Point nam trong model goc " +
                    obj.name,
                    this
                );
                return;
            }
        }

        // Xoa Prefab preview cu.
        Remove(ref current);

        RestoreGroup(category);
        HideGroup(category, null);

        // Chi tao 1 model moi cho moi nhom.
        current = Instantiate(prefab, point, false);

        current.name =
            "Preview_" + label + "_" + prefab.name;

        // Can theo Attach Point.
        current.transform.localPosition = Vector3.zero;
        current.transform.localRotation = Quaternion.identity;
        current.transform.localScale = prefab.transform.localScale;

        current.SetActive(true);

        Renderer[] renderers =
            current.GetComponentsInChildren<Renderer>(true);

        foreach (Renderer r in renderers)
        {
            if (r != null)
                r.enabled = true;
        }

        if (forceEquipmentToPlayerLayer)
        {
            SetLayerRecursively(
                current.transform,
                point.gameObject.layer
            );
        }

        if (!current.activeInHierarchy)
        {
            Debug.LogWarning(
                "[ShopPreview] Prefab dang bi an boi cha: " +
                current.name,
                this
            );
        }

        if (renderers.Length == 0)
        {
            Debug.LogWarning(
                "[ShopPreview] Prefab khong co Renderer: " +
                prefab.name,
                this
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[ShopPreview] CREATED " + current.name +
                " at " + point.name,
                this
            );
        }
    }

    // ==========================================
    // HIDE / RESTORE
    // ==========================================

    private void HideGroup(int category, GameObject except)
    {
        foreach (GameObject obj in GetGroup(category))
        {
            if (obj != null && obj != except)
                obj.SetActive(false);
        }
    }

    private void RestoreGroup(int category)
    {
        foreach (GameObject obj in GetGroup(category))
        {
            if (obj == null)
                continue;

            if (!initialStates.TryGetValue(
                obj, out EquipmentState state))
                continue;

            Transform t = obj.transform;

            if (t.parent != state.parent)
                t.SetParent(state.parent, false);

            t.localPosition = state.position;
            t.localRotation = state.rotation;
            t.localScale = state.scale;

            obj.SetActive(state.active);
        }
    }

    // ==========================================
    // SKIN
    // ==========================================

    // ==========================================
    // SKIN - Doi Material ngay khi click item Skin
    // ==========================================

    public void PreviewSkin(Material material)
    {
        // Click bo chon Skin / reset ve Skin cu.
        if (material == null)
        {
            RestoreSkin();
            if (showDebugLogs)
                Debug.Log("[ShopPreview] Restored original Skin.", this);
            return;
        }

        if (bodyRenderers == null || bodyRenderers.Length == 0)
        {
            Debug.LogWarning(
                "[ShopPreview] Body Renderers trong C01 dang rong! " +
                "Hay keo Body09, Wing09, Tail09 vao Inspector.",
                this);
            return;
        }

        int changed = 0;

        foreach (Renderer renderer in bodyRenderers)
        {
            if (renderer == null)
                continue;

            // Dung sharedMaterials de tranh Unity tu tao
            // nhieu Material instance moi moi lan click Skin.
            Material[] materials = renderer.sharedMaterials;

            if (materials == null || materials.Length == 0)
            {
                Debug.LogWarning(
                    "[ShopPreview] Renderer khong co Material: " +
                    renderer.name, renderer);
                continue;
            }

            // Gan Skin Material cho tung material slot cua Renderer.
            // Khong sua truc tiep asset Material cu.
            for (int i = 0; i < materials.Length; i++)
                materials[i] = material;

            renderer.sharedMaterials = materials;
            changed++;
        }

        if (showDebugLogs)
            Debug.Log(
                "[ShopPreview] Skin changed to " + material.name +
                " | Renderers changed: " + changed, this);
    }

    private void RestoreSkin()
    {
        if (initialMaterials == null || bodyRenderers == null)
            return;

        for (int i = 0;
             i < bodyRenderers.Length && i < initialMaterials.Length;
             i++)
        {
            if (bodyRenderers[i] != null &&
                initialMaterials[i] != null)
            {
                bodyRenderers[i].sharedMaterials =
                    initialMaterials[i];
            }
        }
    }

    // ==========================================
    // RESET
    // ==========================================

    public void ResetPreview()
    {
        Remove(ref currentHelmet);
        Remove(ref currentBalo);
        Remove(ref currentArmor);

        RestoreGroup(0);
        RestoreGroup(1);
        RestoreGroup(2);

        RestoreSkin();

        if (showDebugLogs)
            Debug.Log("[ShopPreview] Reset completed.", this);
    }

    // ==========================================
    // DEBUG
    // ==========================================

    [ContextMenu("TEST - Check Preview Setup")]
    public void CheckPreviewSetup()
    {
        Debug.Log(
            "[ShopPreview] Player: " +
            (previewPlayer ? previewPlayer.name : "NULL") +
            " | Helmet: " +
            (helmetPoint ? helmetPoint.name : "NULL") +
            " | Balo: " +
            (baloPoint ? baloPoint.name : "NULL") +
            " | Armor: " +
            (armorPoint ? armorPoint.name : "NULL"),
            this
        );
    }

    // ==========================================
    // HELPERS
    // ==========================================

    private static void Remove(ref GameObject obj)
    {
        if (obj == null)
            return;

        obj.SetActive(false);
        Destroy(obj);
        obj = null;
    }

    private static void SetLayerRecursively(
        Transform root,
        int layer)
    {
        root.gameObject.layer = layer;

        foreach (Transform child in root)
            SetLayerRecursively(child, layer);
    }
}
