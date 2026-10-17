using UnityEngine;
using Fusion;
using GameSettings;

public class ThirdPersonCamera : MonoBehaviour
{
    // =========================================================
    // TARGET
    // =========================================================

    [Header("Target")]
    public Transform target;


    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]
    public Camera cam;


    // =========================================================
    // MOUSE
    // =========================================================

    [Header("Mouse")]
    public float mouseSensitivity = 150f;


    // =========================================================
    // NORMAL CAMERA
    // =========================================================

    [Header("Normal Camera")]
    public Vector3 shoulderOffset =
        new Vector3(0.6f, 1.6f, 0f);

    public float normalDistance = 4f;


    // =========================================================
    // AIM
    // =========================================================

    [Header("Aim")]
    public bool isAiming = false;


    [Tooltip("Vị trí vai khi Aim")]
    public Vector3 aimShoulderOffset =
        new Vector3(0.75f, 1.55f, 0f);


    [Tooltip("Khoảng cách camera khi Aim")]
    public float aimDistance = 2.2f;


    [Tooltip("FOV bình thường")]
    public float normalFOV = 60f;


    [Tooltip("FOV khi Aim")]
    public float aimFOV = 45f;


    [Tooltip("Tốc độ chuyển vào/ra Aim")]
    public float aimSpeed = 25f;


    // =========================================================
    // SNIPER SCOPE - LOCAL UI
    // =========================================================

    [Header("Sniper Scope (Local Player Only)")]
    [Tooltip("Keo SniperScopeManager co ScopeUIController vao day. Camera se dieu khien UI qua component nay.")]
    public ScopeUIController scopeController;
    [Tooltip("Canvas Scope overlay o Map_True1. Tat Active mac dinh.")]
    public GameObject sniperScopeUI;

    [Tooltip("Tam ngam thuong an khi dang dung Scope")]
    public GameObject normalCrosshair;

    [Tooltip("Chi Rifle duoc dung Scope. Pistol luon Aim TPS binh thuong.")]
    public bool scopeForPistol = false; // Giu field cu de khong mat serialized data; khong duoc su dung.

    [Range(10f, 45f)] public float sniperScopeFOV = 20f;

    [Tooltip("Offset tinh tu goc Player, trong he toa do huong nhin")]
    public Vector3 sniperEyeOffset = new Vector3(0f, 1.55f, 0.45f);

    [Tooltip("Chi keo Renderer cua nhan vat local che mat scope; khong an player remote.")]
    public Renderer[] hideLocalRenderersWhileScoped;

    private bool scopeActive;
    private bool[] originalRendererEnabled;
    private bool rendererStatesCaptured;
    private bool originalCrosshairActive;
    private bool crosshairStateSaved;

    // Player la prefab runtime, nen khong the keo UI tu Scene vao Prefab Asset.
    // Tu dong tim ScopeUIController trong Map_True1, va thu lai neu Canvas load muon.
    private float nextScopeSearchTime;
    private bool missingScopeWarningShown;

    private void FindScopeController()
    {
        if (scopeController != null && scopeController.isActiveAndEnabled)
            return;

        scopeController = null;

        if (Time.unscaledTime < nextScopeSearchTime)
            return;

        nextScopeSearchTime = Time.unscaledTime + 0.5f;
        scopeController = FindFirstObjectByType<ScopeUIController>();

        if (scopeController != null)
        {
            missingScopeWarningShown = false;
            Debug.Log("[SNIPER] Connected to Scope UI: " + scopeController.name, this);
        }
        else if (!missingScopeWarningShown)
        {
            missingScopeWarningShown = true;
            Debug.LogWarning(
                "[SNIPER] Chua tim thay ScopeUIController dang Active trong Scene. " +
                "Kiem tra SniperScopeManager va Canvas cua Map_True1.", this);
        }
    }

    private void SetScopeVisual(bool active)
    {
        // Luon tim UI truoc khi kiem tra trang thai. Neu lan dau Scope ON
        // nhung UI chua load, camera se ket noi lai trong cac frame sau.
        FindScopeController();

        // Day trang thai cho UI moi frame (controller tu bo qua neu khong doi).
        // Nho vay khong bi mat su kien khi Scene/Canvas duoc load muon.
        if (scopeController != null)
            scopeController.SetScope(active);
        else if (sniperScopeUI != null && sniperScopeUI.activeSelf != active)
            sniperScopeUI.SetActive(active);

        if (scopeActive == active)
            return;

        scopeActive = active;

        // Cach gan truc tiep UI vao camera chi la du phong.
        if (scopeController == null && normalCrosshair != null)
        {
            // Khong tat nham hinh kinh Scope neu Crosshair tro vao con cua no.
            bool safeCrosshair = sniperScopeUI == null ||
                (normalCrosshair != sniperScopeUI &&
                 !normalCrosshair.transform.IsChildOf(sniperScopeUI.transform));

            if (safeCrosshair)
            {
                if (active)
                {
                    originalCrosshairActive = normalCrosshair.activeSelf;
                    crosshairStateSaved = true;
                    normalCrosshair.SetActive(false);
                }
                else if (crosshairStateSaved)
                {
                    normalCrosshair.SetActive(originalCrosshairActive);
                    crosshairStateSaved = false;
                }
            }
        }

        if (hideLocalRenderersWhileScoped == null)
            return;

        if (originalRendererEnabled == null ||
            originalRendererEnabled.Length != hideLocalRenderersWhileScoped.Length)
            originalRendererEnabled = new bool[hideLocalRenderersWhileScoped.Length];

        if (active)
        {
            for (int i = 0; i < hideLocalRenderersWhileScoped.Length; i++)
            {
                Renderer r = hideLocalRenderersWhileScoped[i];
                if (r == null) continue;
                originalRendererEnabled[i] = r.enabled;
                r.enabled = false;
            }
            rendererStatesCaptured = true;
        }
        else if (rendererStatesCaptured)
        {
            for (int i = 0; i < hideLocalRenderersWhileScoped.Length; i++)
            {
                Renderer r = hideLocalRenderersWhileScoped[i];
                if (r != null) r.enabled = originalRendererEnabled[i];
            }
            rendererStatesCaptured = false;
        }
    }

    private void OnDisable()
    {
        SetScopeVisual(false);
    }

    // =========================================================
    // CAMERA ANGLE
    // =========================================================

    [Header("Camera Angle")]
    public float minPitch = -30f;
    public float maxPitch = 60f;


    // =========================================================
    // ROTATION SMOOTH
    // =========================================================

    [Header("Rotation Smooth")]
    public float rotationSmoothTime = 0.06f;


    // =========================================================
    // WALL COLLISION
    // =========================================================

    [Header("Wall Collision")]
    public LayerMask wallLayer;

    public float cameraRadius = 0.2f;
    public float minDistance = 0.5f;
    public float collisionOffset = 0.1f;


    // =========================================================
    // CAMERA BOB
    // =========================================================

    [Header("Camera Bob")]
    public float walkBobAmount = 0.01f;
    public float walkBobSpeed = 7f;

    public float runBobAmount = 0.025f;
    public float runBobSpeed = 11f;


    // =========================================================
    // PRIVATE
    // =========================================================

    private float yaw;
    private float pitch;

    private float bobTimer;

    private float distance;

    private float distanceVelocity;

    private Vector3 currentShoulderOffset;

    private PlayerWeapon playerWeapon;
    private PlayerHealth playerHealth;

    private NetworkObject networkObject;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // -----------------------------------------------------
        // CAMERA
        // -----------------------------------------------------

        if (cam == null)
        {
            cam =
                GetComponentInChildren<Camera>(true);
        }


        // -----------------------------------------------------
        // TARGET
        // -----------------------------------------------------

        if (target != null)
        {
            networkObject =
                target.GetComponent<NetworkObject>();


            playerWeapon =
                target.GetComponent<PlayerWeapon>();


            playerHealth =
                target.GetComponent<PlayerHealth>();


            if (playerWeapon == null)
            {
                playerWeapon =
                    target.GetComponentInParent<PlayerWeapon>();
            }


            if (playerHealth == null)
            {
                playerHealth =
                    target.GetComponentInParent<PlayerHealth>();
            }
        }


        // -----------------------------------------------------
        // CHỈ LOCAL PLAYER CÓ CAMERA
        // -----------------------------------------------------

        if (
            networkObject != null &&
            !networkObject.HasInputAuthority
        )
        {
            if (cam != null)
            {
                cam.gameObject.SetActive(false);
            }


            enabled = false;

            return;
        }


        // Tim UI Scene luc chay; neu chua co thi SetScopeVisual se thu lai.
        FindScopeController();

        // Khoi tao scope dung trang thai, ke ca object UI bi bat san.
        scopeActive = true;
        SetScopeVisual(false);

        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;


        // -----------------------------------------------------
        // CAMERA FOV
        // -----------------------------------------------------

        if (cam != null)
        {
            cam.fieldOfView =
                normalFOV;
        }


        // -----------------------------------------------------
        // INITIAL DISTANCE
        // -----------------------------------------------------

        distance =
            normalDistance;


        // -----------------------------------------------------
        // INITIAL SHOULDER
        // -----------------------------------------------------

        currentShoulderOffset =
            shoulderOffset;


        // -----------------------------------------------------
        // INITIAL YAW
        // -----------------------------------------------------

        if (target != null)
        {
            yaw =
                target.eulerAngles.y;
        }
        else
        {
            yaw =
                transform.eulerAngles.y;
        }


        // -----------------------------------------------------
        // INITIAL PITCH
        // -----------------------------------------------------

        pitch =
            transform.eulerAngles.x;


        if (pitch > 180f)
        {
            pitch -= 360f;
        }


        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    private void LateUpdate()
    {
        // -----------------------------------------------------
        // CHECK
        // -----------------------------------------------------

        if (
            target == null ||
            cam == null
        )
        {
            return;
        }


        // -----------------------------------------------------
        // PLAYER HEALTH
        // -----------------------------------------------------

        if (playerHealth == null)
        {
            playerHealth =
                target.GetComponent<PlayerHealth>();


            if (playerHealth == null)
            {
                playerHealth =
                    target.GetComponentInParent<PlayerHealth>();
            }
        }


        // -----------------------------------------------------
        // DEAD / MATCH END = KHÓA HOÀN TOÀN CAMERA INPUT
        // -----------------------------------------------------

        bool matchEnded =
            MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded;


        if (
            matchEnded ||
            (playerHealth != null && playerHealth.IsDead)
        )
        {
            isAiming = false;
            SetScopeVisual(false);
            return;
        }


        // -----------------------------------------------------
        // PLAYER WEAPON
        // -----------------------------------------------------

        if (playerWeapon == null)
        {
            playerWeapon =
                target.GetComponent<PlayerWeapon>();


            if (playerWeapon == null)
            {
                playerWeapon =
                    target.GetComponentInParent<PlayerWeapon>();
            }
        }


        // =====================================================
        // INVENTORY STATE
        // =====================================================

        bool inventoryOpen =
            InventoryUI.IsInventoryOpen;


        // =====================================================
        // AIM
        // =====================================================

        bool canAim = false;


        if (playerWeapon != null)
        {
            canAim =
                playerWeapon.CurrentWeapon ==
                PlayerWeapon.WeaponType.Rifle
                ||
                playerWeapon.CurrentWeapon ==
                PlayerWeapon.WeaponType.Pistol;
        }


        // -----------------------------------------------------
        // KHÔNG AIM KHI MỞ BALO
        // -----------------------------------------------------

        if (inventoryOpen)
        {
            isAiming = false;
        }
        else
        {
            isAiming =
                canAim &&
                Input.GetMouseButton(1);
        }

        // Scope chi cho Rifle. Pistol van isAiming va aimFOV nhu cu.
        bool sniperEquipped = playerWeapon != null &&
            playerWeapon.CurrentWeapon == PlayerWeapon.WeaponType.Rifle;

        bool useSniperScope = isAiming && sniperEquipped;
        SetScopeVisual(useSniperScope);


        // =====================================================
        // CAMERA ROTATION
        // =====================================================

        if (!inventoryOpen)
        {
            // -------------------------------------------------
            // MOUSE INPUT
            // -------------------------------------------------

            float mouseX =
                Input.GetAxis("Mouse X");


            float mouseY =
                Input.GetAxis("Mouse Y");


            // -------------------------------------------------
            // GAME SETTINGS
            // -------------------------------------------------

            float sensitivityScale =
                GameplaySettings.MouseSensitivity;


            // -------------------------------------------------
            // YAW
            // -------------------------------------------------

            yaw +=
                mouseX *
                mouseSensitivity *
                sensitivityScale *
                Time.deltaTime;


            // -------------------------------------------------
            // PITCH
            // -------------------------------------------------

            pitch -=
                mouseY *
                mouseSensitivity *
                sensitivityScale *
                Time.deltaTime;


            pitch =
                Mathf.Clamp(
                    pitch,
                    minPitch,
                    maxPitch
                );
        }


        // =====================================================
        // ROTATION
        // =====================================================

        Quaternion playerRotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );


        // =====================================================
        // KHI ĐANG MỞ BALO
        // CAMERA GIỮ NGUYÊN HƯỚNG
        // =====================================================

        if (inventoryOpen)
        {
            target.rotation =
                playerRotation;
        }
        else
        {
            target.rotation =
                playerRotation;
        }


        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        // =====================================================
        // DISTANCE
        // =====================================================

        float targetDistance =
            useSniperScope ? 0f :
            (isAiming ? aimDistance : normalDistance);


        float distanceSmoothTime =
            1f /
            Mathf.Max(
                aimSpeed,
                0.01f
            );


        distance =
            Mathf.SmoothDamp(
                distance,
                targetDistance,
                ref distanceVelocity,
                distanceSmoothTime
            );


        // =====================================================
        // FOV
        // =====================================================

        float targetFOV =
            useSniperScope ? sniperScopeFOV :
            (isAiming ? aimFOV : normalFOV);


        float fovLerp =
            1f -
            Mathf.Exp(
                -aimSpeed *
                Time.deltaTime
            );


        cam.fieldOfView =
            Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                fovLerp
            );


        // =====================================================
        // SHOULDER OFFSET
        // =====================================================

        Vector3 wantedOffset =
            useSniperScope ? sniperEyeOffset :
            (isAiming ? aimShoulderOffset : shoulderOffset);


        float offsetLerp =
            1f -
            Mathf.Exp(
                -aimSpeed *
                Time.deltaTime
            );


        currentShoulderOffset =
            Vector3.Lerp(
                currentShoulderOffset,
                wantedOffset,
                offsetLerp
            );


        // =====================================================
        // TARGET POSITION
        // =====================================================

        Vector3 targetPos =
            target.position +
            rotation *
            currentShoulderOffset;


        // =====================================================
        // MOVEMENT
        // =====================================================

        float horizontal =
            Input.GetAxis("Horizontal");


        float vertical =
            Input.GetAxis("Vertical");


        float movement =
            new Vector2(
                horizontal,
                vertical
            ).magnitude;


        bool isMoving =
            movement > 0.1f;


        bool isRunning =
            Input.GetKey(KeyCode.LeftShift) &&
            isMoving;


        // =====================================================
        // CAMERA BOB
        // =====================================================

        float bobAmount;
        float bobSpeed;


        if (isAiming)
        {
            bobAmount =
                0.0015f;

            bobSpeed =
                walkBobSpeed;
        }
        else if (isRunning)
        {
            bobAmount =
                runBobAmount;

            bobSpeed =
                runBobSpeed;
        }
        else
        {
            bobAmount =
                walkBobAmount;

            bobSpeed =
                walkBobSpeed;
        }


        // -----------------------------------------------------
        // BOB TIMER
        // -----------------------------------------------------

        if (isMoving)
        {
            bobTimer +=
                Time.deltaTime *
                bobSpeed;
        }
        else
        {
            bobTimer =
                Mathf.Lerp(
                    bobTimer,
                    0f,
                    Time.deltaTime *
                    8f
                );
        }


        float bobX =
            Mathf.Sin(
                bobTimer
            ) *
            bobAmount;


        float bobY =
            Mathf.Cos(
                bobTimer * 2f
            ) *
            bobAmount;


        targetPos +=
            rotation *
            new Vector3(
                bobX,
                bobY,
                0f
            );


        // =====================================================
        // CAMERA POSITION
        // =====================================================

        Vector3 desiredCameraPos =
            targetPos -
            rotation *
            Vector3.forward *
            distance;


        Vector3 direction =
            desiredCameraPos -
            targetPos;


        float desiredDistance =
            direction.magnitude;


        // =====================================================
        // WALL COLLISION
        // =====================================================

        if (
            desiredDistance >
            0.01f
        )
        {
            if (
                Physics.SphereCast(
                    targetPos,
                    cameraRadius,
                    direction.normalized,
                    out RaycastHit hit,
                    desiredDistance,
                    wallLayer,
                    QueryTriggerInteraction.Ignore
                )
            )
            {
                float safeDistance =
                    hit.distance -
                    collisionOffset;


                safeDistance =
                    Mathf.Clamp(
                        safeDistance,
                        minDistance,
                        distance
                    );


                desiredCameraPos =
                    targetPos -
                    rotation *
                    Vector3.forward *
                    safeDistance;
            }
        }


        // =====================================================
        // APPLY CAMERA
        // =====================================================

        cam.transform.position =
            desiredCameraPos;


        cam.transform.rotation =
            rotation;


        // =====================================================
        // FOLLOW TARGET
        // =====================================================

        transform.position =
            target.position;
    }
}