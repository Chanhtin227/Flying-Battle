using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public Vector3 lookDirection;
    public NetworkBool isRunning;
    public NetworkBool isJumping;
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
            Input.GetKey(KeyCode.Space);


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


            if (data.isJumping)
            {
                velocity.y =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );


                if (playerAnim != null)
                {
                    playerAnim.Jump();
                }
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