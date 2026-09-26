using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    // =========================================================
    // HEALTH
    // =========================================================

    [Header("Health")]

    public float maxHealth = 100f;


    // =========================================================
    // NETWORK HEALTH
    // =========================================================

    [Header("Network Health")]

    [Networked]
    public float CurrentHealth { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }


    // =========================================================
    // MEDKIT INVENTORY
    // =========================================================

    [Header("Medkit Inventory")]

    [Networked]
    public int SmallMedkitCount { get; set; }

    [Networked]
    public int MediumMedkitCount { get; set; }

    [Networked]
    public int LargeMedkitCount { get; set; }


    // =========================================================
    // MEDKIT HEAL
    // =========================================================

    [Header("Medkit Heal Amount")]

    public float smallHealAmount = 50f;
    public float mediumHealAmount = 75f;
    public float largeHealAmount = 100f;


    // =========================================================
    // MEDKIT USE TIME
    // =========================================================

    [Header("Medkit Use Time")]

    public float smallUseTime = 2f;
    public float mediumUseTime = 3f;
    public float largeUseTime = 4f;


    // =========================================================
    // MEDKIT INPUT
    // =========================================================

    [Header("Medkit Input")]

    public KeyCode useMedkitKey = KeyCode.E;


    // =========================================================
    // MEDKIT USING
    // =========================================================

    [Header("Medkit Using")]

    // 0 = Không dùng
    // 1 = Small
    // 2 = Medium
    // 3 = Large

    [Networked]
    public int UsingMedkitType { get; set; }


    // =========================================================
    // MEDKIT TIMER
    // =========================================================

    [Networked]
    public TickTimer MedkitTimer { get; set; }


    // =========================================================
    // RANDOM RESPAWN
    // =========================================================

    [Header("Random Respawn")]

    [Tooltip("Thời gian chờ trước khi hồi sinh")]
    public float respawnDelay = 5f;

    [Tooltip("Khoảng cách tối thiểu cách mép Terrain")]
    public float respawnEdgePadding = 5f;

    [Tooltip("Khoảng cách tối thiểu với Player khác")]
    public float minRespawnDistance = 5f;

    [Tooltip("Số lần thử tìm vị trí")]
    public int respawnTryCount = 50;

    [Tooltip("Độ cao cộng thêm khi đặt Player")]
    public float respawnHeightOffset = 1f;

    [Networked]
    public TickTimer RespawnTimer { get; set; }


    // =========================================================
    // RESPAWN LOCK
    // =========================================================

    [Header("Respawn Lock")]

    [Tooltip("Camera script của Player")]
    public MonoBehaviour playerCameraController;


    // =========================================================
    // TERRAIN
    // =========================================================

    private Terrain respawnTerrain;


    // =========================================================
    // PRIVATE
    // =========================================================

    private bool localRespawnLocked = true;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!HasInputAuthority)
            return;


        // =====================================================
        // DEAD
        // =====================================================

        if (IsDead)
        {
            SetLocalRespawnLock(true);
            return;
        }


        // =====================================================
        // ALIVE
        // =====================================================

        SetLocalRespawnLock(false);


        // =====================================================
        // ĐANG DÙNG MEDKIT
        // =====================================================

        if (UsingMedkitType != 0)
            return;


        // =====================================================
        // NHẤN E DÙNG MEDKIT
        // =====================================================

        if (Input.GetKeyDown(useMedkitKey))
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


        // =====================================================
        // DEAD / RESPAWN
        // =====================================================

        if (IsDead)
        {
            if (
                RespawnTimer.Expired(
                    Runner
                )
            )
            {
                RespawnPlayer();
            }


            return;
        }


        // =====================================================
        // MEDKIT
        // =====================================================

        if (UsingMedkitType == 0)
            return;


        if (
            MedkitTimer.Expired(
                Runner
            )
        )
        {
            FinishUseMedkit();
        }
    }


    // =========================================================
    // LOCAL RESPAWN LOCK
    // =========================================================

    private void SetLocalRespawnLock(
        bool locked)
    {
        if (!HasInputAuthority)
            return;


        // =====================================================
        // TÌM CAMERA
        // =====================================================

        if (playerCameraController == null)
        {
            ThirdPersonCamera cameraController =
                GetComponentInChildren<ThirdPersonCamera>(
                    true
                );


            if (cameraController != null)
            {
                playerCameraController =
                    cameraController;
            }
        }


        // =====================================================
        // KHÔNG LẶP
        // =====================================================

        if (
            localRespawnLocked ==
            locked
        )
        {
            return;
        }


        localRespawnLocked =
            locked;


        // =====================================================
        // MOVEMENT
        // =====================================================

        PlayerMovement movement =
            GetComponent<PlayerMovement>();


        if (movement != null)
        {
            if (!locked)
            {
                movement.ResetMovementState();
            }

            movement.enabled =
                !locked;
        }


        // =====================================================
        // WEAPON
        // =====================================================

        PlayerWeapon weapon =
            GetComponent<PlayerWeapon>();


        if (weapon != null)
        {
            weapon.enabled =
                !locked;


            if (!locked)
            {
                weapon.RefreshWeaponVisuals();
            }
        }


        // =====================================================
        // CHARACTER CONTROLLER
        // =====================================================

        CharacterController controller =
            GetComponent<CharacterController>();


        // Chỉ State Authority điều khiển CharacterController
        if (
            controller != null &&
            HasStateAuthority
        )
        {
            controller.enabled =
                !locked;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        if (
            playerCameraController != null
        )
        {
            playerCameraController.enabled =
                !locked;
        }


        // =====================================================
        // CURSOR
        // =====================================================

        // Giữ chuột LOCK kể cả khi chết,
        // tránh cho người chơi tương tác ngoài game.
        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;


        Debug.Log(
            "[PlayerHealth] " +
            "Respawn Lock = " +
            locked
        );
    }


    // =========================================================
    // GET BEST MEDKIT
    // =========================================================

    public int GetBestMedkitType()
    {
        if (CurrentHealth >= maxHealth)
            return 0;


        if (UsingMedkitType != 0)
            return 0;


        if (IsDead)
            return 0;


        float missingHealth =
            maxHealth -
            CurrentHealth;


        // =====================================================
        // SMALL
        // =====================================================

        if (
            SmallMedkitCount > 0 &&
            missingHealth <= smallHealAmount
        )
        {
            return 1;
        }


        // =====================================================
        // MEDIUM
        // =====================================================

        if (
            MediumMedkitCount > 0 &&
            missingHealth <= mediumHealAmount
        )
        {
            return 2;
        }


        // =====================================================
        // LARGE
        // =====================================================

        if (
            LargeMedkitCount > 0 &&
            missingHealth <= largeHealAmount
        )
        {
            return 3;
        }


        // =====================================================
        // FALLBACK
        // =====================================================

        if (LargeMedkitCount > 0)
            return 3;


        if (MediumMedkitCount > 0)
            return 2;


        if (SmallMedkitCount > 0)
            return 1;


        return 0;
    }


    // =========================================================
    // GET HEAL AMOUNT
    // =========================================================

    private float GetHealAmount(
        int medkitType)
    {
        switch (medkitType)
        {
            case 1:
                return smallHealAmount;

            case 2:
                return mediumHealAmount;

            case 3:
                return largeHealAmount;
        }


        return 0f;
    }


    // =========================================================
    // GET MEDKIT USE TIME
    // =========================================================

    public float GetMedkitUseTime(
        int medkitType)
    {
        switch (medkitType)
        {
            case 1:
                return smallUseTime;

            case 2:
                return mediumUseTime;

            case 3:
                return largeUseTime;
        }


        return 0f;
    }


    // =========================================================
    // REQUEST USE MEDKIT
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
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


        int medkitType =
            GetBestMedkitType();


        if (medkitType == 0)
        {
            Debug.Log(
                "PLAYER " +
                Object.InputAuthority +
                " KHÔNG CÓ MEDKIT!"
            );

            return;
        }


        if (!HasMedkit(medkitType))
            return;


        float useTime =
            GetMedkitUseTime(
                medkitType
            );


        UsingMedkitType =
            medkitType;


        MedkitTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                useTime
            );
    }


    // =========================================================
    // CHECK MEDKIT
    // =========================================================

    private bool HasMedkit(
        int medkitType)
    {
        switch (medkitType)
        {
            case 1:
                return SmallMedkitCount > 0;

            case 2:
                return MediumMedkitCount > 0;

            case 3:
                return LargeMedkitCount > 0;
        }


        return false;
    }


    // =========================================================
    // FINISH MEDKIT
    // =========================================================

    private void FinishUseMedkit()
    {
        if (UsingMedkitType == 0)
            return;


        if (IsDead)
        {
            CancelMedkit();
            return;
        }


        int medkitType =
            UsingMedkitType;


        if (!HasMedkit(medkitType))
        {
            CancelMedkit();
            return;
        }


        float healAmount =
            GetHealAmount(
                medkitType
            );


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


        CurrentHealth +=
            healAmount;


        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth,
                0f,
                maxHealth
            );


        Rpc_PlayHeal();


        UsingMedkitType =
            0;


        MedkitTimer =
            TickTimer.None;
    }


    // =========================================================
    // CANCEL MEDKIT
    // =========================================================

    private void CancelMedkit()
    {
        UsingMedkitType =
            0;


        MedkitTimer =
            TickTimer.None;
    }


    // =========================================================
    // HEAL RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayHeal()
    {
        Debug.Log(
            "Player " +
            Object.InputAuthority +
            " đang hồi máu"
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


    // =========================================================
    // TOTAL MEDKIT
    // =========================================================

    public int GetTotalMedkitCount()
    {
        return
            SmallMedkitCount +
            MediumMedkitCount +
            LargeMedkitCount;
    }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        FindRespawnTerrain();


        // =====================================================
        // INITIAL STATE
        // =====================================================

        if (HasStateAuthority)
        {
            CurrentHealth =
                maxHealth;


            IsDead =
                false;


            SmallMedkitCount =
                0;


            MediumMedkitCount =
                0;


            LargeMedkitCount =
                0;


            UsingMedkitType =
                0;


            MedkitTimer =
                TickTimer.None;


            RespawnTimer =
                TickTimer.None;
        }


        // =====================================================
        // LOCAL PLAYER
        // =====================================================

        if (HasInputAuthority)
        {
            localRespawnLocked =
                true;


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


        respawnTerrain =
            Terrain.activeTerrain;


        if (respawnTerrain == null)
        {
            respawnTerrain =
                FindFirstObjectByType<Terrain>();
        }


        if (respawnTerrain != null)
        {
            Debug.Log(
                "PLAYER HEALTH → TERRAIN: " +
                respawnTerrain.name
            );
        }
        else
        {
            Debug.LogError(
                "PLAYER HEALTH → KHÔNG TÌM THẤY TERRAIN!"
            );
        }
    }


    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(
        float damage)
    {
        if (!HasStateAuthority)
            return;


        if (IsDead)
            return;


        if (damage < 0f)
            damage = 0f;


        CurrentHealth -=
            damage;


        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth,
                0f,
                maxHealth
            );


        // =====================================================
        // CANCEL MEDKIT
        // =====================================================

        if (UsingMedkitType != 0)
        {
            CancelMedkit();
        }


        Debug.Log(
            "PLAYER " +
            Object.InputAuthority +
            " HP: " +
            CurrentHealth +
            " / " +
            maxHealth
        );


        // =====================================================
        // HIT
        // =====================================================

        if (CurrentHealth > 0f)
        {
            Rpc_PlayHit();
        }


        // =====================================================
        // DEAD
        // =====================================================

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }


    // =========================================================
    // HIT RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayHit()
    {
        PlayerAnimation playerAnim =
            GetComponent<PlayerAnimation>();


        if (playerAnim != null)
        {
            playerAnim.Hit();
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


        // =====================================================
        // CANCEL MEDKIT
        // =====================================================

        CancelMedkit();


        // =====================================================
        // CANCEL RELOAD
        // =====================================================

        PlayerWeapon playerWeapon =
            GetComponent<PlayerWeapon>();


        if (playerWeapon != null)
        {
            playerWeapon.CancelReloadOnDeath();
        }


        // =====================================================
        // DEAD
        // =====================================================

        IsDead =
            true;


        CurrentHealth =
            0f;


        // =====================================================
        // RESPAWN TIMER
        // =====================================================

        RespawnTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                respawnDelay
            );


        Debug.Log(
            "====================================\n" +
            "PLAYER DEAD\n" +
            "Player: " +
            Object.InputAuthority +
            "\nRespawn sau: " +
            respawnDelay +
            " giây\n" +
            "===================================="
        );


        // =====================================================
        // LOCAL LOCK
        // =====================================================

        if (HasInputAuthority)
        {
            SetLocalRespawnLock(true);
        }


        // =====================================================
        // DEATH ANIMATION
        // =====================================================

        Rpc_PlayDeath();
    }


    // =========================================================
    // RESPAWN PLAYER
    // =========================================================

    private void RespawnPlayer()
    {
        if (!HasStateAuthority)
            return;


        Debug.Log(
            "====================================\n" +
            "PLAYER RESPAWN\n" +
            "Player: " +
            Object.InputAuthority +
            "\n===================================="
        );


        // =====================================================
        // RESET HP
        // =====================================================

        CurrentHealth =
            maxHealth;


        IsDead =
            false;


        // =====================================================
        // RESET TIMERS
        // =====================================================

        RespawnTimer =
            TickTimer.None;


        UsingMedkitType =
            0;


        MedkitTimer =
            TickTimer.None;


        // =====================================================
        // RESET MEDKIT
        // =====================================================

        SmallMedkitCount =
            0;


        MediumMedkitCount =
            0;


        LargeMedkitCount =
            0;


        // =====================================================
        // RESET WEAPON
        // =====================================================

        PlayerWeapon playerWeapon =
            GetComponent<PlayerWeapon>();


        if (playerWeapon != null)
        {
            playerWeapon.ResetAllInventoryOnRespawn();
        }


        // =====================================================
        // RANDOM POSITION
        // =====================================================

        Vector3 randomSpawnPosition;


        bool foundSpawnPosition =
            TryGetRandomRespawnPosition(
                out randomSpawnPosition
            );


        // =====================================================
        // TELEPORT
        // =====================================================

        CharacterController characterController =
            GetComponent<CharacterController>();


        if (characterController != null)
        {
            characterController.enabled =
                false;
        }


        if (foundSpawnPosition)
        {
            transform.position =
                randomSpawnPosition;


            Debug.Log(
                "RESPAWN RANDOM TẠI: " +
                randomSpawnPosition
            );
        }
        else
        {
            Debug.LogWarning(
                "KHÔNG TÌM ĐƯỢC VỊ TRÍ RESPAWN!"
            );
        }


        if (characterController != null)
        {
            characterController.enabled =
                true;
        }


        // =====================================================
        // RESET MOVEMENT
        // =====================================================

        PlayerMovement movement =
            GetComponent<PlayerMovement>();


        if (movement != null)
        {
            movement.ResetMovementState();

            movement.enabled =
                true;
        }


        // =====================================================
        // RESPawn RPC
        // =====================================================

        Rpc_PlayRespawn();
    }


    // =========================================================
    // FIND RANDOM TERRAIN POSITION
    // =========================================================

    private bool TryGetRandomRespawnPosition(
        out Vector3 spawnPosition)
    {
        spawnPosition =
            transform.position;


        FindRespawnTerrain();


        if (respawnTerrain == null)
        {
            Debug.LogError(
                "KHÔNG TÌM THẤY TERRAIN!"
            );

            return false;
        }


        TerrainData terrainData =
            respawnTerrain.terrainData;


        if (terrainData == null)
        {
            Debug.LogError(
                "TERRAIN DATA KHÔNG HỢP LỆ!"
            );

            return false;
        }


        Vector3 terrainPosition =
            respawnTerrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        float minX =
            terrainPosition.x +
            respawnEdgePadding;


        float maxX =
            terrainPosition.x +
            terrainSize.x -
            respawnEdgePadding;


        float minZ =
            terrainPosition.z +
            respawnEdgePadding;


        float maxZ =
            terrainPosition.z +
            terrainSize.z -
            respawnEdgePadding;


        if (
            minX >= maxX ||
            minZ >= maxZ
        )
        {
            Debug.LogWarning(
                "RESPAWN EDGE PADDING QUÁ LỚN!"
            );

            return false;
        }


        // =====================================================
        // TRY RANDOM POSITION
        // =====================================================

        for (
            int i = 0;
            i < respawnTryCount;
            i++
        )
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


            float terrainHeight =
                respawnTerrain.SampleHeight(
                    candidate
                );


            candidate.y =
                terrainHeight +
                respawnHeightOffset;


            if (
                IsRespawnPositionSafe(
                    candidate
                )
            )
            {
                spawnPosition =
                    candidate;


                return true;
            }
        }


        return false;
    }


    // =========================================================
    // CHECK SAFE RESPAWN
    // =========================================================

    private bool IsRespawnPositionSafe(
        Vector3 position)
    {
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        foreach (
            PlayerHealth player
            in players
        )
        {
            // Bỏ qua chính mình
            if (player == this)
                continue;


            // NetworkObject không hợp lệ
            if (
                player.Object == null ||
                !player.Object.IsValid
            )
            {
                continue;
            }


            // Không tính Player đang chết
            if (player.IsDead)
                continue;


            Vector2 playerPos =
                new Vector2(
                    player.transform.position.x,
                    player.transform.position.z
                );


            Vector2 spawnPos =
                new Vector2(
                    position.x,
                    position.z
                );


            float distance =
                Vector2.Distance(
                    playerPos,
                    spawnPos
                );


            if (
                distance <
                minRespawnDistance
            )
            {
                return false;
            }
        }


        return true;
    }


    // =========================================================
    // DEATH RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayDeath()
    {
        PlayerAnimation playerAnim =
            GetComponent<PlayerAnimation>();


        if (playerAnim != null)
        {
            playerAnim.Die();
        }
    }


    // =========================================================
    // RESPAWN RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayRespawn()
    {
        // =====================================================
        // RESET ANIMATION
        // =====================================================

        Animator animator =
            GetComponentInChildren<Animator>(
                true
            );


        if (animator != null)
        {
            animator.Rebind();

            animator.Update(0f);

            animator.speed =
                1f;
        }


        PlayerAnimation playerAnim =
            GetComponent<PlayerAnimation>();


        if (playerAnim != null)
        {
            playerAnim.Move(0f);

            playerAnim.SetMelee(false);
        }


        // =====================================================
        // UNLOCK LOCAL PLAYER
        // =====================================================

        if (HasInputAuthority)
        {
            SetLocalRespawnLock(false);
        }
    }
}