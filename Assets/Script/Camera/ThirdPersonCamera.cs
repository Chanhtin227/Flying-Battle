using UnityEngine;
using Fusion;

public class ThirdPersonCamera : MonoBehaviour
{
    // =========================================================
    // TARGET
    // =========================================================

    [Header("Target")]

    public Transform target;


    // =========================================================
    // CAMERA
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
    // AIM CAMERA
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
    // INTERNAL
    // =========================================================

    private float yaw;

    private float pitch;

    private float bobTimer;

    private float distance;

    private Vector3 currentShoulderOffset;

    private PlayerWeapon playerWeapon;

    private PlayerHealth playerHealth;

    private NetworkObject networkObject;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // CAMERA
        // =====================================================

        if (cam == null)
        {
            cam =
                GetComponentInChildren<Camera>(
                    true
                );
        }


        // =====================================================
        // REFERENCES
        // =====================================================

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


        // =====================================================
        // REMOTE PLAYER
        // =====================================================

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


        // =====================================================
        // CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible = false;


        // =====================================================
        // FOV
        // =====================================================

        if (cam != null)
        {
            cam.fieldOfView =
                normalFOV;
        }


        // =====================================================
        // DISTANCE
        // =====================================================

        distance =
            normalDistance;


        // =====================================================
        // SHOULDER OFFSET
        // =====================================================

        currentShoulderOffset =
            shoulderOffset;


        // =====================================================
        // YAW
        // =====================================================

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


        // =====================================================
        // PITCH
        // =====================================================

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
        // =====================================================
        // BASIC CHECK
        // =====================================================

        if (
            target == null ||
            cam == null
        )
        {
            return;
        }


        // =====================================================
        // FIND PLAYER HEALTH
        // =====================================================

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


        // =====================================================
        // DEAD
        // =====================================================

        if (
            playerHealth != null &&
            playerHealth.IsDead
        )
        {
            return;
        }


        // =====================================================
        // FIND WEAPON
        // =====================================================

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
        // CAN AIM
        // =====================================================

        bool canAim =
            false;


        if (playerWeapon != null)
        {
            canAim =
                playerWeapon.CurrentWeapon ==
                PlayerWeapon.WeaponType.Rifle
                ||
                playerWeapon.CurrentWeapon ==
                PlayerWeapon.WeaponType.Pistol;
        }


        // =====================================================
        // AIM INPUT
        // =====================================================

        isAiming =
            canAim &&
            Input.GetMouseButton(1);


        // =====================================================
        // MOUSE INPUT
        // =====================================================

        float mouseX =
            Input.GetAxis("Mouse X");


        float mouseY =
            Input.GetAxis("Mouse Y");


        // =====================================================
        // YAW
        // =====================================================

        yaw +=
            mouseX *
            mouseSensitivity *
            Time.deltaTime;


        // =====================================================
        // PLAYER ROTATION
        // =====================================================

        Quaternion playerRotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );


        target.rotation =
            playerRotation;


        // =====================================================
        // PITCH
        // =====================================================

        pitch -=
            mouseY *
            mouseSensitivity *
            Time.deltaTime;


        pitch =
            Mathf.Clamp(
                pitch,
                minPitch,
                maxPitch
            );


        // =====================================================
        // CAMERA ROTATION
        // =====================================================

        Quaternion rotation =
            Quaternion.Euler(
                pitch,
                yaw,
                0f
            );


        // =====================================================
        // AIM DISTANCE
        // =====================================================

        float targetDistance =
            isAiming
            ? aimDistance
            : normalDistance;


        // Dùng SmoothDamp để chuyển mượt nhưng nhanh
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
            isAiming
            ? aimFOV
            : normalFOV;


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
            isAiming
            ? aimShoulderOffset
            : shoulderOffset;


        // =====================================================
        // CHUYỂN OFFSET NHANH
        // =====================================================

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
        // CAMERA BOB
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


        float bobAmount;

        float bobSpeed;


        // =====================================================
        // AIM = GIẢM BOB RẤT MẠNH
        // =====================================================

        if (isAiming)
        {
            // Gần như không bob khi aim
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


        // =====================================================
        // BOB TIMER
        // =====================================================

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


        // =====================================================
        // BOB
        // =====================================================

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


        // =====================================================
        // WALL COLLISION
        // =====================================================

        Vector3 direction =
            desiredCameraPos -
            targetPos;


        float desiredDistance =
            direction.magnitude;


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
        // SET CAMERA
        // =====================================================

        cam.transform.position =
            desiredCameraPos;


        cam.transform.rotation =
            rotation;


        // =====================================================
        // FOLLOW PLAYER
        // =====================================================

        transform.position =
            target.position;
    }


    // =========================================================
    // DISTANCE VELOCITY
    // =========================================================

    private float distanceVelocity;
}