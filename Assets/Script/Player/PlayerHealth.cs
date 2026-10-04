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
    public float respawnEdgePadding = 5f;
    public float minRespawnDistance = 5f;
    public int respawnTryCount = 50;
    public float respawnHeightOffset = 1f;

    [Networked] public TickTimer RespawnTimer { get; set; }


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


        IsDead = true;

        CurrentHealth = 0f;


        RespawnTimer =
            TickTimer.CreateFromSeconds(
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


        CurrentHealth = maxHealth;

        IsDead = false;


        RespawnTimer = TickTimer.None;
        MedkitTimer = TickTimer.None;


        SmallMedkitCount = 0;
        MediumMedkitCount = 0;
        LargeMedkitCount = 0;

        UsingMedkitType = 0;


        if (TryGetComponent(out PlayerWeapon playerWeapon))
        {
            playerWeapon.ResetAllInventoryOnRespawn();
        }


        CharacterController characterController = null;

        TryGetComponent(out characterController);


        // Tắt CharacterController trước khi teleport
        if (characterController != null)
        {
            characterController.enabled = false;
        }


        if (TryGetRandomRespawnPosition(
            out Vector3 randomSpawnPosition))
        {
            // Đặt vị trí trực tiếp trên StateAuthority
            transform.position = randomSpawnPosition;


            /*
             * FIX QUAN TRỌNG:
             *
             * NetworkCharacterControllerPrototype
             * không tồn tại trong Fusion version hiện tại.
             *
             * Player hiện tại nếu có NetworkTransform
             * thì dùng NetworkTransform.Teleport().
             */

            if (TryGetComponent(
                out NetworkTransform netTransform))
            {
                netTransform.Teleport(
                    randomSpawnPosition
                );
            }
        }


        // Bật lại CharacterController
        if (characterController != null)
        {
            characterController.enabled = true;
        }


        // Reset movement
        if (TryGetComponent(out PlayerMovement movement))
        {
            movement.ResetMovementState();

            movement.enabled = true;
        }


        Rpc_PlayRespawn();
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
            return false;
        }


        Vector3 terrainPosition =
            respawnTerrain.transform.position;

        Vector3 terrainSize =
            respawnTerrain.terrainData.size;


        float padX = Mathf.Min(
            respawnEdgePadding,
            terrainSize.x / 3f
        );

        float padZ = Mathf.Min(
            respawnEdgePadding,
            terrainSize.z / 3f
        );


        float minX =
            terrainPosition.x + padX;

        float maxX =
            terrainPosition.x
            + terrainSize.x
            - padX;


        float minZ =
            terrainPosition.z + padZ;

        float maxZ =
            terrainPosition.z
            + terrainSize.z
            - padZ;


        for (int i = 0;
             i < respawnTryCount;
             i++)
        {
            Vector3 candidate =
                new Vector3(
                    Random.Range(minX, maxX),
                    0f,
                    Random.Range(minZ, maxZ)
                );


            candidate.y =
                respawnTerrain.SampleHeight(candidate)
                + terrainPosition.y
                + respawnHeightOffset;


            if (IsRespawnPositionSafe(candidate))
            {
                spawnPosition = candidate;

                return true;
            }
        }


        return false;
    }


    // =========================================================
    // CHECK RESPAWN POSITION
    // =========================================================

    private bool IsRespawnPositionSafe(
        Vector3 position)
    {
        // FIX:
        // Unity mới không cần FindObjectsSortMode.None
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        foreach (PlayerHealth player in players)
        {
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


            Vector2 spawnPosition =
                new Vector2(
                    position.x,
                    position.z
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