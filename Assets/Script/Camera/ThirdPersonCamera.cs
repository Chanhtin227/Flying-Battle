using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    // =========================================================
    // TARGET
    // =========================================================

    [Header("Target")]
    public Transform target;


    // =========================================================
    // CAMERA REFERENCE
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

    public Vector3 aimShoulderOffset =
        new Vector3(0.75f, 1.55f, 0f);

    public float aimDistance = 2f;

    public float normalFOV = 60f;

    public float aimFOV = 40f;

    public float aimSpeed = 8f;


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
    // INTERNAL VARIABLES
    // =========================================================

    float yaw;
    float pitch;

    float pitchVelocity;

    float bobTimer;

    float distance;


    // =========================================================
    // START
    // =========================================================

    void Start()
    {
        // Tự tìm Camera con
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }

        // Khóa chuột
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // FOV ban đầu
        if (cam != null)
        {
            cam.fieldOfView = normalFOV;
        }

        // Khoảng cách ban đầu
        distance = normalDistance;

        // =====================================================
        // LẤY HƯỚNG PLAYER LÀM HƯỚNG CAMERA
        // =====================================================

        if (target != null)
        {
            yaw = target.eulerAngles.y;
        }
        else
        {
            yaw = transform.eulerAngles.y;
        }

        // Góc lên xuống ban đầu
        pitch = transform.eulerAngles.x;

        if (pitch > 180f)
        {
            pitch -= 360f;
        }

        pitch = Mathf.Clamp(
            pitch,
            minPitch,
            maxPitch
        );
    }


    // =========================================================
    // LATE UPDATE
    // =========================================================

    void LateUpdate()
    {
        if (target == null || cam == null)
        {
            return;
        }


        // =====================================================
        // AIM
        // =====================================================

        bool canAim = false;

        PlayerWeapon weapon =
            target.GetComponent<PlayerWeapon>();

        if (weapon != null)
        {
            canAim =
                weapon.currentWeapon ==
                PlayerWeapon.WeaponType.Rifle ||

                weapon.currentWeapon ==
                PlayerWeapon.WeaponType.Pistol;
        }

        isAiming =
            canAim &&
            Input.GetMouseButton(1);


        // =====================================================
        // MOUSE
        // =====================================================

        float mouseX =
            Input.GetAxis("Mouse X");

        float mouseY =
            Input.GetAxis("Mouse Y");


        // =====================================================
        // XOAY PLAYER + CAMERA CÙNG NHAU
        // =====================================================

        yaw +=
            mouseX *
            mouseSensitivity *
            Time.deltaTime;


        // =====================================================
        // PLAYER XOAY THEO CAMERA
        // =====================================================

        Quaternion playerRotation =
            Quaternion.Euler(
                0f,
                yaw,
                0f
            );

        target.rotation = playerRotation;


        // =====================================================
        // PITCH CAMERA
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
        // CAMERA DISTANCE
        // =====================================================

        float targetDistance =
            isAiming
            ? aimDistance
            : normalDistance;

        distance =
            Mathf.Lerp(
                distance,
                targetDistance,
                Time.deltaTime * aimSpeed
            );


        // =====================================================
        // CAMERA FOV
        // =====================================================

        float targetFOV =
            isAiming
            ? aimFOV
            : normalFOV;

        cam.fieldOfView =
            Mathf.Lerp(
                cam.fieldOfView,
                targetFOV,
                Time.deltaTime * aimSpeed
            );


        // =====================================================
        // SHOULDER OFFSET
        // =====================================================

        Vector3 wantedOffset =
            isAiming
            ? aimShoulderOffset
            : shoulderOffset;


        Vector3 currentOffset =
            Vector3.Lerp(
                shoulderOffset,
                wantedOffset,
                Time.deltaTime * aimSpeed
            );


        // =====================================================
        // TARGET POSITION
        // =====================================================

        Vector3 targetPos =
            target.position +
            rotation *
            currentOffset;


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


        if (isAiming)
        {
            bobAmount =
                walkBobAmount * 0.2f;

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
                    Time.deltaTime * 8f
                );
        }


        // =====================================================
        // BOB
        // =====================================================

        float bobX =
            Mathf.Sin(bobTimer) *
            bobAmount;

        float bobY =
            Mathf.Cos(bobTimer * 2f) *
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


        if (desiredDistance > 0.01f)
        {
            if (Physics.SphereCast(
                targetPos,
                cameraRadius,
                direction.normalized,
                out RaycastHit hit,
                desiredDistance,
                wallLayer,
                QueryTriggerInteraction.Ignore))
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
        // ĐẶT CAMERA
        // =====================================================

        cam.transform.position =
            desiredCameraPos;

        cam.transform.rotation =
            rotation;


        // =====================================================
        // CAMERA OBJECT ĐI THEO PLAYER
        // =====================================================

        transform.position =
            target.position;

        // Quan trọng:
        // Không xoay Camera Object riêng nữa.
        // Camera đã có rotation ở trên.
    }
}