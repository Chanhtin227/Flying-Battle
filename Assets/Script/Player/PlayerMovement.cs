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

    [Header("Jump Reliability")]
    [Tooltip("Cho phep nhan Space som mot chut truoc khi cham dat.")]
    [Min(0.01f)] public float jumpBufferTime = 0.18f;

    [Tooltip("Van cho phep nhay trong mot khoang rat ngan sau khi roi mep dat.")]
    [Min(0f)] public float coyoteTime = 0.12f;

    [Tooltip("Tranh kich hoat lai jump do cung mot lan bam Space.")]
    [Min(0.01f)] public float jumpRepeatLockTime = 0.2f;



    // =========================================================
    // JUMP BOOST
    // =========================================================

    [Header("Jump Boost (nhấn Space 2 lần, giữ lần 2 để bay)")]

    public float boostSpeed = 4f;

    [Tooltip("Do cao toi da tinh tu vi tri bat dau nhay (met). Den gioi han se lu lung cho den khi het thoi gian boost.")]
    [Min(0.1f)] public float maxBoostHeight = 6f;

    [Min(0.1f)] public float maxBoostTime = 7f;

    [Header("Jetpack Cooldown")]
    [Tooltip("Sau khi dap dat, Jetpack can 4 giay hoi truoc khi bay tiep.")]
    [Min(0f)] public float jetpackRechargeTime = 4f;

    [Header("Smooth Jetpack Flight")]
    [Tooltip("Gia tốc đẩy lên khi giữ Space (m/s²). Càng thấp càng êm.")]
    [Min(0.1f)] public float boostAcceleration = 18f;

    [Tooltip("Tỷ lệ trọng lực khi Jetpack đang hoạt động. 0.2 = 20% trọng lực bình thường.")]
    [Range(0f, 1f)] public float boostGravityScale = 0.2f;

    [Tooltip("Tốc độ rơi tối đa (m/s) để giảm rơi quá gắt.")]
    [Min(1f)] public float maxFallSpeed = 18f;


    // =========================================================
    // MEDKIT MOVEMENT
    // =========================================================

    [Header("Medkit Movement")]

    public float smallMedkitSpeed = 1f;

    public float mediumMedkitSpeed = 0.75f;

    public float largeMedkitSpeed = 0.5f;


    // =========================================================
    // FOOTSTEPS - DISTANCE BASED (SMOOTH MULTIPLAYER)
    // =========================================================

    [Header("Footstep Sound")]
    public AudioClip walkFootstepSound;
    public AudioClip runFootstepSound;

    [Range(0f, 1f)] public float footstepVolume = 0.48f;

    [Header("Footstep Anti-Overlap")]
    [Tooltip("Khoang cach thoi gian toi thieu giua 2 tieng buoc chan khi di bo (giay).")]
    [Min(0.05f)] public float minWalkStepInterval = 0.38f;

    [Tooltip("Khoang cach thoi gian toi thieu giua 2 tieng buoc chan khi chay (giay).")]
    [Min(0.05f)] public float minRunStepInterval = 0.28f;

    [Tooltip("Khong phat buoc chan moi khi clip buoc truoc van dang phat.")]
    public bool preventFootstepOverlap = true;

    [Tooltip("Số mét thực tế giữa hai bước chân khi đi bộ.")]
    [Min(0.1f)] public float walkStepDistance = 0.85f;

    [Tooltip("Số mét thực tế giữa hai bước chân khi chạy.")]
    [Min(0.1f)] public float runStepDistance = 1.35f;

    [Tooltip("Độ mượt của animation khi đổi đi/chạy/dừng.")]
    [Min(0.01f)] public float animationSmoothTime = 0.12f;

    [Tooltip("Tùy chọn AudioSource 3D cho tiếng bước chân. Có thể để trống, script sẽ tự tạo.")]
    public AudioSource footstepSource;

    [Networked] private NetworkBool NetworkedGrounded { get; set; }
    [Networked] private NetworkBool NetworkedMoving { get; set; }
    [Networked] private NetworkBool NetworkedRunning { get; set; }

    private Vector3 previousFootstepPosition;
    private float footstepDistanceAccumulated;
    private float smoothAnimSpeed;
    private float animSmoothVelocity;
    private bool footstepInitialized;
    private float nextFootstepAllowedTime;

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
    private float jumpQueuedUntil;
    private float jumpBufferRemaining;
    private float coyoteRemaining;
    private float jumpLockRemaining;
    // Only one jump command for each press, even if input is re-sent for several ticks.
    private bool serverSpaceWasHeld;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];

    [Header("Ground Probe")]
    [Tooltip("Additional probe distance below the feet (meters).")]
    [Range(0.05f, 0.4f)] public float groundProbeDistance = 0.20f;



    // After the first jump, wait for a separate second Space press.
    private bool boostArmed;
    private bool jetpackSecondPressActivated;

    private float boostTimer;

    private float boostStartY;

    [Header("Jetpack Input Stability")]
    [Tooltip("Cho phep mat input giu Space trong vai network tick ma khong tat Jetpack.")]
    [Range(0.05f, 0.4f)] public float jetpackInputGraceTime = 0.18f;
    private float jetpackHeldGraceRemaining;

    // Chi dem cooldown khi da su dung Jetpack va dap dat.
    private bool jetpackUsedThisFlight;
    private bool jetpackRecharging;
    private float jetpackRechargeRemaining;
    private bool wasAirborneForJetpack;

    // Co the doc thoi gian hoi tren State Authority (neu sau nay lam UI).
    public float JetpackRechargeRemaining => jetpackRechargeRemaining;


    // =========================================================
    // NETWORK ANIMATION
    // =========================================================

    [Networked]
    private float NetworkedAnimSpeed { get; set; }

    // Synced per Player. JetpackVFX reads this, not local keyboard input.
    [Networked]
    public NetworkBool IsJetpackBoosting { get; set; }

    // Sync UI meters to every client. Written only by State Authority.
    [Networked] public float JetpackFuelRemaining { get; set; }
    [Networked] public float JetpackCooldownRemaining { get; set; }


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

        SetupFootstepAudio();
        ResetFootstepTracking();
        jumpQueued = false;
        jumpQueuedUntil = 0f;
        jumpBufferRemaining = 0f;
        coyoteRemaining = 0f;
        jumpLockRemaining = 0f;
        serverSpaceWasHeld = false;
        jetpackSecondPressActivated = false;
        jetpackUsedThisFlight = false;
        jetpackRecharging = false;
        jetpackRechargeRemaining = 0f;
        wasAirborneForJetpack = false;
        jetpackHeldGraceRemaining = 0f;

        if (HasStateAuthority)
        {
            IsJetpackBoosting = false;
            JetpackFuelRemaining = Mathf.Max(0.1f, maxBoostTime);
            JetpackCooldownRemaining = 0f;
        }

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
            jumpQueuedUntil = Time.unscaledTime + Mathf.Max(0.02f, jumpBufferTime);
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


        // A queued press survives short frame/tick differences, but is sent once.
        data.jumpPressed = jumpQueued && Time.unscaledTime <= jumpQueuedUntil;
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
            jetpackSecondPressActivated = false;

            IsJetpackBoosting = false;
            JetpackCooldownRemaining = 0f;
            JetpackFuelRemaining = Mathf.Max(0.1f, maxBoostTime);

            NetworkedAnimSpeed =
                0f;
            NetworkedGrounded = false;
            NetworkedMoving = false;
            NetworkedRunning = false;


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


        // Chuyen dong ngang va doc se duoc Move cung mot lan sau khi xu ly jump.
        // Tranh isGrounded bi ghi de boi mot lenh Move chi theo phuong ngang.


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




        // =====================================================
        // RELIABLE JUMP: GROUND PROBE + BUTTON EDGE + BUFFER
        // =====================================================

        float dt = Runner.DeltaTime;
        if (jetpackRecharging)
        {
            jetpackRechargeRemaining = Mathf.Max(0f, jetpackRechargeRemaining - dt);
            if (jetpackRechargeRemaining <= 0f)
                jetpackRecharging = false;
        }

        bool controllerReady = characterController != null && characterController.enabled;
        // The controller's isGrounded is based on the most recent Move. A probe
        // also works on frames where the character is standing still.
        bool grounded = controllerReady && IsGroundedReliable();

        jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - dt);
        jumpLockRemaining = Mathf.Max(0f, jumpLockRemaining - dt);
        coyoteRemaining = grounded
            ? Mathf.Max(0f, coyoteTime)
            : Mathf.Max(0f, coyoteRemaining - dt);

        bool spaceHeld = data.isJumping;
        bool freshPress = (spaceHeld && !serverSpaceWasHeld) ||
                          (data.jumpPressed && !serverSpaceWasHeld);
        serverSpaceWasHeld = spaceHeld;

        // Second distinct press, after releasing the first jump press.
        // Only while airborne; holding the first Space press never activates Jetpack.
        bool secondJetpackPress = freshPress && boostArmed &&
                                  !jetpackSecondPressActivated &&
                                  !grounded && !jetpackRecharging;

        if (secondJetpackPress)
        {
            jetpackSecondPressActivated = true;
            jetpackHeldGraceRemaining = Mathf.Max(0.05f, jetpackInputGraceTime);
            jumpBufferRemaining = 0f;
        }
        else if (freshPress)
        {
            jumpBufferRemaining = Mathf.Max(0.02f, jumpBufferTime);
        }

        bool doJump = controllerReady &&
                      !secondJetpackPress &&
                      jumpBufferRemaining > 0f &&
                      coyoteRemaining > 0f &&
                      jumpLockRemaining <= 0f;

        if (doJump)
        {
            velocity.y = Mathf.Sqrt(
                Mathf.Max(0.01f, jumpHeight) *
                -2f * Mathf.Min(-0.01f, gravity));

            jumpBufferRemaining = 0f;
            coyoteRemaining = 0f;
            jumpLockRemaining = Mathf.Max(0.05f, jumpRepeatLockTime);
            // Van nhay binh thuong du Jetpack dang hoi.
            boostArmed = !jetpackRecharging;
            jetpackSecondPressActivated = false;
            boostTimer = 0f;
            boostStartY = transform.position.y;
            jetpackUsedThisFlight = false;
            wasAirborneForJetpack = true;
            jetpackHeldGraceRemaining = 0f;

            if (playerAnim != null)
                playerAnim.Jump();
        }
        else if (grounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            boostArmed = false;
            jetpackSecondPressActivated = false;
        }


        // =====================================================
        // SMOOTH JETPACK BOOST (SERVER / STATE AUTHORITY)
        // =====================================================

        bool isBoosting = false;

        if (boostArmed && jetpackSecondPressActivated)
        {
            // Hold the second press to fly; short lost network input is tolerated.
            if (spaceHeld)
                jetpackHeldGraceRemaining = Mathf.Max(0.05f, jetpackInputGraceTime);
            else
                jetpackHeldGraceRemaining = Mathf.Max(0f, jetpackHeldGraceRemaining - dt);

            bool heldSecondPress = spaceHeld || jetpackHeldGraceRemaining > 0f;
            bool underTime = boostTimer < Mathf.Max(0.1f, maxBoostTime);
            isBoosting = heldSecondPress && underTime && !jetpackRecharging;

            if (isBoosting)
            {
                jetpackUsedThisFlight = true;
                float heightLimit = Mathf.Max(0.1f, maxBoostHeight);
                float heightGained = transform.position.y - boostStartY;

                if (heightGained >= heightLimit)
                {
                    // Hover at 6m; keep consuming the 7 second flight budget.
                    velocity.y = 0f;
                }
                else
                {
                    float targetUpSpeed = Mathf.Max(0f, boostSpeed);
                    if (velocity.y < targetUpSpeed)
                        velocity.y = Mathf.MoveTowards(velocity.y, targetUpSpeed,
                            Mathf.Max(0.1f, boostAcceleration) * dt);
                }

                boostTimer = Mathf.Min(Mathf.Max(0.1f, maxBoostTime), boostTimer + dt);
            }
            else
            {
                // Releasing the second press or exhausting fuel ends this flight.
                // Cannot restart in mid-air by tapping Space again.
                boostArmed = false;
                jetpackSecondPressActivated = false;
                jetpackHeldGraceRemaining = 0f;
            }
        }

        // =====================================================
        // GRAVITY / MAX FALL SPEED
        // =====================================================

        // A powered jet maintains lift. When not boosting, gravity resumes.
        // (boostGravityScale remains in Inspector for backwards compatibility.)
        float effectiveGravity = isBoosting ? 0f : gravity;

        velocity.y += effectiveGravity * Runner.DeltaTime;
        velocity.y = Mathf.Max(velocity.y, -Mathf.Max(1f, maxFallSpeed));


        if (
            characterController != null &&
            characterController.enabled
        )
        {
            Vector3 horizontal = Vector3.ClampMagnitude(data.moveDirection, 1f) * speed;
            float verticalStep = velocity.y * dt;

            if (isBoosting)
            {
                // Gioi han DICH CHUYEN cua tick nay, tranh bay vuot 6m
                // khi van toc truoc do dang lon.
                float maxY = boostStartY + Mathf.Max(0.1f, maxBoostHeight);
                float remainingRise = Mathf.Max(0f, maxY - transform.position.y);
                verticalStep = Mathf.Min(verticalStep, remainingRise);
                if (remainingRise <= 0.001f)
                    velocity.y = 0f;
            }

            Vector3 movementStep = horizontal * dt + Vector3.up * verticalStep;
            CollisionFlags flags = characterController.Move(movementStep);

            // Hit ceiling: stop rising to avoid sticky ceiling.
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            {
                velocity.y = 0f;
                boostArmed = false;
                jetpackSecondPressActivated = false;
                jetpackHeldGraceRemaining = 0f;
            }

            bool landed = (flags & CollisionFlags.Below) != 0 && velocity.y <= 0f;
            if (landed)
            {
                velocity.y = -2f;
                boostArmed = false;
                jetpackSecondPressActivated = false;
                jetpackHeldGraceRemaining = 0f;

                // Bat dau hoi SAU KHI cham dat, khong phai khi tha Space.
                if (wasAirborneForJetpack && jetpackUsedThisFlight)
                {
                    jetpackRecharging = jetpackRechargeTime > 0f;
                    jetpackRechargeRemaining = Mathf.Max(0f, jetpackRechargeTime);
                }
                wasAirborneForJetpack = false;
                jetpackUsedThisFlight = false;
            }
            else if (!characterController.isGrounded)
            {
                wasAirborneForJetpack = true;
            }
        }


        // Fuel decreases while the jet is powered, including 6m hover.
        // It does not refill in mid-air. Cooldown starts only after landing.
        JetpackCooldownRemaining = jetpackRecharging
            ? Mathf.Max(0f, jetpackRechargeRemaining) : 0f;
        JetpackFuelRemaining = jetpackRecharging
            ? 0f
            : (jetpackUsedThisFlight || wasAirborneForJetpack)
                ? Mathf.Max(0f, maxBoostTime - boostTimer)
                : Mathf.Max(0.1f, maxBoostTime);

        // =====================================================
        // NETWORK ANIMATION
        // =====================================================

        // State Authority publishes the ACTUAL boost state for this Player.
        // Not affected by keyboard input for other players on this client.
        IsJetpackBoosting = isBoosting && boostArmed &&
            characterController != null && characterController.enabled &&
            !characterController.isGrounded;

        NetworkedAnimSpeed =
            animationSpeed;

        NetworkedGrounded = characterController != null &&
                            characterController.enabled &&
                            characterController.isGrounded;
        NetworkedMoving = data.moveDirection.sqrMagnitude > 0.001f;
        NetworkedRunning = data.isRunning && speed > walkSpeed + 0.1f;
    }


    // =========================================================
    // GROUND CHECK: independent of horizontal movement
    // =========================================================

    private bool IsGroundedReliable()
    {
        if (characterController == null || !characterController.enabled)
            return false;

        if (characterController.isGrounded)
            return true;

        // Do not report ground while clearly rising during a jump.
        if (velocity.y > 0.5f)
            return false;

        Bounds b = characterController.bounds;
        float radius = Mathf.Max(0.06f,
            Mathf.Min(characterController.radius * 0.65f, b.extents.x * 0.7f));
        float lift = radius + 0.08f;
        Vector3 origin = new Vector3(b.center.x, b.min.y + lift, b.center.z);
        float distance = 0.08f + Mathf.Max(0.05f, groundProbeDistance);

        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down,
            groundHits, distance, ~0, QueryTriggerInteraction.Ignore);

        float minNormalY = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = groundHits[i];
            if (h.collider == null) continue;
            Transform hitTransform = h.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                continue;
            if (h.normal.y >= minNormalY - 0.02f)
                return true;
        }
        return false;
    }

    // =========================================================
    // FOOTSTEPS - RUNS LOCALLY ON EACH CLIENT IN RENDER
    // =========================================================

    private void SetupFootstepAudio()
    {
        if (footstepSource == null)
        {
            // Tạo nguồn âm riêng để không cắt ngang tiếng bắn / hit.
            GameObject audioObject = new GameObject("FootstepAudio");
            audioObject.transform.SetParent(transform, false);
            footstepSource = audioObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 1f; // 3D cho các Player khác
            footstepSource.minDistance = 1.5f;
            footstepSource.maxDistance = 22f;
            footstepSource.rolloffMode = AudioRolloffMode.Logarithmic;
        }

        footstepSource.playOnAwake = false;
        footstepSource.loop = false;
        footstepSource.volume = footstepVolume;
    }

    private void ResetFootstepTracking()
    {
        previousFootstepPosition = transform.position;
        footstepDistanceAccumulated = 0f;
        footstepInitialized = true;
        nextFootstepAllowedTime = Time.time + 0.10f;
        if (footstepSource != null && footstepSource.isPlaying)
            footstepSource.Stop();
    }

    private void UpdateFootsteps()
    {
        if (!footstepInitialized)
        {
            ResetFootstepTracking();
            return;
        }

        Vector3 position = transform.position;
        Vector3 delta = position - previousFootstepPosition;
        previousFootstepPosition = position;
        delta.y = 0f;

        // Teleport, correction lớn hoặc respawn: không tạo tiếng bước chân.
        float traveled = delta.magnitude;
        if (traveled > 2f)
        {
            footstepDistanceAccumulated = 0f;
            return;
        }

        bool canStep = NetworkedGrounded && NetworkedMoving &&
                       NetworkedAnimSpeed > 0.05f &&
                       (playerHealth == null || !playerHealth.IsDead) &&
                       (MatchManager.Instance == null || !MatchManager.Instance.MatchEnded);

        if (!canStep)
        {
            footstepDistanceAccumulated = 0f;
            return;
        }

        // Chỉ phát khi nhân vật có dịch chuyển, không phát do đứng giữ phím.
        if (traveled < 0.002f)
            return;

        float stepLength = NetworkedRunning ? runStepDistance : walkStepDistance;
        stepLength = Mathf.Max(0.1f, stepLength);

        // Khi bắt đầu đi, phát bước đầu tiên sau nửa nhịp.
        if (footstepDistanceAccumulated <= 0f)
            footstepDistanceAccumulated = stepLength * 0.5f;

        footstepDistanceAccumulated += traveled;
        if (footstepDistanceAccumulated < stepLength)
            return;

        // Chi co mot am buoc chan moi lan. Khong de tieng chong len nhau
        // khi Player chay nhanh, mang jitter hoac animation doi lien tuc.
        if (Time.time < nextFootstepAllowedTime)
        {
            footstepDistanceAccumulated = stepLength; // giu 1 buoc dang cho
            return;
        }

        if (footstepSource == null)
            return;

        if (preventFootstepOverlap && footstepSource.isPlaying)
        {
            footstepDistanceAccumulated = stepLength;
            return;
        }

        AudioClip clip = NetworkedRunning ? runFootstepSound : walkFootstepSound;
        if (clip == null)
            return;

        footstepDistanceAccumulated = 0f;
        footstepSource.clip = clip;
        footstepSource.volume = footstepVolume;
        footstepSource.Play(); // phat tu AudioSource rieng, khong chong PlayOneShot
        nextFootstepAllowedTime = Time.time + (NetworkedRunning
            ? minRunStepInterval
            : minWalkStepInterval);
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
        jetpackSecondPressActivated = false;
        jetpackHeldGraceRemaining = 0f;
        jetpackUsedThisFlight = false;
        jetpackRecharging = false;
        jetpackRechargeRemaining = 0f;
        wasAirborneForJetpack = false;

        jumpQueued = false;
        jumpQueuedUntil = 0f;
        jumpBufferRemaining = 0f;
        coyoteRemaining = 0f;
        jumpLockRemaining = 0f;
        serverSpaceWasHeld = false;

        ResetFootstepTracking();
        smoothAnimSpeed = 0f;
        animSmoothVelocity = 0f;


        if (HasStateAuthority)
        {
            IsJetpackBoosting = false;
            JetpackFuelRemaining = Mathf.Max(0.1f, maxBoostTime);
            JetpackCooldownRemaining = 0f;
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
        float targetSpeed = NetworkedAnimSpeed;
        smoothAnimSpeed = Mathf.SmoothDamp(
            smoothAnimSpeed,
            targetSpeed,
            ref animSmoothVelocity,
            animationSmoothTime,
            Mathf.Infinity,
            Time.deltaTime
        );

        if (playerAnim != null)
            playerAnim.Move(smoothAnimSpeed);

        UpdateFootsteps();
    }
}
