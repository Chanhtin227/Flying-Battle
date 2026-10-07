using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public Vector3 lookDirection;

    public NetworkBool isRunning;

    public NetworkBool isJumping;

    public NetworkBool jumpPressed;
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
    // JUMP BOOST
    // =========================================================

    [Header("Jump Boost (giữ Space sau khi nhảy)")]

    public float boostSpeed = 4f;

    public float maxBoostHeight = 4f;

    public float maxBoostTime = 1.5f;


    // =========================================================
    // MEDKIT MOVEMENT
    // =========================================================

    [Header("Medkit Movement")]

    public float smallMedkitSpeed = 1f;

    public float mediumMedkitSpeed = 0.75f;

    public float largeMedkitSpeed = 0.5f;


    // =========================================================
    // FOOTSTEP SOUND
    // =========================================================

    [Header("Footstep Sound")]

    [Tooltip("Âm thanh bước chân khi đi bộ.")]
    public AudioClip walkFootstepSound;

    [Tooltip("Âm thanh bước chân khi chạy.")]
    public AudioClip runFootstepSound;


    [Range(0f, 1f)]
    public float footstepVolume = 0.7f;


    [Tooltip("Thời gian giữa 2 bước khi đi bộ.")]
    public float walkStepInterval = 0.3f;


    [Tooltip("Thời gian giữa 2 bước khi chạy.")]
    public float runStepInterval = 0.1f;


    private float nextFootstepTime = 0f;


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
    // JUMP STATE
    // =========================================================

    private bool jumpQueued;


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
        // ONLY STATE AUTHORITY SIMULATES MOVEMENT
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
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // ONLY LOCAL PLAYER
        // =====================================================

        if (!HasInputAuthority)
            return;


        // =====================================================
        // DEAD / MATCH END
        // =====================================================

        if (
            (playerHealth != null &&
             playerHealth.IsDead) ||

            (MatchManager.Instance != null &&
             MatchManager.Instance.MatchEnded)
        )
        {
            jumpQueued = false;

            return;
        }


        // =====================================================
        // JUMP PRESS
        // =====================================================

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
        // DEAD / MATCH END
        // =====================================================

        if (
            (playerHealth != null &&
             playerHealth.IsDead) ||

            (MatchManager.Instance != null &&
             MatchManager.Instance.MatchEnded)
        )
        {
            jumpQueued = false;

            return data;
        }


        // =====================================================
        // MOVEMENT INPUT
        // =====================================================

        float horizontal =
            Input.GetAxisRaw(
                "Horizontal"
            );


        float vertical =
            Input.GetAxisRaw(
                "Vertical"
            );


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
            Input.GetKey(
                KeyCode.LeftShift
            );


        // =====================================================
        // JUMP
        // =====================================================

        data.isJumping =
            Input.GetKey(
                KeyCode.Space
            );


        data.jumpPressed =
            jumpQueued;


        jumpQueued = false;


        return data;
    }


    // =========================================================
    // FIXED UPDATE NETWORK
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        // =====================================================
        // ONLY STATE AUTHORITY
        // =====================================================

        if (!HasStateAuthority)
            return;


        // =====================================================
        // DEAD / MATCH END
        // =====================================================

        if (
            (playerHealth != null &&
             playerHealth.IsDead) ||

            (MatchManager.Instance != null &&
             MatchManager.Instance.MatchEnded)
        )
        {
            velocity =
                Vector3.zero;


            boostArmed =
                false;


            NetworkedAnimSpeed =
                0f;


            if (playerAnim != null)
            {
                playerAnim.Move(
                    0f
                );
            }


            return;
        }


        // =====================================================
        // GET INPUT
        // =====================================================

        if (
            !GetInput(
                out NetworkInputData data
            )
        )
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
        // FOOTSTEP SOUND
        // =====================================================

        HandleFootstepSound(
            data
        );


        // =====================================================
        // JUMP
        // =====================================================

        if (
            characterController != null &&
            characterController.enabled &&
            characterController.isGrounded
        )
        {
            velocity.y =
                -2f;


            boostArmed =
                false;


            if (data.jumpPressed)
            {
                velocity.y =
                    Mathf.Sqrt(
                        jumpHeight *
                        -2f *
                        gravity
                    );


                boostArmed =
                    true;


                boostTimer =
                    0f;


                boostStartY =
                    transform.position.y;


                if (playerAnim != null)
                {
                    playerAnim.Jump();
                }
            }
        }


        // =====================================================
        // JUMP BOOST
        // =====================================================

        if (boostArmed)
        {
            bool heldSpace =
                data.isJumping;


            bool underTime =
                boostTimer <
                maxBoostTime;


            bool underHeight =
                transform.position.y -
                boostStartY <
                maxBoostHeight;


            if (
                heldSpace &&
                underTime &&
                underHeight
            )
            {
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
                boostArmed =
                    false;
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
    // FOOTSTEP SOUND
    // =========================================================

    private void HandleFootstepSound(
        NetworkInputData data)
    {
        // =====================================================
        // ONLY LOCAL PLAYER
        // =====================================================

        if (!HasInputAuthority)
            return;


        // =====================================================
        // CHECK CHARACTER CONTROLLER
        // =====================================================

        if (characterController == null)
            return;


        // =====================================================
        // MUST BE GROUNDED
        // =====================================================

        if (!characterController.isGrounded)
            return;


        // =====================================================
        // MUST BE MOVING
        // =====================================================

        if (
            data.moveDirection.sqrMagnitude <
            0.01f
        )
        {
            return;
        }


        // =====================================================
        // TIMER
        // =====================================================

        if (
            Time.time <
            nextFootstepTime
        )
        {
            return;
        }


        // =====================================================
        // WALK / RUN
        // =====================================================

        bool isRunning =
            data.isRunning;


        float interval =
            isRunning
            ? runStepInterval
            : walkStepInterval;


        AudioClip footstepClip =
            isRunning
            ? runFootstepSound
            : walkFootstepSound;


        nextFootstepTime =
            Time.time +
            interval;


        // =====================================================
        // CHECK AUDIO CLIP
        // =====================================================

        if (footstepClip == null)
            return;


        // =====================================================
        // CHECK SFX PLAYER
        // =====================================================

        if (
            GameAudio.SfxPlayer.Instance ==
            null
        )
        {
            return;
        }


        // =====================================================
        // PLAY FOOTSTEP
        // =====================================================

        GameAudio.SfxPlayer.Instance.PlaySfx(
            footstepClip,
            footstepVolume
        );
    }


    // =========================================================
    // RESET MOVEMENT
    // =========================================================

    public void ResetMovementState()
    {
        velocity =
            Vector3.zero;


        boostArmed =
            false;


        nextFootstepTime =
            0f;


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