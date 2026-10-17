using UnityEngine;

// ==========================================================
// SNIPER SCOPE UI CONTROLLER
// ==========================================================
// Gan vao SniperScopeManager trong Canvas Map_True1.
//
// ThirdPersonCamera cua Player local se tu tim script nay
// va goi SetScope().
//
// RIFLE + RMB  -> Hien Scope.
// PISTOL + RMB -> Aim binh thuong, khong Scope.
//
// Khong can NetworkBehaviour hay RPC.
// Hoat dong trong Unity Editor va Windows Build.
// ==========================================================

public class ScopeUIController : MonoBehaviour
{
    // ======================================================
    // REFERENCES
    // ======================================================

    [Header("Scope UI")]
    public GameObject sniperScopeUI;

    [Header("Normal Crosshair - Outside ScopeUI")]
    public GameObject normalCrosshair;

    [Header("Optional HUD")]
    public GameObject[] hideWhileScoped;

    // ======================================================
    // LEGACY INSPECTOR SETTINGS
    // ======================================================

    [Header("Compatibility Settings")]

    [Tooltip(
        "Khong tu tim Player nua. ThirdPersonCamera se dieu khien Scope."
    )]
    public bool autoFindLocalPlayer = false;

    [Min(0.1f)]
    public float searchInterval = 0.5f;

    // ======================================================
    // DEBUG
    // ======================================================

    [Header("Build Debug")]
    public bool showDebugLogs = true;

    [Tooltip(
        "Chi dung de test UI trong ban Build. " +
        "Khong dung dong thoi voi camera khi phat hanh."
    )]
    public bool enableF9ScopeTest = false;

    // ======================================================
    // SCOPE RECOIL - LOCAL VISUAL ONLY
    // ======================================================

    [Header("Scope Recoil")]
    [Tooltip("Keo RectTransform cua Source Image (con cua ScopeUI). Khong keo Canvas.")]
    public RectTransform scopeRecoilRect;

    [Min(0f)] public float recoilUp = 12f;
    [Min(0f)] public float recoilSide = 3f;
    [Min(0f)] public float recoilRotation = 1f;
    [Min(0.1f)] public float recoilReturnSpeed = 18f;

    private Vector2 recoilBasePosition;
    private Quaternion recoilBaseRotation;
    private Vector2 recoilOffset;
    private float recoilAngle;
    private bool recoilInitialized;

    private void InitializeRecoil()
    {
        if (scopeRecoilRect == null && sniperScopeUI != null)
            scopeRecoilRect = sniperScopeUI.GetComponent<RectTransform>();

        if (scopeRecoilRect == null || recoilInitialized)
            return;

        recoilBasePosition = scopeRecoilRect.anchoredPosition;
        recoilBaseRotation = scopeRecoilRect.localRotation;
        recoilInitialized = true;
    }

    private void ResetRecoil()
    {
        recoilOffset = Vector2.zero;
        recoilAngle = 0f;
        if (!recoilInitialized || scopeRecoilRect == null) return;

        scopeRecoilRect.anchoredPosition = recoilBasePosition;
        scopeRecoilRect.localRotation = recoilBaseRotation;
    }

    // Duoc goi tu RPC hieu ung ban cua Rifle da duoc server xac nhan.
    // Khong anh huong huong dan, camera hay network state.
    public void PlayScopeRecoil()
    {
        if (!isScoped || sniperScopeUI == null || !sniperScopeUI.activeInHierarchy)
            return;

        InitializeRecoil();
        if (scopeRecoilRect == null) return;

        recoilOffset.x = Mathf.Clamp(
            recoilOffset.x + Random.Range(-recoilSide, recoilSide),
            -recoilSide * 2f, recoilSide * 2f);
        recoilOffset.y = Mathf.Clamp(
            recoilOffset.y + recoilUp, 0f, recoilUp * 2f);
        recoilAngle = Mathf.Clamp(
            recoilAngle + Random.Range(-recoilRotation, recoilRotation),
            -recoilRotation * 2f, recoilRotation * 2f);

        ApplyRecoilTransform();
    }

