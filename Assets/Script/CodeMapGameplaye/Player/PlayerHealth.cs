using UnityEngine;
using Fusion;
using System.Collections;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }

    [Networked] public int Kills { get; set; }

    [Networked] public PlayerRef LastAttacker { get; set; }


    [Header("Medkit Inventory")]
    [Networked] public int SmallMedkitCount { get; set; }
    [Networked] public int MediumMedkitCount { get; set; }
    [Networked] public int LargeMedkitCount { get; set; }


    // =========================================================
    // MEDKIT DROP PREFABS
    // =========================================================

    [Header("Medkit Drop Prefabs")]

    public NetworkObject smallMedkitDropPrefab;
    public NetworkObject mediumMedkitDropPrefab;
    public NetworkObject largeMedkitDropPrefab;


    // =========================================================
    // MEDKIT DROP SETTINGS
    // =========================================================

    [Header("Medkit Drop Settings")]

    public float medkitDropDistance = 1.2f;
    public float medkitDropHeight = 0.5f;


    [Header("Medkit Heal Amount")]
    public float smallHealAmount = 50f;
    public float mediumHealAmount = 75f;
    public float largeHealAmount = 100f;


    [Header("Medkit Use Time")]
    public float smallUseTime = 2f;
    public float mediumUseTime = 3f;
    public float largeUseTime = 4f;


    [Header("Medkit Input")]
    public KeyCode useMedkitKey = KeyCode.E;

    [Networked] public int UsingMedkitType { get; set; }
    [Networked] public TickTimer MedkitTimer { get; set; }


    [Header("Random Respawn")]
    public float respawnDelay = 5f;

    [Tooltip("Tỉ lệ vùng giữa map được phép respawn. 0.5 = chỉ dùng 50% khu vực giữa Terrain.")]
    [Range(0.1f, 1f)]
    public float centerRespawnPercent = 0.5f;

    [Tooltip("Khoảng cách tối thiểu giữa Player vừa hồi sinh và Player khác.")]
    public float minRespawnDistance = 30f;

    [Tooltip("Khoảng cách tối thiểu so với vị trí vừa chết.")]
    public float minDistanceFromDeathPosition = 20f;

    [Tooltip("Số lần thử tìm vị trí hồi sinh an toàn.")]
    public int respawnTryCount = 50;

    [Tooltip("Nâng Player lên khỏi mặt Terrain một chút.")]
    public float respawnHeightOffset = 1f;

    [Tooltip("Độ dốc tối đa cho phép hồi sinh. Nhỏ hơn sẽ tránh sườn núi.")]
    [Range(0f, 60f)]
    public float maxRespawnSlope = 25f;

    [Networked] public TickTimer RespawnTimer { get; set; }

    [Header("Respawn Invulnerability")]
    [Min(0f)]
    public float respawnInvulnerabilityDuration = 5f;

    [Networked] public TickTimer InvulnerabilityTimer { get; set; }

    // Timer được State Authority tạo và Fusion đồng bộ cho các máy.
    public bool IsInvulnerable =>
        Object != null && Object.IsValid && Runner != null &&
        !IsDead && !InvulnerabilityTimer.ExpiredOrNotRunning(Runner);

    // Vị trí Player vừa chết để tránh hồi sinh lại gần chỗ cũ
    private Vector3 lastDeathPosition;


    [Header("Respawn Lock")]
    [Tooltip("Kéo script điều khiển camera của Player vào đây. Có thể để trống nếu script tên ThirdPersonCamera.")]
    public MonoBehaviour playerCameraController;

    private Terrain respawnTerrain;

    private bool localRespawnLocked = true;

    // =========================================================
    // DEATH CAMERA
    // =========================================================

    [Header("Death Camera")]

    [Tooltip("Camera của local player. Có thể để trống.")]
    public Camera deathCamera;

    [Tooltip("UI hồi sinh. UI này chỉ hiện sau khi camera bay lên xong.")]
    public GameObject respawnPanel;

    [Tooltip("Camera bay cao bao nhiêu mét so với vị trí chết.")]
    public float deathCameraHeight = 9f;

    [Tooltip("Thời gian camera bay từ vị trí hiện tại lên trên.")]
    public float deathCameraRiseDuration = 2.2f;

    [Tooltip("Giữ góc nhìn trên cao trước khi hiện Respawn UI.")]
    public float deathCameraHoldDuration = 0.6f;

    [Tooltip("Độ lệch camera. Z âm sẽ tạo góc nhìn hơi xiên.")]
    public Vector3 deathCameraOffset =
        new Vector3(
            0f,
            0f,
            -2f
        );

    [Tooltip("Camera nhìn vào vị trí cao hơn chân Player một chút.")]
    public float deathCameraLookHeight = 0.8f;


    private Coroutine deathCameraCoroutine;
    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!HasInputAuthority)
            return;


        // MATCH END = KHÓA TOÀN BỘ CONTROL VÀ KHÔNG BẬT LẠI
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            SetLocalRespawnLock(true);

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            return;
        }


        if (IsDead)
        {
            SetLocalRespawnLock(true);
            return;
        }

        SetLocalRespawnLock(false);

        if (UsingMedkitType == 0 &&
            Input.GetKeyDown(useMedkitKey))
        {
            RequestUseMedkitRpc();
        }
    }


    // =========================================================
    // FIXED UPDATE NETWORK
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;


        // Khi trận đã kết thúc thì không heal / không respawn nữa.
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }


        if (IsDead)
        {
            if (RespawnTimer.Expired(Runner))
            {
                RespawnPlayer();
            }

            return;
        }

        if (UsingMedkitType != 0 &&
            MedkitTimer.Expired(Runner))
        {
            FinishUseMedkit();
        }
    }


    // =========================================================
    // LOCAL RESPAWN LOCK
    // =========================================================

    private void SetLocalRespawnLock(bool locked)
    {
        if (!HasInputAuthority)
            return;

        if (localRespawnLocked == locked)
            return;

        localRespawnLocked = locked;


        // Tìm camera controller nếu chưa được kéo vào Inspector
        if (playerCameraController == null)
        {
            FindCameraController();
        }


        // Lock / Unlock movement
        if (TryGetComponent(out PlayerMovement movement))
        {
            if (!locked)
            {
                movement.ResetMovementState();
            }

            movement.enabled = !locked;
        }


        // Lock / Unlock weapon
        if (TryGetComponent(out PlayerWeapon weapon))
        {
            weapon.enabled = !locked;

            if (!locked)
            {
                weapon.RefreshWeaponVisuals();
            }
        }


        // CharacterController chỉ được StateAuthority điều khiển
        if (HasStateAuthority &&
            TryGetComponent(out CharacterController controller))
        {
            controller.enabled = !locked;
        }


        // Lock / Unlock camera controller
        if (playerCameraController != null)
        {
            playerCameraController.enabled = !locked;
        }


        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    /// <summary>
    /// Tìm script camera mà không cần reference trực tiếp tới
    /// ThirdPersonCamera, tránh lỗi compile nếu đổi tên class.
    /// </summary>
    private void FindCameraController()
    {
        ThirdPersonCamera[] cameraControllers =
            FindObjectsByType<ThirdPersonCamera>(
                FindObjectsInactive.Include
            );

        foreach (ThirdPersonCamera cameraController
                 in cameraControllers)
        {
            if (cameraController == null)
                continue;

            if (cameraController.target == null)
                continue;


            Transform target =
                cameraController.target;


            bool belongsToThisPlayer =
                target == transform
                ||
                target.IsChildOf(transform)
                ||
                transform.IsChildOf(target);


            if (!belongsToThisPlayer)
                continue;


            playerCameraController =
                cameraController;

            return;
        }


        Debug.LogWarning(
            "[PlayerHealth] Không tìm thấy ThirdPersonCamera của Local Player."
        );
    }


    // =========================================================
    // MEDKIT
    // =========================================================

    public int GetBestMedkitType()
    {
        if (CurrentHealth >= maxHealth ||
            UsingMedkitType != 0 ||
            IsDead)
        {
            return 0;
        }


        float missingHealth = maxHealth - CurrentHealth;


        if (SmallMedkitCount > 0 &&
            missingHealth <= smallHealAmount)
        {
            return 1;
        }


        if (MediumMedkitCount > 0 &&
            missingHealth <= mediumHealAmount)
        {
            return 2;
        }


        if (LargeMedkitCount > 0 &&
            missingHealth <= largeHealAmount)
        {
            return 3;
        }


        // Nếu lượng máu thiếu lớn hơn khả năng hồi
        // ưu tiên medkit lớn nhất đang có
        if (LargeMedkitCount > 0)
            return 3;

        if (MediumMedkitCount > 0)
            return 2;

        if (SmallMedkitCount > 0)
            return 1;


        return 0;
    }


    private float GetHealAmount(int medkitType)
    {
        return medkitType switch
        {
            1 => smallHealAmount,
            2 => mediumHealAmount,
            3 => largeHealAmount,
            _ => 0f
        };
    }


    public float GetMedkitUseTime(int medkitType)
    {
        return medkitType switch
        {
            1 => smallUseTime,
            2 => mediumUseTime,
            3 => largeUseTime,
            _ => 0f
        };
    }


    private bool HasMedkit(int medkitType)
    {
        return medkitType switch
        {
            1 => SmallMedkitCount > 0,
            2 => MediumMedkitCount > 0,
            3 => LargeMedkitCount > 0,
            _ => false
        };
    }


    // =========================================================
    // REQUEST USE MEDKIT
    // =========================================================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestUseMedkitRpc()
    {
        if (!HasStateAuthority)
            return;

        if (IsDead)
            return;

        if (UsingMedkitType != 0)
            return;

        if (CurrentHealth >= maxHealth)
            return;


        int medkitType = GetBestMedkitType();


        if (medkitType == 0)
            return;

        if (!HasMedkit(medkitType))
            return;


        UsingMedkitType = medkitType;

        MedkitTimer = TickTimer.CreateFromSeconds(
            Runner,
            GetMedkitUseTime(medkitType)
        );
    }


    // =========================================================
    // FINISH MEDKIT
    // =========================================================

    private void FinishUseMedkit()
    {
        if (UsingMedkitType == 0)
            return;


        if (IsDead ||
            !HasMedkit(UsingMedkitType))
        {
            CancelMedkit();
            return;
        }


        CurrentHealth = Mathf.Clamp(
            CurrentHealth + GetHealAmount(UsingMedkitType),
            0f,
            maxHealth
        );


        switch (UsingMedkitType)
        {
            case 1:
                SmallMedkitCount--;
                break;

            case 2:
                MediumMedkitCount--;
                break;

            case 3:
                LargeMedkitCount--;
                break;
        }


        Rpc_PlayHeal();

        CancelMedkit();
    }


    private void CancelMedkit()
    {
        UsingMedkitType = 0;
        MedkitTimer = TickTimer.None;
    }


    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayHeal()
    {
        // Có thể thêm animation / sound heal ở đây
    }


    // =========================================================
    // INVENTORY UI - USE SELECTED MEDKIT
    // =========================================================

    public void UseMedkitFromInventoryUI(int medkitType)
    {
        if (!HasInputAuthority)
            return;

        if (medkitType < 1 || medkitType > 3)
            return;

        RequestUseSpecificMedkitRpc(medkitType);
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestUseSpecificMedkitRpc(int medkitType)
    {
        if (!HasStateAuthority || IsDead)
            return;

        if (UsingMedkitType != 0)
            return;

        if (CurrentHealth >= maxHealth)
            return;

        if (!HasMedkit(medkitType))
            return;

        UsingMedkitType = medkitType;

        MedkitTimer = TickTimer.CreateFromSeconds(
            Runner,
            GetMedkitUseTime(medkitType)
        );
    }


    // =========================================================
    // INVENTORY UI - THROW 1 MEDKIT
    // =========================================================

    public void ThrowMedkitFromInventoryUI(int medkitType)
    {
        if (!HasInputAuthority)
            return;

        if (medkitType < 1 || medkitType > 3)
            return;

        RequestThrowMedkitRpc(medkitType);
    }


    // =========================================================
    // THROW MEDKIT RPC
    // =========================================================

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestThrowMedkitRpc(int medkitType)
    {
        if (!HasStateAuthority || IsDead)
            return;

        if (UsingMedkitType != 0)
            return;

        NetworkObject dropPrefab = null;
        MedkitPickup.MedkitType pickupType;

        switch (medkitType)
        {
            case 1:
                if (SmallMedkitCount <= 0)
                    return;

                dropPrefab = smallMedkitDropPrefab;
                pickupType = MedkitPickup.MedkitType.Small;
                break;

            case 2:
                if (MediumMedkitCount <= 0)
                    return;

                dropPrefab = mediumMedkitDropPrefab;
                pickupType = MedkitPickup.MedkitType.Medium;
                break;

            case 3:
                if (LargeMedkitCount <= 0)
                    return;

                dropPrefab = largeMedkitDropPrefab;
                pickupType = MedkitPickup.MedkitType.Large;
                break;

            default:
                return;
        }

        if (dropPrefab == null)
        {
            Debug.LogError(
                "[PlayerHealth] CHƯA GÁN MEDKIT DROP PREFAB | Type = " +
                medkitType
            );
            return;
        }

        Vector3 dropPosition =
            transform.position +
            transform.forward * medkitDropDistance +
            Vector3.up * medkitDropHeight;

        NetworkObject droppedObject =
            Runner.Spawn(
                dropPrefab,
                dropPosition,
                Quaternion.identity
            );

        if (droppedObject == null)
        {
            Debug.LogError(
                "[PlayerHealth] KHÔNG SPAWN ĐƯỢC MEDKIT DROP!"
            );
            return;
        }

        MedkitPickup pickup =
            droppedObject.GetComponent<MedkitPickup>();

        if (pickup == null)
        {
            Debug.LogError(
                "[PlayerHealth] MEDKIT DROP PREFAB KHÔNG CÓ MedkitPickup!"
            );

            Runner.Despawn(droppedObject);
            return;
        }

        pickup.SetupDroppedMedkit(pickupType);

        // Chỉ trừ inventory sau khi spawn thành công.
        switch (medkitType)
        {
            case 1:
                SmallMedkitCount--;
                break;

            case 2:
                MediumMedkitCount--;
                break;

            case 3:
                LargeMedkitCount--;
                break;
        }

        Debug.Log(
            "[PlayerHealth] THROW MEDKIT THÀNH CÔNG | " +
            pickupType
        );
    }


    // =========================================================
    // ADD MEDKIT
    // =========================================================

    public void AddMedkit(
        MedkitPickup.MedkitType type,
        float healAmount)
    {
        if (!HasStateAuthority)
            return;


        switch (type)
        {
            case MedkitPickup.MedkitType.Small:
                SmallMedkitCount++;
                break;

            case MedkitPickup.MedkitType.Medium:
                MediumMedkitCount++;
                break;

            case MedkitPickup.MedkitType.Large:
                LargeMedkitCount++;
                break;
        }
    }


    public int GetTotalMedkitCount()
    {
        return SmallMedkitCount
             + MediumMedkitCount
             + LargeMedkitCount;
    }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        FindRespawnTerrain();


        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;

            IsDead = false;

            Kills = 0;
            LastAttacker = PlayerRef.None;

            SmallMedkitCount = 0;
            MediumMedkitCount = 0;
            LargeMedkitCount = 0;

            UsingMedkitType = 0;

            MedkitTimer = TickTimer.None;
            RespawnTimer = TickTimer.None;
            InvulnerabilityTimer = TickTimer.None;
        }


        if (HasInputAuthority)
        {
            localRespawnLocked = true;
            if (respawnPanel != null)
            {
                respawnPanel.SetActive(false);
            }


            SetLocalRespawnLock(false);
        }
    }


    // =========================================================
    // FIND TERRAIN
    // =========================================================

    private void FindRespawnTerrain()
    {
        if (respawnTerrain != null)
            return;


        respawnTerrain = Terrain.activeTerrain;


        // FIX:
        // FindAnyObjectByType() phải có generic type
        if (respawnTerrain == null)
        {
            respawnTerrain =
                FindAnyObjectByType<Terrain>();
        }
    }


    // =========================================================
    // DAMAGE
    // =========================================================

    public void TakeDamage(float damage)
    {
        TakeDamage(
            damage,
            transform.position,
            PlayerRef.None
        );
    }

    // DAMAGE WITH ATTACKER POSITION

    public void TakeDamage(
        float damage,
        Vector3 attackerPosition)
    {
        TakeDamage(
            damage,
            attackerPosition,
            PlayerRef.None
        );
    }

    // DAMAGE WITH ATTACKER

    public void TakeDamage(
        float damage,
        Vector3 attackerPosition,
        PlayerRef attacker)
    {
        // ONLY STATE AUTHORITY

        if (!HasStateAuthority)
            return;

        // DEAD / INVULNERABLE

        if (IsDead || IsInvulnerable)
            return;

        // VALID DAMAGE

        damage =
            Mathf.Max(
                damage,
                0f
            );


        if (damage <= 0f)
            return;

        // SAVE HEALTH BEFORE DAMAGE

        float healthBeforeDamage =
            CurrentHealth;

        // SAVE ATTACKER

        if (attacker != PlayerRef.None &&
            attacker != Object.InputAuthority)
        {
            LastAttacker =
                attacker;
        }

        // APPLY DAMAGE

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth - damage,
                0f,
                maxHealth
            );

        // ACTUAL DAMAGE
        //
        // Ví dụ:
        // Enemy còn 10 HP
        // Rifle gây 25
        // => Total Damage chỉ +10

        float actualDamage =
            Mathf.Max(
                0f,
                healthBeforeDamage -
                CurrentHealth
            );

        // =====================================================
        // ADD DAMAGE TAKEN TO VICTIM STATS
        // =====================================================

        if (actualDamage > 0f)
        {
            MatchStatsTracker victimTracker =
                GetComponent<MatchStatsTracker>();


            if (victimTracker != null)
            {
                victimTracker.AddDamageTaken(
                    actualDamage
                );
            }
        }

        // ADD DAMAGE TO ATTACKER STATS

        if (actualDamage > 0f &&
            attacker != PlayerRef.None &&
            attacker != Object.InputAuthority)
        {
            AddDamageToAttacker(
                attacker,
                actualDamage
            );
        }

        // DEBUG

        Debug.Log(
            "[PlayerHealth] DAMAGE" +
            " | Victim = " +
            Object.InputAuthority +
            " | Attacker = " +
            attacker +
            " | Damage = " +
            damage.ToString("F0") +
            " | Actual = " +
            actualDamage.ToString("F0") +
            " | HP = " +
            CurrentHealth.ToString("F0") +
            "/" +
            maxHealth.ToString("F0")
        );

        // CANCEL MEDKIT

        if (UsingMedkitType != 0)
        {
            CancelMedkit();
        }

        // HIT / DIE

        if (CurrentHealth > 0f)
        {
            Rpc_PlayHit(
                attackerPosition
            );
        }
        else
        {
            Die();
        }
    }

    // ADD DAMAGE TO ATTACKER STATS

    private void AddDamageToAttacker(
        PlayerRef attackerRef,
        float damageAmount)
    {
        if (!HasStateAuthority)
            return;


        if (attackerRef == PlayerRef.None)
            return;


        if (damageAmount <= 0f)
            return;

        // FIND ALL PLAYERS

        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );

        // FIND ATTACKER

        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;


            if (player.Object == null)
                continue;


            if (!player.Object.IsValid)
                continue;


            if (player.Object.InputAuthority !=
                attackerRef)
            {
                continue;
            }

            // GET MATCH STATS TRACKER

            MatchStatsTracker tracker =
                player.GetComponent<MatchStatsTracker>();


            if (tracker == null)
            {
                Debug.LogWarning(
                    "[MatchStats] Player " +
                    attackerRef +
                    " chưa có MatchStatsTracker!"
                );

                return;
            }

            // ADD DAMAGE

            tracker.AddDamage(
                damageAmount
            );


            Debug.Log(
                "[MatchStats]" +
                " | Player = " +
                attackerRef +
                " | Damage +" +
                damageAmount.ToString("F0") +
                " | Total Damage = " +
                tracker.TotalDamage.ToString("F0")
            );


            return;
        }


        Debug.LogWarning(
            "[MatchStats] Không tìm thấy attacker" +
            " | PlayerRef = " +
            attackerRef
        );
    }

    // HIT RPC

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayHit(
        Vector3 attackerPosition)
    {

        // HIT ANIMATION

        if (TryGetComponent(
            out PlayerAnimation playerAnim))
        {
            playerAnim.Hit();
        }

        // DAMAGE DIRECTION UI

        if (HasInputAuthority)
        {
            DamageDirectionUI damageUI =
                FindAnyObjectByType<DamageDirectionUI>();


            if (damageUI != null)
            {
                damageUI.ShowDamage(
                    attackerPosition
                );
            }
        }
    }

    // DIE

    private void Die()
    {
        if (!HasStateAuthority)
            return;

        if (IsDead)
            return;

        CancelMedkit();

        if (TryGetComponent(out PlayerWeapon playerWeapon))
        {
            playerWeapon.CancelReloadOnDeath();
        }

        // Lưu vị trí chết TRƯỚC khi bắt đầu hồi sinh
        lastDeathPosition = transform.position;

        Debug.Log(
            "[PlayerHealth] PLAYER DIE | Player: " + Object.InputAuthority +
            " | Death Position: " + lastDeathPosition
        );

        IsDead = true;
        InvulnerabilityTimer = TickTimer.None;
        CurrentHealth = 0f;

        RespawnTimer = TickTimer.CreateFromSeconds(
            Runner,
            respawnDelay
        );

        if (HasInputAuthority)
        {
            SetLocalRespawnLock(true);
        }

        RegisterKillForLastAttacker();

        Rpc_PlayDeath();
    }

    // REGISTER SOLO KILL

    private void RegisterKillForLastAttacker()
    {
        if (!HasStateAuthority)
            return;


        PlayerRef killerRef =
            LastAttacker;


        // Reset ngay để cùng một mạng không thể cộng kill 2 lần.
        LastAttacker =
            PlayerRef.None;


        // Không có killer.
        if (killerRef == PlayerRef.None)
        {
            Debug.LogWarning(
                "[SOLO KILL] LastAttacker = NONE"
            );

            return;
        }


        // Không tính tự sát.
        if (killerRef == Object.InputAuthority)
        {
            Debug.LogWarning(
                "[SOLO KILL] Player tự giết chính mình."
            );

            return;
        }

        // TÌM PLAYER GÂY KILL


        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        PlayerHealth killerHealth =
            null;


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;


            if (player.Object == null)
                continue;


            if (!player.Object.IsValid)
                continue;


            if (player.Object.InputAuthority ==
                killerRef)
            {
                killerHealth =
                    player;

                break;
            }
        }


        // =====================================================
        // KHÔNG TÌM THẤY KILLER
        // =====================================================

        if (killerHealth == null)
        {
            Debug.LogError(
                "[SOLO KILL] KHÔNG TÌM THẤY KILLER | PlayerRef = " +
                killerRef
            );

            return;
        }


        // =====================================================
        // CỘNG KILL
        // =====================================================

        killerHealth.Kills++;


        Debug.Log(
            "[SOLO KILL] THÀNH CÔNG | Killer = " +
            killerRef +
            " | Kills = " +
            killerHealth.Kills
        );


        // =====================================================
        // BÁO MATCH MANAGER
        // =====================================================

        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.ReportKill(
                killerRef,
                killerHealth.Kills
            );
        }
        else
        {
            Debug.LogWarning(
                "[SOLO KILL] MatchManager.Instance = NULL"
            );
        }
    }


    // =========================================================
    // RESPAWN
    // =========================================================

    private void RespawnPlayer()
    {
        if (!HasStateAuthority)
            return;


        // Không hồi sinh nếu trận đã kết thúc.
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }


        // Tìm vị trí mới TRƯỚC khi đặt IsDead = false
        if (!TryGetRandomRespawnPosition(out Vector3 randomSpawnPosition))
        {
            Debug.LogWarning(
                "[PlayerHealth] Không tìm được vị trí respawn an toàn. Sẽ thử lại ở network tick tiếp theo."
            );
            return;
        }

        Debug.Log(
            "[PlayerHealth] RESPAWN TARGET | Death: " + lastDeathPosition +
            " | New: " + randomSpawnPosition
        );

        CharacterController characterController = null;
        TryGetComponent(out characterController);

        if (characterController != null)
        {
            characterController.enabled = false;
        }

        if (TryGetComponent(out PlayerMovement movement))
        {
            movement.ResetMovementState();
        }

        // Teleport bằng NetworkTransform để Fusion đồng bộ vị trí
        if (TryGetComponent(out NetworkTransform netTransform))
        {
            netTransform.Teleport(randomSpawnPosition);
        }
        else
        {
            Debug.LogWarning(
                "[PlayerHealth] Player không có NetworkTransform. Dùng transform.position."
            );
        }

        // Đặt trực tiếp trên State Authority trong tick hiện tại
        transform.position = randomSpawnPosition;

        // Reset inventory
        if (TryGetComponent(out PlayerWeapon playerWeapon))
        {
            playerWeapon.ResetAllInventoryOnRespawn();
        }

        SmallMedkitCount = 0;
        MediumMedkitCount = 0;
        LargeMedkitCount = 0;
        UsingMedkitType = 0;
        MedkitTimer = TickTimer.None;

        // Bắt đầu bảo vệ chỉ sau khi tìm được vị trí và teleport thành công.
        InvulnerabilityTimer = respawnInvulnerabilityDuration > 0f
            ? TickTimer.CreateFromSeconds(Runner, respawnInvulnerabilityDuration)
            : TickTimer.None;

        // Chỉ cho sống lại SAU KHI đã teleport xong
        CurrentHealth = maxHealth;
        IsDead = false;
        LastAttacker = PlayerRef.None;
        RespawnTimer = TickTimer.None;

        if (characterController != null)
        {
            characterController.enabled = true;
        }

        if (movement != null)
        {
            movement.ResetMovementState();
            movement.enabled = true;
        }

        Rpc_PlayRespawn();

        Debug.Log(
            "[PlayerHealth] RESPAWN SUCCESS | Player: " + Object.InputAuthority +
            " | Position: " + transform.position
        );
    }


    // =========================================================
    // RANDOM RESPAWN POSITION
    // =========================================================

    private bool TryGetRandomRespawnPosition(
        out Vector3 spawnPosition)
    {
        spawnPosition = transform.position;

        FindRespawnTerrain();

        if (respawnTerrain == null ||
            respawnTerrain.terrainData == null)
        {
            Debug.LogError(
                "[PlayerHealth] Không tìm thấy Terrain để random respawn!"
            );

            return false;
        }


        TerrainData terrainData =
            respawnTerrain.terrainData;

        Vector3 terrainPosition =
            respawnTerrain.transform.position;

        Vector3 terrainSize =
            terrainData.size;


        // =====================================================
        // KHU VỰC RESPAWN GẦN GIỮA MAP
        // =====================================================

        float percent =
            Mathf.Clamp(
                centerRespawnPercent,
                0.1f,
                1f
            );


        float centerX =
            terrainPosition.x +
            terrainSize.x * 0.5f;

        float centerZ =
            terrainPosition.z +
            terrainSize.z * 0.5f;


        float halfSpawnWidth =
            terrainSize.x *
            percent *
            0.5f;

        float halfSpawnLength =
            terrainSize.z *
            percent *
            0.5f;


        float minX =
            centerX -
            halfSpawnWidth;

        float maxX =
            centerX +
            halfSpawnWidth;

        float minZ =
            centerZ -
            halfSpawnLength;

        float maxZ =
            centerZ +
            halfSpawnLength;


        Vector2 deathXZ =
            new Vector2(
                lastDeathPosition.x,
                lastDeathPosition.z
            );


        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        int tryCount =
            Mathf.Max(
                10,
                respawnTryCount
            );


        // =====================================================
        // THAY VÌ BẮT BUỘC "PHẢI >= 30M",
        // TA TÌM ĐIỂM TỐT NHẤT / XA NHẤT.
        //
        // Điều này giúp:
        // - vẫn ưu tiên xa Player khác
        // - không bị kẹt nếu vùng giữa map nhỏ
        // - luôn có cơ hội hồi sinh
        // =====================================================

        bool foundCandidate =
            false;

        Vector3 bestCandidate =
            transform.position;

        float bestScore =
            float.MinValue;

        float bestPlayerDistance =
            0f;

        float bestDeathDistance =
            0f;


        for (int i = 0;
             i < tryCount;
             i++)
        {
            float randomX =
                Random.Range(
                    minX,
                    maxX
                );

            float randomZ =
                Random.Range(
                    minZ,
                    maxZ
                );


            Vector3 candidate =
                new Vector3(
                    randomX,
                    0f,
                    randomZ
                );


            // =================================================
            // KIỂM TRA ĐỘ DỐC
            // =================================================

            float normalizedX =
                Mathf.InverseLerp(
                    terrainPosition.x,
                    terrainPosition.x +
                    terrainSize.x,
                    randomX
                );

            float normalizedZ =
                Mathf.InverseLerp(
                    terrainPosition.z,
                    terrainPosition.z +
                    terrainSize.z,
                    randomZ
                );


            Vector3 terrainNormal =
                terrainData.GetInterpolatedNormal(
                    normalizedX,
                    normalizedZ
                );


            float slope =
                Vector3.Angle(
                    terrainNormal,
                    Vector3.up
                );


            if (slope > maxRespawnSlope)
            {
                continue;
            }


            // =================================================
            // LẤY ĐỘ CAO TERRAIN
            // =================================================

            candidate.y =
                respawnTerrain.SampleHeight(
                    candidate
                ) +
                terrainPosition.y +
                respawnHeightOffset;


            Vector2 candidateXZ =
                new Vector2(
                    candidate.x,
                    candidate.z
                );


            float distanceFromDeath =
                Vector2.Distance(
                    deathXZ,
                    candidateXZ
                );


            // Không ưu tiên spawn sát nơi vừa chết.
            if (distanceFromDeath <
                minDistanceFromDeathPosition)
            {
                continue;
            }


            // =================================================
            // TÍNH KHOẢNG CÁCH TỚI PLAYER GẦN NHẤT
            // =================================================

            float nearestPlayerDistance =
                GetNearestLivingPlayerDistance(
                    candidate,
                    players
                );


            // =================================================
            // ĐIỂM ƯU TIÊN
            //
            // Ưu tiên mạnh việc cách xa Player khác,
            // sau đó mới xét khoảng cách với nơi vừa chết.
            // =================================================

            float score =
                nearestPlayerDistance * 2f +
                distanceFromDeath * 0.25f;


            // Nếu đạt khoảng cách mong muốn thì cộng bonus lớn.
            if (nearestPlayerDistance >=
                minRespawnDistance)
            {
                score += 10000f;
            }


            if (!foundCandidate ||
                score > bestScore)
            {
                foundCandidate =
                    true;

                bestScore =
                    score;

                bestCandidate =
                    candidate;

                bestPlayerDistance =
                    nearestPlayerDistance;

                bestDeathDistance =
                    distanceFromDeath;
            }
        }


        // =====================================================
        // CÓ ĐIỂM HỢP LỆ
        // =====================================================

        if (foundCandidate)
        {
            spawnPosition =
                bestCandidate;


            Debug.Log(
                "[PlayerHealth] BEST CENTER RESPAWN" +
                " | Distance To Player = " +
                bestPlayerDistance.ToString("F1") +
                " | Desired Min = " +
                minRespawnDistance.ToString("F1") +
                " | Distance From Death = " +
                bestDeathDistance.ToString("F1") +
                " | Position = " +
                bestCandidate
            );


            return true;
        }


        // =====================================================
        // FALLBACK CUỐI
        //
        // Nếu không có điểm nào đạt điều kiện slope/death distance
        // thì lấy một điểm phẳng gần giữa map.
        // KHÔNG kiểm tra cứng Min Respawn Distance ở đây,
        // để tránh Player bị kẹt không hồi sinh.
        // =====================================================

        for (int i = 0;
             i < 30;
             i++)
        {
            float randomX =
                Random.Range(
                    minX,
                    maxX
                );

            float randomZ =
                Random.Range(
                    minZ,
                    maxZ
                );


            float normalizedX =
                Mathf.InverseLerp(
                    terrainPosition.x,
                    terrainPosition.x +
                    terrainSize.x,
                    randomX
                );

            float normalizedZ =
                Mathf.InverseLerp(
                    terrainPosition.z,
                    terrainPosition.z +
                    terrainSize.z,
                    randomZ
                );


            Vector3 terrainNormal =
                terrainData.GetInterpolatedNormal(
                    normalizedX,
                    normalizedZ
                );


            float slope =
                Vector3.Angle(
                    terrainNormal,
                    Vector3.up
                );


            if (slope > maxRespawnSlope)
            {
                continue;
            }


            Vector3 fallback =
                new Vector3(
                    randomX,
                    0f,
                    randomZ
                );


            fallback.y =
                respawnTerrain.SampleHeight(
                    fallback
                ) +
                terrainPosition.y +
                respawnHeightOffset;


            spawnPosition =
                fallback;


            Debug.LogWarning(
                "[PlayerHealth] RESPAWN FALLBACK" +
                " | Không tìm được điểm đạt khoảng cách mong muốn." +
                " Dùng điểm phẳng gần giữa map để tránh bị kẹt." +
                " | Position = " +
                fallback
            );


            return true;
        }


        Debug.LogError(
            "[PlayerHealth] Không tìm được bất kỳ vị trí hồi sinh hợp lệ nào."
        );


        return false;
    }


    // =========================================================
    // GET NEAREST LIVING PLAYER DISTANCE
    // =========================================================

    private float GetNearestLivingPlayerDistance(
        Vector3 position,
        PlayerHealth[] players)
    {
        if (players == null ||
            players.Length == 0)
        {
            return 99999f;
        }


        Vector2 spawnXZ =
            new Vector2(
                position.x,
                position.z
            );


        float nearestDistance =
            99999f;

        bool foundOtherPlayer =
            false;


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            if (player == this)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            if (player.IsDead)
                continue;


            foundOtherPlayer =
                true;


            Vector2 playerXZ =
                new Vector2(
                    player.transform.position.x,
                    player.transform.position.z
                );


            float distance =
                Vector2.Distance(
                    spawnXZ,
                    playerXZ
                );


            if (distance <
                nearestDistance)
            {
                nearestDistance =
                    distance;
            }
        }


        if (!foundOtherPlayer)
        {
            return 99999f;
        }


        return nearestDistance;
    }


    // =========================================================
    // CHECK RESPAWN POSITION
    // =========================================================

    private bool IsRespawnPositionSafe(
        Vector3 position,
        PlayerHealth[] players)
    {
        if (players == null)
        {
            return true;
        }


        Vector2 spawnPosition =
            new Vector2(
                position.x,
                position.z
            );


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            if (player == this)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;

            if (player.IsDead)
                continue;


            Vector2 playerPosition =
                new Vector2(
                    player.transform.position.x,
                    player.transform.position.z
                );


            if (Vector2.Distance(
                    playerPosition,
                    spawnPosition)
                < minRespawnDistance)
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // DEATH RPC
    // =========================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayDeath()
    {
        // =====================================================
        // DEATH ANIMATION
        // =====================================================

        if (TryGetComponent(
            out PlayerAnimation playerAnim))
        {
            playerAnim.Die();
        }

        // =====================================================
        // CHỈ LOCAL PLAYER BỊ CHẾT
        // MỚI CHẠY DEATH CAMERA
        // =====================================================

        if (!HasInputAuthority)
            return;


        // Ẩn Respawn UI trước
        if (respawnPanel != null)
        {
            respawnPanel.SetActive(false);
        }


        // Dừng cinematic cũ nếu còn
        if (deathCameraCoroutine != null)
        {
            StopCoroutine(
                deathCameraCoroutine
            );

            deathCameraCoroutine = null;
        }


        deathCameraCoroutine =
            StartCoroutine(
                PlayDeathCameraEffect()
            );
    }

    // =========================================================
    // DEATH CAMERA EFFECT
    // =========================================================

    private IEnumerator PlayDeathCameraEffect()
    {
        // =====================================================
        // CHECK
        // =====================================================

        if (!HasInputAuthority)
        {
            yield break;
        }
        // =====================================================
        // LOCK PLAYER + TPS CAMERA
        // =====================================================

        SetLocalRespawnLock(
            true
        );


        // =====================================================
        // GET CAMERA
        // =====================================================

        if (deathCamera == null)
        {
            deathCamera =
                Camera.main;
        }


        if (deathCamera == null)
        {
            Debug.LogWarning(
                "[PlayerHealth] Không tìm thấy Camera.main cho Death Camera."
            );


            ShowRespawnPanel();
            yield break;
        }


        // =====================================================
        // START CAMERA
        // =====================================================

        Vector3 startPosition =
            deathCamera
                .transform
                .position;


        Quaternion startRotation =
            deathCamera
                .transform
                .rotation;


        // =====================================================
        // PLAYER DEATH POSITION
        // =====================================================

        Vector3 deathPosition =
            transform.position;


        // =====================================================
        // END POSITION
        // =====================================================

        Vector3 endPosition =
            deathPosition +
            Vector3.up *
            deathCameraHeight +
            deathCameraOffset;


        Vector3 lookPoint =
            deathPosition +
            Vector3.up *
            deathCameraLookHeight;


        Vector3 lookDirection =
            lookPoint -
            endPosition;


        Quaternion endRotation =
            startRotation;


        if (lookDirection.sqrMagnitude >
            0.001f)
        {
            endRotation =
                Quaternion.LookRotation(
                    lookDirection.normalized,
                    Vector3.up
                );
        }


        // =====================================================
        // CAMERA RISE
        // =====================================================

        float duration =
            Mathf.Max(
                0.01f,
                deathCameraRiseDuration
            );


        float timer =
            0f;


        while (timer <
               duration)
        {
            // Nếu trận kết thúc trong lúc camera đang chạy
            // thì dừng cinematic hồi sinh.
            if (MatchManager.Instance != null &&
                MatchManager.Instance.MatchEnded)
            {
                deathCameraCoroutine =
                    null;


                yield break;
            }


            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            // SmoothStep
            float smoothT =
                t *
                t *
                (3f - 2f * t);


            // =============================================
            // POSITION
            // =============================================

            deathCamera
                .transform
                .position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    smoothT
                );


            // =============================================
            // ROTATION
            // =============================================

            deathCamera
                .transform
                .rotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    smoothT
                );


            yield return null;
        }


        // =====================================================
        // FINAL POSITION
        // =====================================================

        deathCamera
            .transform
            .position =
            endPosition;


        deathCamera
            .transform
            .rotation =
            endRotation;


        // =====================================================
        // HOLD TOP VIEW
        // =====================================================

        if (deathCameraHoldDuration >
            0f)
        {
            yield return
                new WaitForSecondsRealtime(
                    deathCameraHoldDuration
                );
        }


        // =====================================================
        // SHOW RESPAWN UI
        // =====================================================

        if (MatchManager.Instance == null ||
            !MatchManager.Instance.MatchEnded)
        {
            ShowRespawnPanel();
        }
        deathCameraCoroutine =
            null;
    }

    // =========================================================
    // SHOW RESPAWN UI
    // =========================================================

    private void ShowRespawnPanel()
    {
        if (!HasInputAuthority)
            return;


        if (respawnPanel != null)
        {
            respawnPanel.SetActive(
                true
            );
        }
    }


    // =========================================================
    // RESPAWN RPC
    // =========================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayRespawn()
    {
        // FIX:
        // GetComponentInChildren(true) thiếu generic type
        Animator animator =
            GetComponentInChildren<Animator>(true);


        if (animator != null)
        {
            animator.Rebind();

            animator.Update(0f);

            animator.speed = 1f;
        }


        if (TryGetComponent(
            out PlayerAnimation playerAnim))
        {
            playerAnim.Move(0f);

            playerAnim.SetMelee(false);
        }


        if (HasInputAuthority)
        {
            // =====================================================
            // STOP DEATH CAMERA
            // =====================================================

            if (deathCameraCoroutine != null)
            {
                StopCoroutine(
                    deathCameraCoroutine
                );

                deathCameraCoroutine =
                    null;
            }
            // =====================================================
            // HIDE RESPAWN UI
            // =====================================================

            if (respawnPanel != null)
            {
                respawnPanel.SetActive(
                    false
                );
            }


            // =====================================================
            // UNLOCK PLAYER + TPS CAMERA
            // =====================================================

            SetLocalRespawnLock(
                false
            );
        }
    }
}