
using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;

public class DamageNumberUI : MonoBehaviour
{
    // =====================================================
    // INSTANCE
    // =====================================================

    public static DamageNumberUI Instance { get; private set; }

    // =====================================================
    // UI REFERENCES
    // =====================================================

    [Header("UI References")]
    public Canvas canvas;
    public RectTransform popupRoot;
    public TMP_Text damageTextPrefab;

    // =====================================================
    // CAMERA SETTINGS
    // =====================================================

    [Header("Camera - Auto Find Local Player")]
    public bool autoFindLocalCamera = true;

    [Min(0.05f)]
    public float cameraSearchInterval = 0.5f;

    [Header("Debug")]
    public bool showCameraDebug = true;
    public bool testPopupWithF8 = false;

    // =====================================================
    // DAMAGE ANIMATION
    // =====================================================

    [Header("Animation")]
    [Min(0.1f)]
    public float duration = 0.8f;

    public float floatDistance = 65f;
    public float randomSideOffset = 25f;

    [Header("Scale Effect")]
    public float startScale = 0.6f;
    public float popScale = 1.25f;
    public float finalScale = 0.85f;
    public float popDuration = 0.15f;

    // =====================================================
    // DAMAGE COLOR
    // =====================================================

    [Header("Damage Color")]
    public Color normalDamageColor =
        new Color(1f, 0.82f, 0.22f, 1f);

    // =====================================================
    // PRIVATE
    // =====================================================

    private Camera localCamera;
    private PlayerMovement localPlayer;

    private float nextCameraSearchTime;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        Instance = this;

        if (canvas == null)
            canvas = GetComponentInParent<Canvas>();