    private void ApplyRecoilTransform()
    {
        if (!recoilInitialized || scopeRecoilRect == null) return;
        scopeRecoilRect.anchoredPosition = recoilBasePosition + recoilOffset;
        scopeRecoilRect.localRotation = recoilBaseRotation *
            Quaternion.Euler(0f, 0f, recoilAngle);
    }

    // ======================================================
    // PRIVATE
    // ======================================================

    private bool isScoped;

    private bool crosshairWasActive;
    private bool crosshairSaved;

    private bool[] hudWasActive;

    private bool warnedMissingUI;

    // ======================================================
    // UNITY
    // ======================================================

    private void Awake()
    {
        isScoped = false;

        InitializeRecoil();
        ResetRecoil();
        ValidateReferences();

        if (sniperScopeUI != null)
        {
            sniperScopeUI.SetActive(false);
        }
        else
        {
            Debug.LogError(
                "[SCOPE] Chua gan Sniper Scope UI!",
                this
            );
        }
    }

    private void Start()
    {
        if (showDebugLogs)
        {
            Debug.Log(
                "[SCOPE] SniperScopeManager ready.",
                this
            );
        }
    }

    // Chi dung khi bat che do test.
    // Khi choi binh thuong, KHONG doc RMB o day.
    private void LateUpdate()
    {
        if (!enableF9ScopeTest)
            return;

        if (Input.GetKey(KeyCode.F9))
        {
            SetScope(true);
        }
    }

    private void Update()
    {
        if (!isScoped || !recoilInitialized || scopeRecoilRect == null)
            return;

        float t = 1f - Mathf.Exp(-recoilReturnSpeed * Time.unscaledDeltaTime);
        recoilOffset = Vector2.Lerp(recoilOffset, Vector2.zero, t);
        recoilAngle = Mathf.Lerp(recoilAngle, 0f, t);
        ApplyRecoilTransform();
    }

    // ======================================================
    // VALIDATE REFERENCES
    // ======================================================

    private void ValidateReferences()
    {
        if (normalCrosshair == null ||
            sniperScopeUI == null)
        {
            return;
        }

        // Normal Crosshair khong duoc la chinh
        // ScopeUI hoac mot object nam trong ScopeUI.
        if (!IsSafeUIReference(normalCrosshair))
        {
            Debug.LogWarning(
                "[SCOPE] Normal Crosshair dang gan sai. " +
                "Khong duoc gan Source Image cua ScopeUI. " +
                "Da tu dong bo qua tham chieu.",
                this
            );

            normalCrosshair = null;
        }
    }

    // ======================================================
    // SET SCOPE
    // ======================================================

    public void SetScope(bool active)
    {
        if (sniperScopeUI == null)
        {
            if (!warnedMissingUI)
            {
                Debug.LogError(
                    "[SCOPE] Sniper Scope UI = None! " +
                    "Hay gan ScopeUI trong Inspector.",
                    this
                );

                warnedMissingUI = true;
            }

            return;
        }

        // Khong xu ly lap lai neu trang thai khong doi.
        if (isScoped == active)
        {
            if (sniperScopeUI.activeSelf != active)
            {
                sniperScopeUI.SetActive(active);
            }

            return;
        }

        // ==================================================
        // SHOW
        // ==================================================

        if (active)
        {
            InitializeRecoil();
            ResetRecoil();
            HideNormalCrosshair();
            HideOptionalHUD();
        }

        // ==================================================
        // HIDE
        // ==================================================

        else
        {
            ResetRecoil();
            RestoreNormalCrosshair();
            RestoreOptionalHUD();
        }

        isScoped = active;

        sniperScopeUI.SetActive(active);

        if (showDebugLogs)
        {
            Debug.Log(
                "[SCOPE] " +
                (active ? "ON" : "OFF") +
                " | ActiveSelf=" +
                sniperScopeUI.activeSelf +
                " | ActiveInHierarchy=" +
                sniperScopeUI.activeInHierarchy,
                this
            );
        }

        // Neu activeSelf true nhung khong hien,
        // nghia la mot GameObject cha dang inactive.
        if (active && !sniperScopeUI.activeInHierarchy)
        {
            Debug.LogWarning(
                "[SCOPE] ScopeUI da bat nhung " +
                "GameObject cha dang bi tat!",
                this
            );
        }
    }

