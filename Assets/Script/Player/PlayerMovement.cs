using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public Vector3 lookDirection;
    public NetworkBool isRunning;
    public NetworkBool isJumping;    // giữ Space (dùng để bay lên sau khi đã nhảy)
    public NetworkBool jumpPressed;  // chỉ true đúng 1 lần khi bấm Space xuống (để nhảy)
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
{
    public static PlayerMovement LocalPlayer;

    // =========================================================
    // MOVEMENT
    // =========================================================

    [Header("Movement")]

    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 2f;

    // MỚI: nhảy xong giữ Space để bay lên có giới hạn
    [Header("Jump Boost (giữ Space sau khi nhảy)")]

    // Tốc độ bay lên khi giữ Space
    public float boostSpeed = 4f;

    // Độ cao tối đa so với điểm bắt đầu nhảy (tính cả cú nhảy)
    public float maxBoostHeight = 4f;

    // Thời gian tối đa được bay lên tính từ lúc nhảy (giây)
    public float maxBoostTime = 1.5f;


    // =========================================================
    // MEDKIT MOVEMENT
    // =========================================================

    [Header("Medkit Movement")]

    public float smallMedkitSpeed = 1f;
    public float mediumMedkitSpeed = 0.75f;
    public float largeMedkitSpeed = 0.5f;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Camera")]

    public Transform cameraRoot;


    // =========================================================
    // REFERENCES
    // =========================================================

    private CharacterController characterController;
    private PlayerAnimation playerAnim;
    private PlayerHealth playerHealth;

    private Vector3 velocity;

    // Ghi nhận bấm Space 1 lần để nhảy trên mặt đất (tiêu thụ 1 lần trong GetLocalInput)
    private bool jumpQueued;

    // Trạng thái "bay lên có giới hạn" sau khi nhảy (chỉ State Authority dùng)
    private bool boostArmed;
    private float boostTimer;
    private float boostStartY;


    // =========================================================
    // NETWORK ANIMATION
    // =========================================================

    [Networked]
    private float NetworkedAnimSpeed { get; set; }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        characterController =
            GetComponent<CharacterController>();

        playerAnim =
            GetComponent<PlayerAnimation>();

        playerHealth =
            GetComponent<PlayerHealth>();


        // =====================================================
        // LOCAL PLAYER
        // =====================================================

        if (HasInputAuthority)
        {
            LocalPlayer = this;
        }


        // =====================================================
        // CHỈ STATE AUTHORITY SIMULATE CHARACTER CONTROLLER
        // =====================================================

        if (!HasStateAuthority)
        {
            characterController.enabled = false;
        }
    }


    // =========================================================
    // DESPAWNED
    // =========================================================

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        if (LocalPlayer == this)
        {
            LocalPlayer = null;
        }
    }


    // =========================================================
    // UPDATE (MỚI THÊM - chỉ để bắt phím bật/tắt bay cho đúng khung hình bấm xuống)
    // =========================================================

    private void Update()
    {
        // Chỉ người chơi đang điều khiển nhân vật này mới được bấm phím nhảy
        if (!HasInputAuthority)
            return;


        // Chỉ ghi nhận đúng khung hình vừa bấm Space xuống (không tính giữ phím),
        // để 1 lần bấm chỉ tính là 1 lần nhảy, không nhảy lặp lại khi giữ.
        if (Input.GetKeyDown(KeyCode.Space))
        {
            jumpQueued = true;
        }
    }


    // =========================================================
    // GET LOCAL INPUT
    // =========================================================

    public NetworkInputData GetLocalInput()
    {
        NetworkInputData data =
            new NetworkInputData();


        // =====================================================
        // DEAD = KHÔNG NHẬN INPUT
        // =====================================================

        if (
            playerHealth != null &&
            playerHealth.IsDead
        )
        {
            // Xóa cờ đã queue để không tự nhảy ngay khi vừa hồi sinh
            jumpQueued = false;

            return data;
        }


        float horizontal =
            Input.GetAxisRaw("Horizontal");

        float vertical =
            Input.GetAxisRaw("Vertical");


        // =====================================================
        // CAMERA DIRECTION
        // =====================================================

        if (cameraRoot != null)
        {
            Vector3 forward =
                cameraRoot.forward;

            Vector3 right =
                cameraRoot.right;


            forward.y = 0f;
            right.y = 0f;


            forward.Normalize();
            right.Normalize();


            data.moveDirection =
                Vector3.ClampMagnitude(
                    forward * vertical +
                    right * horizontal,
                    1f
                );


            data.lookDirection =
                forward;
        }


        // =====================================================
        // RUN
        // =====================================================

        data.isRunning =
            Input.GetKey(KeyCode.LeftShift);


        // =====================================================
        // JUMP
        // =====================================================

        data.isJumping =
            Input.GetKey(KeyCode.Space); // giữ để bay lên


        data.jumpPressed =
            jumpQueued;                  // bấm 1 lần để nhảy

        jumpQueued = false;


        return data;
    }


    // =========================================================
    // FIXED UPDATE NETWORK
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        // =====================================================
        // CHỈ STATE AUTHORITY
        // =====================================================

        if (!HasStateAuthority)
            return;


        // =====================================================
        // DEAD = ĐỨNG IM HOÀN TOÀN
        // =====================================================

        if (
            playerHealth != null &&
            playerHealth.IsDead
        )
        {
            velocity =
                Vector3.zero;

            boostArmed = false;


            NetworkedAnimSpeed =
                0f;


            if (playerAnim != null)
            {
                playerAnim.Move(0f);
            }


            return;
        }


        // =====================================================
        // GET INPUT
        // =====================================================

        if (!GetInput(
            out NetworkInputData data))
        {
            return;
        }


        // =====================================================
        // ROTATION
        // =====================================================

        if (
            data.lookDirection.sqrMagnitude >
            0.001f
        )
        {
            transform.rotation =
                Quaternion.LookRotation(
                    data.lookDirection,
                    Vector3.up
                );
        }

        // =====================================================
        // SPEED
        // =====================================================

        float speed =
            data.isRunning
            ? runSpeed
            : walkSpeed;


        // =====================================================
        // MEDKIT SPEED
        // =====================================================

        if (
            playerHealth != null &&
            playerHealth.UsingMedkitType != 0
        )
        {
            speed =
                GetMedkitSpeed(
                    playerHealth.UsingMedkitType
                );
        }


        // =====================================================
        // MOVE
        // =====================================================

        if (
            characterController != null &&
            characterController.enabled
        )
        {
            characterController.Move(
                data.moveDirection *
                speed *
                Runner.DeltaTime
            );
        }


        // =====================================================
        // ANIMATION
        // =====================================================

        float animationSpeed = 0f;


        if (
            data.moveDirection.sqrMagnitude >
            0.001f
        )
        {
            if (speed == walkSpeed)
            {
                animationSpeed =
                    data.moveDirection.magnitude *
                    0.5f;
            }
            else
            {
                animationSpeed =
                    data.moveDirection.magnitude;
            }
        }


        if (playerAnim != null)
        {
            playerAnim.Move(
                animationSpeed
            );
        }


        // =====================================================
        // JUMP
        // =====================================================

        if (
            characterController != null &&
            characterController.enabled &&
            characterController.isGrounded
        )
        {
            velocity.y = -2f;

            boostArmed = false;


            if (data.jumpPressed) // đổi từ data.isJumping
            {
                velocity.y =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );

                // MỚI: bắt đầu cho phép giữ Space để bay lên
                boostArmed = true;
                boostTimer = 0f;
                boostStartY = transform.position.y;


                if (playerAnim != null)
                {
                    playerAnim.Jump();
                }
            }
        }


        // =====================================================
        // JUMP BOOST (MỚI): nhảy rồi giữ Space để bay lên có giới hạn
        // =====================================================

        if (boostArmed)
        {
            bool heldSpace =
                data.isJumping;

            bool underTime =
                boostTimer < maxBoostTime;

            bool underHeight =
                transform.position.y - boostStartY <
                maxBoostHeight;


            if (heldSpace && underTime && underHeight)
            {
                // Giữ vận tốc đi lên tối thiểu bằng boostSpeed
                velocity.y =
                    Mathf.Max(
                        velocity.y,
                        boostSpeed
                    );

                boostTimer +=
                    Runner.DeltaTime;
            }
            else
            {
                // Thả Space hoặc hết giới hạn: tắt boost, rớt xuống
                // (phải nhảy lại mới có boost tiếp)
                boostArmed = false;
            }
        }


        // =====================================================
        // GRAVITY
        // =====================================================

        velocity.y +=
            gravity *
            Runner.DeltaTime;


        if (
            characterController != null &&
            characterController.enabled
        )
        {
            characterController.Move(
                velocity *
                Runner.DeltaTime
            );
        }


        // =====================================================
        // NETWORK ANIMATION
        // =====================================================

        NetworkedAnimSpeed =
            animationSpeed;
    }


    // =========================================================
    // RESET MOVEMENT
    // =========================================================

    public void ResetMovementState()
    {
        velocity =
            Vector3.zero;

        boostArmed = false;


        if (HasStateAuthority)
        {
            NetworkedAnimSpeed =
                0f;
        }
    }


    // =========================================================
    // GET MEDKIT SPEED
    // =========================================================

    private float GetMedkitSpeed(
        int medkitType)
    {
        switch (medkitType)
        {
            case 1:
                return smallMedkitSpeed;

            case 2:
                return mediumMedkitSpeed;

            case 3:
                return largeMedkitSpeed;
        }


        return walkSpeed;
    }


    // =========================================================
    // RENDER
    // =========================================================

    public override void Render()
    {
        if (
            !HasStateAuthority &&
            playerAnim != null
        )
        {
            playerAnim.Move(
                NetworkedAnimSpeed
            );
        }
    }
}