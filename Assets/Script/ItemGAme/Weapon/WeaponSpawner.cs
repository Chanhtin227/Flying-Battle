using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class WeaponSpawner : NetworkBehaviour
{
    // =========================================================
    // CHEST
    // =========================================================

    [Header("Chest")]

    public NetworkPrefabRef chestPrefab;


    // =========================================================
    // TERRAIN
    // =========================================================

    [Header("Terrain")]

    public Terrain terrain;


    // =========================================================
    // CENTRAL MAP AREA
    // =========================================================

    [Header("Central Map Spawn Area")]

    [Tooltip(
        "Phần trăm diện tích trung tâm của map được phép spawn.\n" +
        "0.6 = chỉ spawn trong khoảng 60% ở giữa map."
    )]
    [Range(0.1f, 1f)]
    public float centerAreaPercent = 0.6f;


    [Tooltip(
        "Khoảng cách thêm từ mép vùng trung tâm."
    )]
    public float centerAreaPadding = 5f;


    // =========================================================
    // TERRAIN SLOPE
    // =========================================================

    [Header("Terrain Slope")]

    [Tooltip(
        "Độ dốc tối đa cho phép spawn rương.\n" +
        "25 = địa hình khá thoải."
    )]
    [Range(0f, 60f)]
    public float maxSlopeAngle = 25f;


    // =========================================================
    // SPAWN SETTINGS
    // =========================================================

    [Header("Spawn Settings")]

    [Tooltip(
        "Khoảng cách tối thiểu giữa các rương đang tồn tại."
    )]
    public float minSpawnDistance = 30f;


    [Tooltip(
        "Khoảng cách tối thiểu với mép Terrain."
    )]
    public float terrainEdgePadding = 5f;


    // =========================================================
    // SPAWN OFFSET
    // =========================================================

    [Header("Spawn Offset")]

    [Tooltip(
        "Độ cao cộng thêm so với mặt Terrain."
    )]
    public float spawnOffset = 0.5f;


    // =========================================================
    // CONTINUOUS SPAWN
    // =========================================================

    [Header("Continuous Spawn")]

    [Tooltip(
        "Bao nhiêu giây sẽ spawn một wave rương."
    )]
    public float spawnDelay = 5f;


    // =========================================================
    // CHEST COUNT
    // =========================================================

    [Header("Chest Count")]

    [Tooltip(
        "Số rương tối thiểu mỗi wave khi 1v1."
    )]
    public int minChestFor1v1 = 1;


    [Tooltip(
        "Số rương tối đa mỗi wave khi 1v1."
    )]
    public int maxChestFor1v1 = 2;


    [Tooltip(
        "Số rương tối thiểu mỗi wave khi 2v2."
    )]
    public int minChestFor2v2 = 2;


    [Tooltip(
        "Số rương tối đa mỗi wave khi 2v2."
    )]
    public int maxChestFor2v2 = 3;


    // =========================================================
    // ACTIVE CHESTS
    // =========================================================

    [Header("Runtime")]

    [SerializeField]
    private List<NetworkObject> activeChests =
        new List<NetworkObject>();


    // =========================================================
    // SPAWN CONTROL
    // =========================================================

    private Coroutine spawnRoutine;

    private bool isSpawning = false;


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();


        // Chỉ State Authority được phép spawn
        if (!HasStateAuthority)
        {
            return;
        }


        // =====================================================
        // FIND TERRAIN
        // =====================================================

        if (terrain == null)
        {
            terrain =
                Terrain.activeTerrain;
        }


        if (terrain == null)
        {
            terrain =
                FindAnyObjectByType<Terrain>();
        }


        if (terrain == null)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "KHÔNG TÌM THẤY TERRAIN!"
            );

            return;
        }


        // =====================================================
        // START SPAWNING
        // =====================================================

        isSpawning = true;


        spawnRoutine =
            StartCoroutine(
                SpawnChestRoutine()
            );


        Debug.Log(
            "[WeaponSpawner] " +
            "Spawner bắt đầu hoạt động."
        );
    }


    // =========================================================
    // SPAWN ROUTINE
    // =========================================================

    private IEnumerator SpawnChestRoutine()
    {
        // =====================================================
        // WAIT FIRST WAVE
        // =====================================================

        yield return new WaitForSeconds(
            Mathf.Max(
                0.1f,
                spawnDelay
            )
        );


        // =====================================================
        // CONTINUOUS SPAWN
        // =====================================================

        while (
            isSpawning &&
            Runner != null &&
            Runner.IsRunning
        )
        {
            // =================================================
            // CLEAN CHESTS
            // =================================================

            CleanupInactiveChests();


            // =================================================
            // SPAWN WAVE
            // =================================================

            SpawnChestWave();


            // =================================================
            // WAIT NEXT WAVE
            // =================================================

            yield return new WaitForSeconds(
                Mathf.Max(
                    0.1f,
                    spawnDelay
                )
            );
        }


        spawnRoutine = null;


        Debug.Log(
            "[WeaponSpawner] " +
            "Đã dừng spawn rương."
        );
    }


    // =========================================================
    // STOP SPAWNING
    // =========================================================

    public void StopSpawning()
    {
        // Chỉ State Authority điều khiển
        if (!HasStateAuthority)
        {
            return;
        }


        isSpawning = false;


        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );

            spawnRoutine = null;
        }


        Debug.Log(
            "[WeaponSpawner] " +
            "Đã STOP spawn rương."
        );
    }


    // =========================================================
    // DESPAWN ALL CHESTS
    // =========================================================

    public void DespawnAllChests()
    {
        // Chỉ State Authority được despawn
        if (!HasStateAuthority)
        {
            return;
        }


        // =====================================================
        // STOP SPAWNING
        // =====================================================

        StopSpawning();


        // =====================================================
        // CLEAN LIST
        // =====================================================

        CleanupInactiveChests();


        // =====================================================
        // DESPAWN ALL
        // =====================================================

        for (
            int i = activeChests.Count - 1;
            i >= 0;
            i--
        )
        {
            NetworkObject chest =
                activeChests[i];


            if (chest != null)
            {
                Runner.Despawn(
                    chest
                );
            }
        }


        // =====================================================
        // CLEAR
        // =====================================================

        activeChests.Clear();


        Debug.Log(
            "[WeaponSpawner] " +
            "Đã xóa toàn bộ chest."
        );
    }


    // =========================================================
    // DESPAWNED
    // =========================================================

    public override void Despawned(
        NetworkRunner runner,
        bool hasState)
    {
        isSpawning = false;


        if (spawnRoutine != null)
        {
            StopCoroutine(
                spawnRoutine
            );

            spawnRoutine = null;
        }


        activeChests.Clear();


        base.Despawned(
            runner,
            hasState
        );
    }


    // =========================================================
    // CLEANUP INACTIVE CHESTS
    // =========================================================

    private void CleanupInactiveChests()
    {
        for (
            int i = activeChests.Count - 1;
            i >= 0;
            i--
        )
        {
            NetworkObject chest =
                activeChests[i];


            // =================================================
            // OBJECT ĐÃ BỊ DESTROY / DESPAWN
            // =================================================

            if (chest == null)
            {
                activeChests.RemoveAt(i);
            }
        }
    }


    // =========================================================
    // SPAWN CHEST WAVE
    // =========================================================

    private void SpawnChestWave()
    {
        // =====================================================
        // PLAYER COUNT
        // =====================================================

        int playerCount =
            GetPlayerCount();


        // =====================================================
        // DEFAULT
        // =====================================================

        int minChest = 1;
        int maxChest = 2;


        // =====================================================
        // 1V1
        // =====================================================

        if (playerCount <= 2)
        {
            minChest =
                minChestFor1v1;

            maxChest =
                maxChestFor1v1;
        }


        // =====================================================
        // 2V2
        // =====================================================

        else if (playerCount <= 4)
        {
            minChest =
                minChestFor2v2;

            maxChest =
                maxChestFor2v2;
        }


        // =====================================================
        // MORE PLAYERS
        // =====================================================

        else
        {
            minChest = 2;
            maxChest = 3;
        }


        // =====================================================
        // SAFETY
        // =====================================================

        minChest =
            Mathf.Max(
                0,
                minChest
            );


        maxChest =
            Mathf.Max(
                minChest,
                maxChest
            );


        // =====================================================
        // RANDOM COUNT
        // =====================================================

        int chestCount =
            Random.Range(
                minChest,
                maxChest + 1
            );


        Debug.Log(
            "====================================\n" +
            "[WeaponSpawner]\n" +
            "Players: " +
            playerCount +
            "\n" +
            "Chest Wave: " +
            chestCount +
            "\n" +
            "Active Chests: " +
            activeChests.Count +
            "\n" +
            "Spawn Area: Central Map\n" +
            "===================================="
        );


        // =====================================================
        // SPAWN
        // =====================================================

        int spawnedCount = 0;


        for (
            int i = 0;
            i < chestCount;
            i++
        )
        {
            if (SpawnChest())
            {
                spawnedCount++;
            }
        }


        Debug.Log(
            "[WeaponSpawner] " +
            "Spawn thành công: " +
            spawnedCount +
            "/" +
            chestCount
        );
    }


    // =========================================================
    // GET PLAYER COUNT
    // =========================================================

    private int GetPlayerCount()
    {
        if (
            Runner == null ||
            !Runner.IsRunning
        )
        {
            return 0;
        }


        int count = 0;


        foreach (
            PlayerRef player
            in Runner.ActivePlayers
        )
        {
            count++;
        }


        return count;
    }


    // =========================================================
    // SPAWN CHEST
    // =========================================================

    private bool SpawnChest()
    {
        // =====================================================
        // TERRAIN CHECK
        // =====================================================

        if (terrain == null)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "Chưa gán Terrain!"
            );

            return false;
        }


        // =====================================================
        // PREFAB CHECK
        // =====================================================

        if (!chestPrefab.IsValid)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "Chưa gán Chest Prefab!"
            );

            return false;
        }


        // =====================================================
        // RUNNER CHECK
        // =====================================================

        if (
            Runner == null ||
            !Runner.IsRunning
        )
        {
            return false;
        }


        // =====================================================
        // TERRAIN DATA
        // =====================================================

        TerrainData terrainData =
            terrain.terrainData;


        if (terrainData == null)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "TerrainData không hợp lệ!"
            );

            return false;
        }


        Vector3 terrainPosition =
            terrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        // =====================================================
        // CENTRAL AREA
        // =====================================================

        float areaPercent =
            Mathf.Clamp(
                centerAreaPercent,
                0.1f,
                1f
            );


        float sideMarginPercent =
            (1f - areaPercent) * 0.5f;


        float minX =
            terrainPosition.x +
            terrainSize.x *
            sideMarginPercent;


        float maxX =
            terrainPosition.x +
            terrainSize.x *
            (1f - sideMarginPercent);


        float minZ =
            terrainPosition.z +
            terrainSize.z *
            sideMarginPercent;


        float maxZ =
            terrainPosition.z +
            terrainSize.z *
            (1f - sideMarginPercent);


        // =====================================================
        // CENTER PADDING
        // =====================================================

        minX += centerAreaPadding;
        maxX -= centerAreaPadding;

        minZ += centerAreaPadding;
        maxZ -= centerAreaPadding;


        // =====================================================
        // TERRAIN EDGE PADDING
        // =====================================================

        minX =
            Mathf.Max(
                minX,
                terrainPosition.x +
                terrainEdgePadding
            );


        maxX =
            Mathf.Min(
                maxX,
                terrainPosition.x +
                terrainSize.x -
                terrainEdgePadding
            );


        minZ =
            Mathf.Max(
                minZ,
                terrainPosition.z +
                terrainEdgePadding
            );


        maxZ =
            Mathf.Min(
                maxZ,
                terrainPosition.z +
                terrainSize.z -
                terrainEdgePadding
            );


        // =====================================================
        // CHECK AREA
        // =====================================================

        if (
            minX >= maxX ||
            minZ >= maxZ
        )
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "Central spawn area không hợp lệ!"
            );

            return false;
        }


        // =====================================================
        // FIND RANDOM POSITION
        // =====================================================

        Vector3 spawnPosition =
            Vector3.zero;


        bool validPosition =
            false;


        for (
            int attempt = 0;
            attempt < 150;
            attempt++
        )
        {
            // =================================================
            // RANDOM X
            // =================================================

            float randomX =
                Random.Range(
                    minX,
                    maxX
                );


            // =================================================
            // RANDOM Z
            // =================================================

            float randomZ =
                Random.Range(
                    minZ,
                    maxZ
                );


            // =================================================
            // TEST POSITION
            // =================================================

            Vector3 testPosition =
                new Vector3(
                    randomX,
                    0f,
                    randomZ
                );


            // =================================================
            // TERRAIN HEIGHT
            // =================================================

            float groundY =
                terrain.SampleHeight(
                    testPosition
                ) +
                terrainPosition.y;


            testPosition.y =
                groundY +
                spawnOffset;


            // =================================================
            // CHECK SLOPE
            // =================================================

            if (
                !IsTerrainSlopeValid(
                    testPosition
                )
            )
            {
                continue;
            }


            // =================================================
            // CHECK DISTANCE
            // =================================================

            if (
                !IsPositionValid(
                    testPosition
                )
            )
            {
                continue;
            }


            // =================================================
            // FOUND
            // =================================================

            spawnPosition =
                testPosition;


            validPosition =
                true;


            break;
        }


        // =====================================================
        // FAILED
        // =====================================================

        if (!validPosition)
        {
            Debug.LogWarning(
                "[WeaponSpawner] " +
                "Không tìm được vị trí spawn hợp lệ."
            );

            return false;
        }


        // =====================================================
        // FUSION SPAWN
        // =====================================================

        NetworkObject chest =
            Runner.Spawn(
                chestPrefab,
                spawnPosition,
                Quaternion.identity
            );


        // =====================================================
        // SPAWN FAILED
        // =====================================================

        if (chest == null)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "Runner.Spawn Chest FAILED!"
            );

            return false;
        }


        // =====================================================
        // ADD ACTIVE CHEST
        // =====================================================

        activeChests.Add(
            chest
        );


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[WeaponSpawner] " +
            "Chest spawn tại: " +
            spawnPosition
        );


        return true;
    }


    // =========================================================
    // CHECK TERRAIN SLOPE
    // =========================================================

    private bool IsTerrainSlopeValid(
        Vector3 worldPosition)
    {
        if (terrain == null)
        {
            return false;
        }


        TerrainData terrainData =
            terrain.terrainData;


        if (terrainData == null)
        {
            return false;
        }


        Vector3 terrainPosition =
            terrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        // =====================================================
        // WORLD -> NORMALIZED
        // =====================================================

        float normalizedX =
            Mathf.InverseLerp(
                terrainPosition.x,
                terrainPosition.x +
                terrainSize.x,
                worldPosition.x
            );


        float normalizedZ =
            Mathf.InverseLerp(
                terrainPosition.z,
                terrainPosition.z +
                terrainSize.z,
                worldPosition.z
            );


        normalizedX =
            Mathf.Clamp01(
                normalizedX
            );


        normalizedZ =
            Mathf.Clamp01(
                normalizedZ
            );


        // =====================================================
        // GET SLOPE
        // =====================================================

        float slope =
            terrainData.GetSteepness(
                normalizedX,
                normalizedZ
            );


        // =====================================================
        // CHECK
        // =====================================================

        return slope <= maxSlopeAngle;
    }


    // =========================================================
    // CHECK POSITION
    // =========================================================

    private bool IsPositionValid(
        Vector3 position)
    {
        // =====================================================
        // CLEAN INACTIVE CHESTS
        // =====================================================

        CleanupInactiveChests();


        // =====================================================
        // CHECK ACTIVE CHESTS
        // =====================================================

        for (
            int i = 0;
            i < activeChests.Count;
            i++
        )
        {
            NetworkObject oldChest =
                activeChests[i];


            // =================================================
            // CHEST KHÔNG CÒN
            // =================================================

            if (oldChest == null)
            {
                continue;
            }


            // =================================================
            // OLD POSITION
            // =================================================

            Vector3 oldPosition =
                oldChest.transform.position;


            Vector2 newPos =
                new Vector2(
                    position.x,
                    position.z
                );


            Vector2 oldPos =
                new Vector2(
                    oldPosition.x,
                    oldPosition.z
                );


            // =================================================
            // DISTANCE
            // =================================================

            float distance =
                Vector2.Distance(
                    newPos,
                    oldPos
                );


            if (
                distance <
                minSpawnDistance
            )
            {
                return false;
            }
        }


        return true;
    }
}