        if (popupRoot == null)
            popupRoot = transform as RectTransform;
    }

    private void OnEnable()
    {
        nextCameraSearchTime = 0f;
    }

    private void Update()
    {
        // Bat testPopupWithF8 trong Inspector de kiem tra UI doc lap voi vu khi.
        if (testPopupWithF8 && Input.GetKeyDown(KeyCode.F8))
        {
            Camera cameraToTest = GetLocalCamera();
            if (cameraToTest != null)
            {
                ShowDamage(25f,
                    cameraToTest.transform.position +
                    cameraToTest.transform.forward * 8f);
            }
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // =====================================================
    // FIND LOCAL PLAYER
    // =====================================================

    private PlayerMovement FindLocalPlayer()
    {
        if (localPlayer != null &&
            localPlayer.Object != null &&
            localPlayer.Object.IsValid &&
            localPlayer.HasInputAuthority)
        {
            return localPlayer;
        }

        localPlayer = null;

        PlayerMovement[] players =
            FindObjectsByType<PlayerMovement>(
                FindObjectsSortMode.None
            );

        foreach (PlayerMovement player in players)
        {
            if (player == null ||
                player.Object == null ||
                !player.Object.IsValid)
            {
                continue;
            }

            if (!player.HasInputAuthority)
                continue;

            localPlayer = player;

            return localPlayer;
        }

        return null;
    }

    // =====================================================
    // CAMERA VALIDATION
    // =====================================================

    private bool IsUsableCamera(Camera cam)
    {
        return cam != null &&
               cam.isActiveAndEnabled &&
               cam.targetDisplay == 0;
    }

    // =====================================================
    // AUTO FIND CAMERA
    // =====================================================

    private Camera GetLocalCamera()
    {
        if (!autoFindLocalCamera)
            return null;

        // Camera da tim duoc va van hoat dong.
        if (IsUsableCamera(localCamera))
            return localCamera;

        if (Time.unscaledTime < nextCameraSearchTime)
            return null;

        nextCameraSearchTime =
            Time.unscaledTime +
            Mathf.Max(0.05f, cameraSearchInterval);

        // =============================================
        // STEP 1 - FIND LOCAL PLAYER
        // =============================================

        PlayerMovement player = FindLocalPlayer();

        if (player == null)
            return null;

        // =============================================
        // STEP 2 - FIND CAMERA INSIDE LOCAL PLAYER
        // =============================================

        Camera[] playerCameras =
            player.GetComponentsInChildren<Camera>(true);

        foreach (Camera cam in playerCameras)
        {
            if (IsUsableCamera(cam))
            {
                return CacheCamera(cam);
            }
        }

        // =============================================
        // STEP 3 - FIND THIRD PERSON CAMERA
        // CAMERA MAY BE OUTSIDE PLAYER
        // =============================================

        ThirdPersonCamera[] controllers =
            FindObjectsByType<ThirdPersonCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );

        foreach (ThirdPersonCamera controller in controllers)
        {
            if (controller == null ||
                !controller.isActiveAndEnabled ||
                controller.target == null)
            {
                continue;
            }

            Transform target = controller.target;

            bool belongsToLocalPlayer =
                target == player.transform ||
                target.IsChildOf(player.transform) ||
                player.transform.IsChildOf(target);

            if (!belongsToLocalPlayer)
                continue;

            // Camera cung GameObject voi controller.
            Camera cam =
                controller.GetComponent<Camera>();

            if (IsUsableCamera(cam))
                return CacheCamera(cam);

            // Camera o GameObject con.
            cam =
                controller.GetComponentInChildren<Camera>(true);

            if (IsUsableCamera(cam))
                return CacheCamera(cam);

            // Camera o GameObject cha.
            cam =
                controller.GetComponentInParent<Camera>();

            if (IsUsableCamera(cam))
                return CacheCamera(cam);

            // Camera nam duoi cha cua controller,
            // vi du CameraRoot -> Main Camera.
            if (controller.transform.parent != null)
            {
                Camera[] rootCameras =
                    controller.transform.parent
                        .GetComponentsInChildren<Camera>(true);

                foreach (Camera rootCamera in rootCameras)
                {
                    if (IsUsableCamera(rootCamera))
                        return CacheCamera(rootCamera);
                }
            }
        }

        // =============================================
        // STEP 4 - FALLBACK CAMERA.MAIN
        // =============================================

        Camera mainCamera = Camera.main;

        if (IsUsableCamera(mainCamera))
            return CacheCamera(mainCamera);

        // =============================================
        // STEP 5 - OTHER ACTIVE SCENE CAMERAS
        // =============================================

        Camera[] sceneCameras =
            FindObjectsByType<Camera>(
                FindObjectsSortMode.None
            );

        foreach (Camera cam in sceneCameras)
        {
            if (IsUsableCamera(cam))
                return CacheCamera(cam);
        }

        if (showCameraDebug)
        {
            Debug.LogWarning(
                "[DamageNumberUI] Cannot find active camera " +
                "for local player.",
                this
            );
        }

        return null;
    }

    // =====================================================
    // CACHE CAMERA
    // =====================================================

    private Camera CacheCamera(Camera cam)
    {
        if (localCamera != cam)
        {
            localCamera = cam;

            if (showCameraDebug)
            {
                Debug.Log(
                    "[DamageNumberUI] Found Camera: " +
                    cam.name,
                    this
                );
            }
        }

        return localCamera;
    }

    // =====================================================
    // SHOW DAMAGE
    // =====================================================

    public void ShowDamage(
        float damage,
        Vector3 worldPosition)
    {
        if (damage <= 0f)
            return;

        if (damageTextPrefab == null ||
            popupRoot == null ||
            canvas == null)
        {
            Debug.LogWarning(
                "[DamageNumberUI] Missing UI references.",
                this
            );

            return;
        }

        Camera cam = GetLocalCamera();

        if (cam == null)
        {
            Debug.LogWarning("[DamageNumberUI] Khong tim duoc camera local.", this);
            return;
        }

        // =============================================
        // WORLD POSITION TO SCREEN POSITION
        // =============================================

        Vector3 screenPosition =
            cam.WorldToScreenPoint(worldPosition);

        if (screenPosition.z <= 0f)
        {
            if (showCameraDebug)
                Debug.LogWarning("[DamageNumberUI] Popup nam sau Camera.", this);
            return;
        }

        // Khong tao popup ngoai viewport.
        if (screenPosition.x < 0f ||
            screenPosition.x > Screen.width ||
            screenPosition.y < 0f ||
            screenPosition.y > Screen.height)
        {
            if (showCameraDebug)
                Debug.LogWarning("[DamageNumberUI] Popup ngoai man hinh: " + screenPosition, this);
            return;
        }

        Camera uiCamera =
            canvas.renderMode ==
            RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera != null
                    ? canvas.worldCamera
                    : cam;

        Vector2 localPosition;

        bool converted =
            RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    popupRoot,
                    screenPosition,
                    uiCamera,
                    out localPosition
                );

        if (!converted)
            return;

        // =============================================
        // CREATE DAMAGE TEXT
        // =============================================

        TMP_Text damageText =
            Instantiate(
                damageTextPrefab,
                popupRoot
            );

        damageText.gameObject.SetActive(true);

        damageText.text =
            Mathf.RoundToInt(damage).ToString();

        damageText.color = normalDamageColor;
        damageText.raycastTarget = false;

        RectTransform rect =
            damageText.rectTransform;

        Vector2 randomOffset =
            new Vector2(
                Random.Range(
                    -randomSideOffset,
                    randomSideOffset
                ),
                Random.Range(-8f, 8f)
            );

        Vector2 startPosition =
            localPosition + randomOffset;

        rect.anchoredPosition = startPosition;
        rect.localScale = Vector3.one * startScale;

        if (showCameraDebug)
            Debug.Log("[DamageNumberUI] Created popup: " + damageText.text, this);

        StartCoroutine(
            AnimateDamage(
                damageText,
                startPosition
            )
        );
    }

    // =====================================================
    // ANIMATE DAMAGE
    // =====================================================

    private IEnumerator AnimateDamage(
        TMP_Text damageText,
        Vector2 startPosition)
    {
        if (damageText == null)
            yield break;

        RectTransform rect =
            damageText.rectTransform;

        Color initialColor = damageText.color;

        float elapsed = 0f;

        float safeDuration =
            Mathf.Max(0.1f, duration);

        float safePopDuration =
            Mathf.Clamp(
                popDuration,
                0.01f,
                safeDuration
            );

        while (elapsed < safeDuration)
        {
            if (damageText == null)
                yield break;

            elapsed += Time.unscaledDeltaTime;

            float t =
                Mathf.Clamp01(
                    elapsed / safeDuration
                );

            // =========================================
            // FLOAT UP
            // =========================================

            float smoothFloat =
                Mathf.SmoothStep(0f, 1f, t);

            rect.anchoredPosition =
                startPosition +
                Vector2.up *
                floatDistance *
                smoothFloat;

            // =========================================
            // POP SCALE
            // =========================================

            float scale;

            if (elapsed < safePopDuration)
            {
                float popT =
                    Mathf.Clamp01(
                        elapsed / safePopDuration
                    );

                scale = Mathf.Lerp(
                    startScale,
                    popScale,
                    popT
                );
            }
            else
            {
                float shrinkT =
                    Mathf.Clamp01(
                        (elapsed - safePopDuration) /
                        Mathf.Max(
                            0.01f,
                            safeDuration - safePopDuration
                        )
                    );

                scale = Mathf.Lerp(
                    popScale,
                    finalScale,
                    shrinkT
                );
            }

            rect.localScale = Vector3.one * scale;

            // =========================================
            // FADE OUT
            // =========================================

            float fadeT =
                Mathf.InverseLerp(
                    0.25f,
                    1f,
                    t
                );

            Color color = initialColor;

            color.a =
                initialColor.a * (1f - fadeT);

            damageText.color = color;

            yield return null;
        }

        if (damageText != null)
            Destroy(damageText.gameObject);
    }
}
