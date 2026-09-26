using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    // =========================================================
    // PLAYER SETTINGS
    // =========================================================

    [Header("Player Settings")]

    public NetworkPrefabRef playerPrefab;

    [Tooltip("Nâng Player lên khỏi mặt đất")]
    public float yOffset = 1.5f;

    [Tooltip("Khoảng cách tối thiểu giữa các Player")]
    public float minSpawnDistance = 40f;

    [Tooltip("Khoảng cách tránh mép Terrain")]
    public float edgePadding = 20f;

    [Tooltip("Số lần thử tìm vị trí")]
    public int maxSpawnAttempts = 100;


    // =========================================================
    // MAP SETTINGS
    // =========================================================

    [Header("Map Settings")]

    public Terrain mapTerrain;


    // =========================================================
    // SPAWNED PLAYERS
    // =========================================================

    // Lưu PlayerRef đã được spawn
    private readonly HashSet<PlayerRef> spawnedPlayers =
        new HashSet<PlayerRef>();


    // =========================================================
    // USED SPAWN POSITIONS
    // =========================================================

    // Lưu vị trí đã spawn
    private readonly List<Vector3> usedSpawnPositions =
        new List<Vector3>();


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        // Chỉ Host / Server spawn Player
        if (!Runner.IsServer)
            return;


        // =====================================================
        // TÌM TERRAIN
        // =====================================================

        FindTerrain();


        if (mapTerrain == null)
        {
            Debug.LogError(
                "[SpawnPlayer] KHÔNG TÌM THẤY TERRAIN!"
            );

            return;
        }


        Debug.Log(
            "[SpawnPlayer] Terrain = " +
            mapTerrain.name
        );


        // =====================================================
        // SPAWN NHỮNG PLAYER ĐÃ CÓ
        // =====================================================

        foreach (
            PlayerRef player
            in Runner.ActivePlayers
        )
        {
            SpawnPlayerOnTerrain(player);
        }
    }


    // =========================================================
    // FIND TERRAIN
    // =========================================================

    private void FindTerrain()
    {
        if (mapTerrain != null)
            return;


        mapTerrain =
            Terrain.activeTerrain;


        if (mapTerrain == null)
        {
            mapTerrain =
                FindFirstObjectByType<Terrain>();
        }
    }


    // =========================================================
    // PLAYER JOINED
    // =========================================================

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer)
            return;


        Debug.Log(
            "[SpawnPlayer] PlayerJoined = " +
            player
        );


        SpawnPlayerOnTerrain(player);
    }


    // =========================================================
    // SPAWN PLAYER
    // =========================================================

    private void SpawnPlayerOnTerrain(
        PlayerRef player
    )
    {
        if (!Runner.IsServer)
            return;


        // =====================================================
        // CHỐNG SPAWN TRÙNG
        // =====================================================

        if (spawnedPlayers.Contains(player))
        {
            Debug.Log(
                "[SpawnPlayer] Player " +
                player +
                " đã spawn rồi."
            );

            return;
        }


        // =====================================================
        // KIỂM TRA PREFAB
        // =====================================================

        if (!playerPrefab.IsValid)
        {
            Debug.LogError(
                "[SpawnPlayer] Player Prefab chưa được gán!"
            );

            return;
        }


        // =====================================================
        // TÌM TERRAIN
        // =====================================================

        FindTerrain();


        if (mapTerrain == null)
        {
            Debug.LogError(
                "[SpawnPlayer] Terrain = NULL!"
            );

            return;
        }


        // =====================================================
        // LẤY VỊ TRÍ RANDOM
        // =====================================================

        Vector3 spawnPosition;


        bool found =
            TryGetRandomSpawnPosition(
                out spawnPosition
            );


        if (!found)
        {
            Debug.LogWarning(
                "[SpawnPlayer] Không tìm được vị trí an toàn!"
            );


            spawnPosition =
                GetRandomFallbackPosition();
        }


        // =====================================================
        // SPAWN
        // =====================================================

        NetworkObject playerObject =
            Runner.Spawn(
                playerPrefab,
                spawnPosition,
                Quaternion.identity,
                player
            );


        if (playerObject == null)
        {
            Debug.LogError(
                "[SpawnPlayer] Spawn Player thất bại!"
            );

            return;
        }


        // =====================================================
        // LƯU TRẠNG THÁI
        // =====================================================

        spawnedPlayers.Add(player);

        usedSpawnPositions.Add(
            spawnPosition
        );


        Debug.Log(
            "====================================\n" +
            "PLAYER SPAWN\n" +
            "Player: " +
            player +
            "\nPosition: " +
            spawnPosition +
            "\n===================================="
        );
    }


    // =========================================================
    // RANDOM SPAWN POSITION
    // =========================================================

    private bool TryGetRandomSpawnPosition(
        out Vector3 spawnPosition
    )
    {
        spawnPosition =
            Vector3.zero;


        if (mapTerrain == null)
            return false;


        TerrainData terrainData =
            mapTerrain.terrainData;


        if (terrainData == null)
            return false;


        // =====================================================
        // TERRAIN
        // =====================================================

        Vector3 terrainPosition =
            mapTerrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        // =====================================================
        // RANGE
        // =====================================================

        float minX =
            terrainPosition.x +
            edgePadding;


        float maxX =
            terrainPosition.x +
            terrainSize.x -
            edgePadding;


        float minZ =
            terrainPosition.z +
            edgePadding;


        float maxZ =
            terrainPosition.z +
            terrainSize.z -
            edgePadding;


        if (
            minX >= maxX ||
            minZ >= maxZ
        )
        {
            Debug.LogError(
                "[SpawnPlayer] Terrain quá nhỏ " +
                "hoặc Edge Padding quá lớn!"
            );

            return false;
        }


        // =====================================================
        // RANDOM
        // =====================================================

        for (
            int attempt = 0;
            attempt < maxSpawnAttempts;
            attempt++
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


            // =================================================
            // TERRAIN HEIGHT
            // =================================================

            float height =
                mapTerrain.SampleHeight(
                    candidate
                );


            candidate.y =
                height +
                terrainPosition.y +
                yOffset;


            // =================================================
            // CHECK SPAWN
            // =================================================

            if (
                IsSpawnPositionSafe(
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
    // CHECK POSITION SAFE
    // =========================================================

    private bool IsSpawnPositionSafe(
        Vector3 position
    )
    {
        // =====================================================
        // CHECK VỚI VỊ TRÍ ĐÃ DÙNG
        // =====================================================

        foreach (
            Vector3 usedPosition
            in usedSpawnPositions
        )
        {
            float distance =
                Vector3.Distance(
                    position,
                    usedPosition
                );


            if (
                distance <
                minSpawnDistance
            )
            {
                return false;
            }
        }


        // =====================================================
        // CHECK VỚI PLAYER ĐANG CÓ
        // =====================================================

        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        foreach (
            PlayerHealth player
            in players
        )
        {
            if (
                player == null ||
                player.Object == null ||
                !player.Object.IsValid
            )
            {
                continue;
            }


            float distance =
                Vector3.Distance(
                    position,
                    player.transform.position
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


    // =========================================================
    // FALLBACK
    // =========================================================

    private Vector3 GetRandomFallbackPosition()
    {
        if (mapTerrain == null)
            return Vector3.zero;


        TerrainData terrainData =
            mapTerrain.terrainData;


        Vector3 terrainPosition =
            mapTerrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        float randomX =
            Random.Range(
                terrainPosition.x + edgePadding,
                terrainPosition.x +
                terrainSize.x -
                edgePadding
            );


        float randomZ =
            Random.Range(
                terrainPosition.z + edgePadding,
                terrainPosition.z +
                terrainSize.z -
                edgePadding
            );


        Vector3 position =
            new Vector3(
                randomX,
                0f,
                randomZ
            );


        float height =
            mapTerrain.SampleHeight(
                position
            );


        position.y =
            height +
            terrainPosition.y +
            yOffset;


        return position;
    }
}