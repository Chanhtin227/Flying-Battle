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
        "Khoảng cách tối thiểu giữa các rương."
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
    // SPAWN DELAY
    // =========================================================

    [Header("Spawn Delay")]

    [Tooltip(
        "Sau bao nhiêu giây sẽ tạo một đợt rương."
    )]
    public float spawnDelay = 5f;


    // =========================================================
    // CHEST COUNT
    // =========================================================

    [Header("Chest Count")]

    // 1v1
    public int minChestFor1v1 = 1;
    public int maxChestFor1v1 = 2;


    // 2v2
    public int minChestFor2v2 = 2;
    public int maxChestFor2v2 = 3;


    // =========================================================
    // SPAWN POSITIONS
    // =========================================================

    private List<Vector3> spawnedPositions =
        new List<Vector3>();


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();


        // Chỉ State Authority spawn
        if (!HasStateAuthority)
            return;


        // Tự tìm Terrain nếu chưa gán
        if (terrain == null)
        {
            terrain =
                Terrain.activeTerrain;
        }


        if (terrain == null)
        {
            terrain =
                FindFirstObjectByType<Terrain>();
        }


        if (terrain == null)
        {
            Debug.LogError(
                "[WeaponSpawner] " +
                "KHÔNG TÌM THẤY TERRAIN!"
            );

            return;
        }


        StartCoroutine(
            SpawnChestRoutine()
        );
    }


    // =========================================================
    // SPAWN ROUTINE
    // =========================================================

    private IEnumerator SpawnChestRoutine()
    {
        // Chờ trước khi spawn wave đầu tiên
        yield return new WaitForSeconds(
            spawnDelay
        );


        while (true)
        {
            SpawnChestWave();


            yield return new WaitForSeconds(
                spawnDelay
            );
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
        // CHEST COUNT
        // =====================================================

        int minChest = 1;
        int maxChest = 2;


        // -----------------------------------------------------
        // 1V1
        // -----------------------------------------------------

        if (playerCount <= 2)
        {
            minChest =
                minChestFor1v1;

            maxChest =
                maxChestFor1v1;
        }


        // -----------------------------------------------------
        // 2V2
        // -----------------------------------------------------

        else if (playerCount <= 4)
        {
            minChest =
                minChestFor2v2;

            maxChest =
                maxChestFor2v2;
        }


        // -----------------------------------------------------
        // NHIỀU PLAYER
        // -----------------------------------------------------

        else
        {
            minChest = 2;
            maxChest = 3;
        }


        // =====================================================
        // RANDOM CHEST COUNT
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
            "\nChest Wave: " +
            chestCount +
            "\nSpawn Area: Central Map" +
            "\n===================================="
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
        // CHECK TERRAIN
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
        // CHECK PREFAB
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


        // Phần bị cắt ở mỗi cạnh
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
        // PADDING
        // =====================================================

        minX += centerAreaPadding;
        maxX -= centerAreaPadding;

        minZ += centerAreaPadding;
        maxZ -= centerAreaPadding;


        // Đảm bảo vẫn nằm trong Terrain
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
        // RANDOM SEARCH
        // =====================================================

        Vector3 spawnPosition =
            Vector3.zero;


        bool validPosition =
            false;


        // Tăng số lần thử để tìm được địa hình tốt
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
            // WORLD POSITION
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
            // CHECK CHEST DISTANCE
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
                "Không tìm được vị trí spawn tốt " +
                "ở khu vực giữa map!"
            );

            return false;
        }


        // =====================================================
        // SAVE POSITION
        // =====================================================

        spawnedPositions.Add(
            spawnPosition
        );


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
        // CHECK
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
            return false;


        TerrainData terrainData =
            terrain.terrainData;


        Vector3 terrainPosition =
            terrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        // =====================================================
        // WORLD -> NORMALIZED TERRAIN POSITION
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
        foreach (
            Vector3 oldPosition
            in spawnedPositions
        )
        {
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