using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;

    [Networked] public float CurrentHealth { get; set; }
    [Networked] public NetworkBool IsDead { get; set; }


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
    public float minRespawnDistance = 10f;

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

    // Vị trí Player vừa chết để tránh hồi sinh lại gần chỗ cũ
    private Vector3 lastDeathPosition;


    [Header("Respawn Lock")]
    [Tooltip("Kéo script điều khiển camera của Player vào đây. Có thể để trống nếu script tên ThirdPersonCamera.")]
    public MonoBehaviour playerCameraController;

    private Terrain respawnTerrain;

    private bool localRespawnLocked = true;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!HasInputAuthority)
            return;

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
        MonoBehaviour[] behaviours =
            GetComponentsInChildren<MonoBehaviour>(true);

        foreach (MonoBehaviour behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this)
                continue;

            string typeName = behaviour.GetType().Name;

            if (typeName == "ThirdPersonCamera" ||
                typeName == "PlayerCameraController" ||
                typeName == "CameraController")
            {
                playerCameraController = behaviour;
                return;
            }
        }
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

            SmallMedkitCount = 0;
            MediumMedkitCount = 0;
            LargeMedkitCount = 0;

            UsingMedkitType = 0;

            MedkitTimer = TickTimer.None;
            RespawnTimer = TickTimer.None;
        }


        if (HasInputAuthority)
        {
            localRespawnLocked = true;

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
            transform.position
        );
    }


    public void TakeDamage(
        float damage,
        Vector3 attackerPosition)
    {
        if (!HasStateAuthority)
            return;

        if (IsDead)
            return;


        damage = Mathf.Max(damage, 0f);


        CurrentHealth = Mathf.Clamp(
            CurrentHealth - damage,
            0f,
            maxHealth
        );


        if (UsingMedkitType != 0)
        {
            CancelMedkit();
        }


        if (CurrentHealth > 0f)
        {
            Rpc_PlayHit(attackerPosition);
        }
        else
        {
            Die();
        }
    }


    // =========================================================
    // HIT RPC
    // =========================================================

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayHit(
        Vector3 attackerPosition)
    {
        if (TryGetComponent(out PlayerAnimation playerAnim))
        {
            playerAnim.Hit();
        }


        if (HasInputAuthority)
        {
            // FIX:
            // phải chỉ rõ DamageDirectionUI
            DamageDirectionUI damageUI =
                FindAnyObjectByType<DamageDirectionUI>();


            if (damageUI != null)
            {
                damageUI.ShowDamage(attackerPosition);
            }
        }
    }


    // =========================================================
    // DIE
    // =========================================================

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
        CurrentHealth = 0f;

        RespawnTimer = TickTimer.CreateFromSeconds(
            Runner,
            respawnDelay
        );

        if (HasInputAuthority)
        {
            SetLocalRespawnLock(true);
        }

        Rpc_PlayDeath();
    }


    // =========================================================
    // RESPAWN
    // =========================================================

    private void RespawnPlayer()
    {
        if (!HasStateAuthority)
            return;

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

        // Chỉ cho sống lại SAU KHI đã teleport xong
        CurrentHealth = maxHealth;
        IsDead = false;
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
        // CHỈ RESPAWN Ở KHU VỰC GẦN GIỮA MAP
        //
        // Ví dụ:
        // centerRespawnPercent = 0.5
        // => chỉ dùng 50% diện tích giữa Terrain.
        // => bỏ 25% mỗi bên của map.
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


        // Chỉ tìm Player khác 1 lần.
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        int tryCount =
            Mathf.Max(
                1,
                respawnTryCount
            );


        // =====================================================
        // THỬ TÌM VỊ TRÍ AN TOÀN
        // =====================================================

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
            // KIỂM TRA ĐỘ DỐC TERRAIN
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


            // Quá dốc -> bỏ.
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


            // =================================================
            // KHÔNG QUÁ GẦN CHỖ VỪA CHẾT
            // =================================================

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


            if (distanceFromDeath <
                minDistanceFromDeathPosition)
            {
                continue;
            }


            // =================================================
            // KHÔNG QUÁ GẦN PLAYER KHÁC
            // =================================================

            if (!IsRespawnPositionSafe(
                candidate,
                players))
            {
                continue;
            }


            spawnPosition =
                candidate;


            Debug.Log(
                "[PlayerHealth] CENTER RESPAWN OK" +
                " | Try = " + (i + 1) +
                " | Slope = " +
                slope.ToString("F1") +
                " | Distance From Death = " +
                distanceFromDeath.ToString("F1") +
                " | Position = " +
                candidate
            );


            return true;
        }


        // =====================================================
        // FALLBACK
        //
        // Vẫn chỉ lấy ở vùng giữa map.
        // Dùng để tránh bị kẹt ở giây 1.
        // =====================================================

        for (int i = 0; i < 20; i++)
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


            // Fallback vẫn không cho spawn trên sườn núi quá dốc.
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
                "[PlayerHealth] Dùng CENTER FALLBACK RESPawn" +
                " | Position = " +
                fallback +
                " | Slope = " +
                slope.ToString("F1")
            );


            return true;
        }


        Debug.LogError(
            "[PlayerHealth] Không tìm được vị trí hồi sinh phẳng ở vùng giữa map."
        );


        return false;
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
        if (TryGetComponent(
            out PlayerAnimation playerAnim))
        {
            playerAnim.Die();
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
            SetLocalRespawnLock(false);
        }
    }
}