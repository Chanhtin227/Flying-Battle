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

    [Header("Jetpack Cooldown & Recharge")]
    [Tooltip("Thời gian để hồi đầy 100% nhiên liệu Jetpack (tính bằng giây).")]
    [Min(0.1f)] public float jetpackRechargeTime = 4f;

    [Header("Smooth Jetpack Flight")]
    [Tooltip("Gia tốc đẩy lên khi giữ Space (m/s²). Càng thấp càng êm.")]
    [Min(0.1f)] public float boostAcceleration = 18f;
    [Tooltip("Tỷ lệ trọng lực khi Jetpack đang hoạt động. 0.2 = 20% trọng lực bình thường.")]
    [Range(0f, 1f)] public float boostGravityScale = 0.2f;
    [Tooltip("Tốc độ rơi tối đa (m/s) để giảm rơi quá gắt.")]
    [Min(1f)] public float maxFallSpeed = 18f;
    
    [Header("Jetpack Anti-Spam")]
    [Tooltip("Thời gian trễ sau khi tắt Jetpack trước khi bắt đầu hồi nhiên liệu (giây).")]
    [Min(0f)] public float fuelRechargeDelay = 0.5f;
    [Tooltip("Lượng nhiên liệu tối thiểu (giây) cần có để có thể bật lại Jetpack.")]
    [Min(0f)] public float minFuelToIgnite = 0.5f;

    // =========================================================
    // MEDKIT MOVEMENT
    // =========================================================

    [Header("Medkit Movement")]
    public float smallMedkitSpeed = 1f;
    public float mediumMedkitSpeed = 0.75f;
    public float largeMedkitSpeed = 0.5f;

    // =========================================================
    // FOOTSTEPS - DISTANCE BASED
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
    // CAMERA & REFERENCES
    // =========================================================

    [Header("Camera")]
    public Transform cameraRoot;

    private CharacterController characterController;
    private PlayerAnimation playerAnim;
    private PlayerHealth playerHealth;
    private Vector3 velocity;

    // =========================================================
    // JUMP & JETPACK STATE
    // =========================================================

    private bool jumpQueued;
    private float jumpQueuedUntil;
    private float jumpBufferRemaining;
    private float coyoteRemaining;
    private float jumpLockRemaining;
    private bool serverSpaceWasHeld;
    private readonly RaycastHit[] groundHits = new RaycastHit[16];

    [Header("Ground Probe")]
    [Tooltip("Additional probe distance below the feet (meters).")]
    [Range(0.05f, 0.4f)] public float groundProbeDistance = 0.20f;

    [Header("Jetpack Input Stability")]
    [Tooltip("Cho phep mat input giu Space trong vai network tick ma khong tat Jetpack.")]
    [Range(0.05f, 0.4f)] public float jetpackInputGraceTime = 0.18f;
    private float jetpackHeldGraceRemaining;

    // --- NEW JETPACK STATE VARIABLES ---
    private float currentFuel;
    private bool isJetpackActive;
    private bool jetpackInputReady;
    private bool hasBoostStartY;
    private float boostStartY;
    private float rechargeDelayTimer;

    public float JetpackRechargeRemaining => Mathf.Max(0f, maxBoostTime - currentFuel);

    // =========================================================
    // NETWORK ANIMATION
    // =========================================================

    [Networked]
    private float NetworkedAnimSpeed { get; set; }

    [Networked]
    public NetworkBool IsJetpackBoosting { get; set; }

    [Networked] public float JetpackFuelRemaining { get; set; }
    [Networked] public float JetpackCooldownRemaining { get; set; }

    // =========================================================
    // SPAWNED & DESPAWNED
    // =========================================================

    public override void Spawned()
    {
        characterController = GetComponent<CharacterController>();
        playerAnim = GetComponent<PlayerAnimation>();
        playerHealth = GetComponent<PlayerHealth>();

        SetupFootstepAudio();
        ResetFootstepTracking();
        jumpQueued = false;
        jumpQueuedUntil = 0f;
        jumpBufferRemaining = 0f;
        coyoteRemaining = 0f;
        jumpLockRemaining = 0f;
        serverSpaceWasHeld = false;
        
        currentFuel = Mathf.Max(0.1f, maxBoostTime);
        isJetpackActive = false;
        jetpackInputReady = true;
        hasBoostStartY = false;
        jetpackHeldGraceRemaining = 0f;
        rechargeDelayTimer = 0f;

        if (HasStateAuthority)
        {
            IsJetpackBoosting = false;
            JetpackFuelRemaining = currentFuel;
            JetpackCooldownRemaining = 0f;
        }

        if (HasInputAuthority)
        {
            LocalPlayer = this;
        }

        if (!HasStateAuthority)
        {
            characterController.enabled = false;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
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
        if (!HasInputAuthority) return;

        if ((playerHealth != null && playerHealth.IsDead) ||
            (MatchManager.Instance != null && MatchManager.Instance.MatchEnded))
        {
            jumpQueued = false;
            return;
        }

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
        NetworkInputData data = new NetworkInputData();

        if ((playerHealth != null && playerHealth.IsDead) ||
            (MatchManager.Instance != null && MatchManager.Instance.MatchEnded))
        {
            jumpQueued = false;
            return data;
        }

        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (cameraRoot != null)
        {
            Vector3 forward = cameraRoot.forward;
            Vector3 right = cameraRoot.right;

            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            data.moveDirection = Vector3.ClampMagnitude(forward * vertical + right * horizontal, 1f);
            data.lookDirection = forward;
        }

        data.isRunning = Input.GetKey(KeyCode.LeftShift);
        data.isJumping = Input.GetKey(KeyCode.Space);

        data.jumpPressed = jumpQueued && Time.unscaledTime <= jumpQueuedUntil;
        jumpQueued = false;

        return data;
    }

    // =========================================================
    // FIXED UPDATE NETWORK
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        // --- DEAD / MATCH END ---
        if ((playerHealth != null && playerHealth.IsDead) ||
            (MatchManager.Instance != null && MatchManager.Instance.MatchEnded))
        {
            velocity = Vector3.zero;
            
            isJetpackActive = false;
            IsJetpackBoosting = false;
            JetpackCooldownRemaining = 0f;
            JetpackFuelRemaining = Mathf.Max(0.1f, maxBoostTime);

            NetworkedAnimSpeed = 0f;
            NetworkedGrounded = false;
            NetworkedMoving = false;
            NetworkedRunning = false;

            if (playerAnim != null) playerAnim.Move(0f);
            return;
        }

        if (!GetInput(out NetworkInputData data)) return;

        // --- ROTATION ---
        if (data.lookDirection.sqrMagnitude > 0.001f)
        {
            transform.rotation = Quaternion.LookRotation(data.lookDirection, Vector3.up);
        }

        // --- SPEED ---
        float speed = data.isRunning ? runSpeed : walkSpeed;
        if (playerHealth != null && playerHealth.UsingMedkitType != 0)
        {
            speed = GetMedkitSpeed(playerHealth.UsingMedkitType);
        }

        // --- ANIMATION SPEED ---
        float animationSpeed = 0f;
        if (data.moveDirection.sqrMagnitude > 0.001f)
        {
            animationSpeed = (speed == walkSpeed) ? data.moveDirection.magnitude * 0.5f : data.moveDirection.magnitude;
        }

        float dt = Runner.DeltaTime;

        bool controllerReady = characterController != null && characterController.enabled;
        bool grounded = controllerReady && IsGroundedReliable();

        // Reset the boost height reference when grounded
        if (grounded) hasBoostStartY = false;

        jumpBufferRemaining = Mathf.Max(0f, jumpBufferRemaining - dt);
        jumpLockRemaining = Mathf.Max(0f, jumpLockRemaining - dt);
        coyoteRemaining = grounded ? Mathf.Max(0f, coyoteTime) : Mathf.Max(0f, coyoteRemaining - dt);

        bool spaceHeld = data.isJumping;
        bool freshPress = (spaceHeld && !serverSpaceWasHeld) ||
                          (data.jumpPressed && !serverSpaceWasHeld);
        serverSpaceWasHeld = spaceHeld;

        if (freshPress)
        {
            jumpBufferRemaining = Mathf.Max(0.02f, jumpBufferTime);
        } 
        
        // Tín hiệu người chơi đã thả phím, sẵn sàng kích hoạt Jetpack cho lần ấn kế tiếp
        if (!spaceHeld)
        {
            jetpackInputReady = true;
        }

        bool doJump = controllerReady &&
                      jumpBufferRemaining > 0f &&
                      coyoteRemaining > 0f &&
                      jumpLockRemaining <= 0f;

        // --- JUMP LOGIC ---
        if (doJump)
        {
            velocity.y = Mathf.Sqrt(Mathf.Max(0.01f, jumpHeight) * -2f * Mathf.Min(-0.01f, gravity));
            jumpBufferRemaining = 0f;
            coyoteRemaining = 0f;
            jumpLockRemaining = Mathf.Max(0.05f, jumpRepeatLockTime);

            // Cần nhả phím rồi ấn lại mới được bay
            jetpackInputReady = false; 
            isJetpackActive = false;
            jetpackHeldGraceRemaining = 0f;

            boostStartY = transform.position.y;
            hasBoostStartY = true;

            if (playerAnim != null) playerAnim.Jump();
        }
        else if (grounded && velocity.y <= 0f)
        {
            velocity.y = -2f;
            isJetpackActive = false;
        }

        // =====================================================
        // NEW SMOOTH MID-AIR JETPACK BOOST
        // =====================================================

        // Kích hoạt Jetpack nếu ở trên không, đã thả phím ra trước đó, có tín hiệu ấn phím mới 
        // VÀ nhiên liệu phải lớn hơn hoặc bằng ngưỡng đánh lửa (minFuelToIgnite)
        if (!grounded && jetpackInputReady && freshPress)
        {
            if (currentFuel >= minFuelToIgnite)
            {
                isJetpackActive = true;
                jetpackHeldGraceRemaining = Mathf.Max(0.05f, jetpackInputGraceTime);

                if (!hasBoostStartY)
                {
                    boostStartY = transform.position.y;
                    hasBoostStartY = true;
                }
            }
        }

        bool isBoosting = false;

        if (isJetpackActive)
        {
            if (spaceHeld)
                jetpackHeldGraceRemaining = Mathf.Max(0.05f, jetpackInputGraceTime);
            else
                jetpackHeldGraceRemaining = Mathf.Max(0f, jetpackHeldGraceRemaining - dt);

            bool heldSecondPress = spaceHeld || jetpackHeldGraceRemaining > 0f;

            if (heldSecondPress && currentFuel > 0f)
            {
                isBoosting = true;
                rechargeDelayTimer = fuelRechargeDelay; // Đang bay thì luôn đặt lại đồng hồ đếm trễ nạp
            }
            else
            {
                // Hết nhiên liệu hoặc thả phím -> ngắt Jetpack
                isJetpackActive = false; 
                jetpackHeldGraceRemaining = 0f;
            }
        }

        if (isBoosting)
        {
            // Tiêu hao nhiên liệu đều đặn (tính theo giây)
            currentFuel = Mathf.Max(0f, currentFuel - dt);

            float heightLimit = Mathf.Max(0.1f, maxBoostHeight);
            float heightGained = transform.position.y - boostStartY;

            if (heightGained >= heightLimit)
            {
                velocity.y = 0f; // Đạt độ cao giới hạn thì lơ lửng (hover)
            }
            else
            {
                float targetUpSpeed = Mathf.Max(0f, boostSpeed);
                if (velocity.y < targetUpSpeed)
                    velocity.y = Mathf.MoveTowards(velocity.y, targetUpSpeed, Mathf.Max(0.1f, boostAcceleration) * dt);
            }
        }
        else
        {
            // Trễ nạp (Recharge Delay): Đếm ngược thời gian trễ trước khi bắt đầu hồi
            if (rechargeDelayTimer > 0f)
            {
                rechargeDelayTimer -= dt;
            }
            else
            {
                // Tự động hồi nhiên liệu ngay lập tức sau khi hết thời gian chờ
                float maxFuel = Mathf.Max(0.1f, maxBoostTime);
                float rechargeRate = maxFuel / Mathf.Max(0.1f, jetpackRechargeTime);
                currentFuel = Mathf.Min(maxFuel, currentFuel + (rechargeRate * dt));
            }
        }

        // =====================================================
        // GRAVITY & APPLY MOVEMENT
        // =====================================================
        float effectiveGravity = isBoosting ? 0f : gravity;
        velocity.y += effectiveGravity * dt;
        velocity.y = Mathf.Max(velocity.y, -Mathf.Max(1f, maxFallSpeed));

        if (characterController != null && characterController.enabled)
        {
            Vector3 horizontal = Vector3.ClampMagnitude(data.moveDirection, 1f) * speed;
            float verticalStep = velocity.y * dt;

            if (isBoosting)
            {
                float maxY = boostStartY + Mathf.Max(0.1f, maxBoostHeight);
                float remainingRise = Mathf.Max(0f, maxY - transform.position.y);
                verticalStep = Mathf.Min(verticalStep, remainingRise);
                if (remainingRise <= 0.001f) velocity.y = 0f;
            }

            Vector3 movementStep = horizontal * dt + Vector3.up * verticalStep;
            CollisionFlags flags = characterController.Move(movementStep);

            // Chạm trần
            if ((flags & CollisionFlags.Above) != 0 && velocity.y > 0f)
            {
                velocity.y = 0f;
                isJetpackActive = false;
                jetpackHeldGraceRemaining = 0f;
            }

            // Chạm đất
            bool landed = (flags & CollisionFlags.Below) != 0 && velocity.y <= 0f;
            if (landed)
            {
                velocity.y = -2f;
                isJetpackActive = false;
                jetpackHeldGraceRemaining = 0f;
            }
        }

        // =====================================================
        // NETWORK ANIMATION SYNC
        // =====================================================
        JetpackCooldownRemaining = 0f; // Không dùng cooldown khoá cứng nữa
        JetpackFuelRemaining = currentFuel;
        IsJetpackBoosting = isBoosting && characterController != null && 
                            characterController.enabled && !characterController.isGrounded;

        NetworkedAnimSpeed = animationSpeed;
        NetworkedGrounded = characterController != null &&
                            characterController.enabled &&
                            characterController.isGrounded;
        NetworkedMoving = data.moveDirection.sqrMagnitude > 0.001f;
        NetworkedRunning = data.isRunning && speed > walkSpeed + 0.1f;
    }

    // =========================================================
    // GROUND CHECK
    // =========================================================

    private bool IsGroundedReliable()
    {
        if (characterController == null || !characterController.enabled) return false;
        if (characterController.isGrounded) return true;

        if (velocity.y > 0.5f) return false;

        Bounds b = characterController.bounds;
        float radius = Mathf.Max(0.06f, Mathf.Min(characterController.radius * 0.65f, b.extents.x * 0.7f));
        float lift = radius + 0.08f;
        Vector3 origin = new Vector3(b.center.x, b.min.y + lift, b.center.z);
        float distance = 0.08f + Mathf.Max(0.05f, groundProbeDistance);

        int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, groundHits, distance, ~0, QueryTriggerInteraction.Ignore);
        float minNormalY = Mathf.Cos(characterController.slopeLimit * Mathf.Deg2Rad);
        
        for (int i = 0; i < count; i++)
        {
            RaycastHit h = groundHits[i];
            if (h.collider == null) continue;
            Transform hitTransform = h.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform)) continue;
            if (h.normal.y >= minNormalY - 0.02f) return true;
        }
        return false;
    }

    // =========================================================
    // FOOTSTEPS
    // =========================================================

    private void SetupFootstepAudio()
    {
        if (footstepSource == null)
        {
            GameObject audioObject = new GameObject("FootstepAudio");
            audioObject.transform.SetParent(transform, false);
            footstepSource = audioObject.AddComponent<AudioSource>();
            footstepSource.spatialBlend = 1f;
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
        if (footstepSource != null && footstepSource.isPlaying) footstepSource.Stop();
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

        if (traveled < 0.002f) return;

        float stepLength = NetworkedRunning ? runStepDistance : walkStepDistance;
        stepLength = Mathf.Max(0.1f, stepLength);

        if (footstepDistanceAccumulated <= 0f) footstepDistanceAccumulated = stepLength * 0.5f;

        footstepDistanceAccumulated += traveled;
        if (footstepDistanceAccumulated < stepLength) return;

        if (Time.time < nextFootstepAllowedTime)
        {
            footstepDistanceAccumulated = stepLength; 
            return;
        }

        if (footstepSource == null) return;
        if (preventFootstepOverlap && footstepSource.isPlaying)
        {
            footstepDistanceAccumulated = stepLength;
            return;
        }

        AudioClip clip = NetworkedRunning ? runFootstepSound : walkFootstepSound;
        if (clip == null) return;

        footstepDistanceAccumulated = 0f;
        footstepSource.clip = clip;
        footstepSource.volume = footstepVolume;
        footstepSource.Play(); 
        
        nextFootstepAllowedTime = Time.time + (NetworkedRunning ? minRunStepInterval : minWalkStepInterval);
    }

    // =========================================================
    // RESET MOVEMENT
    // =========================================================

    public void ResetMovementState()
    {
        velocity = Vector3.zero;

        // Reset states
        currentFuel = Mathf.Max(0.1f, maxBoostTime);
        isJetpackActive = false;
        jetpackInputReady = true;
        hasBoostStartY = false;
        jetpackHeldGraceRemaining = 0f;
        rechargeDelayTimer = 0f;

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
            JetpackFuelRemaining = currentFuel;
            JetpackCooldownRemaining = 0f;
            NetworkedAnimSpeed = 0f;
        }
    }

    // =========================================================
    // MEDKIT SPEED
    // =========================================================

    private float GetMedkitSpeed(int medkitType)
    {
        switch (medkitType)
        {
            case 1: return smallMedkitSpeed;
            case 2: return mediumMedkitSpeed;
            case 3: return largeMedkitSpeed;
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

        if (playerAnim != null) playerAnim.Move(smoothAnimSpeed);
        UpdateFootsteps();
    }
}