    // ======================================================
    // NORMAL CROSSHAIR
    // ======================================================

    private void HideNormalCrosshair()
    {
        if (!IsSafeUIReference(normalCrosshair))
            return;

        crosshairWasActive =
            normalCrosshair.activeSelf;

        crosshairSaved = true;

        normalCrosshair.SetActive(false);
    }

    private void RestoreNormalCrosshair()
    {
        if (!crosshairSaved)
            return;

        if (IsSafeUIReference(normalCrosshair))
        {
            normalCrosshair.SetActive(
                crosshairWasActive
            );
        }

        crosshairSaved = false;
    }

    // ======================================================
    // OPTIONAL HUD
    // ======================================================

    private void HideOptionalHUD()
    {
        if (hideWhileScoped == null)
            return;

        hudWasActive =
            new bool[hideWhileScoped.Length];

        for (int i = 0;
             i < hideWhileScoped.Length;
             i++)
        {
            GameObject hud =
                hideWhileScoped[i];

            if (!IsSafeUIReference(hud))
                continue;

            hudWasActive[i] = hud.activeSelf;

            hud.SetActive(false);
        }
    }

    private void RestoreOptionalHUD()
    {
        if (hideWhileScoped == null ||
            hudWasActive == null)
        {
            return;
        }

        int count = Mathf.Min(
            hideWhileScoped.Length,
            hudWasActive.Length
        );

        for (int i = 0; i < count; i++)
        {
            GameObject hud =
                hideWhileScoped[i];

            if (!IsSafeUIReference(hud))
                continue;

            hud.SetActive(hudWasActive[i]);
        }

        hudWasActive = null;
    }

    // ======================================================
    // SAFE UI REFERENCES
    // ======================================================

    private bool IsSafeUIReference(GameObject obj)
    {
        if (obj == null ||
            obj == gameObject ||
            obj == sniperScopeUI)
        {
            return false;
        }

        Transform objTransform = obj.transform;

        // Khong tat con cua ScopeUI.
        // Khong tat cha chua ScopeUI.
        if (sniperScopeUI != null)
        {
            Transform scopeTransform =
                sniperScopeUI.transform;

            if (objTransform.IsChildOf(scopeTransform) ||
                scopeTransform.IsChildOf(objTransform))
            {
                return false;
            }
        }

        // Khong tat SniperScopeManager hoac cha cua no.
        if (transform.IsChildOf(objTransform) ||
            objTransform.IsChildOf(transform))
        {
            return false;
        }

        return true;
    }

    // ======================================================
    // COMPATIBILITY
    // ======================================================

    public void RefreshScope(
        bool aiming,
        bool rifleEquipped)
    {
        SetScope(
            aiming && rifleEquipped
        );
    }

    // ======================================================
    // TEST IN UNITY EDITOR
    // ======================================================

    [ContextMenu("TEST - Show Scope")]
    public void TestShowScope()
    {
        SetScope(true);
    }

    [ContextMenu("TEST - Hide Scope")]
    public void TestHideScope()
    {
        SetScope(false);
    }

    // ======================================================
    // DISABLE
    // ======================================================

    private void OnDisable()
    {
        if (isScoped)
        {
            SetScope(false);
        }
    }
}
