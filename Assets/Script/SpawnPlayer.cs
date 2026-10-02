using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    [Header("Player Settings")]
    public NetworkPrefabRef playerPrefab;
    public float yOffset = 1.5f;
    public float minSpawnDistance = 40f;
    public float edgePadding = 20f;
    public int maxSpawnAttempts = 100;

    [Header("Map Settings")]
    public Terrain mapTerrain;

    private readonly HashSet<PlayerRef> spawnedPlayers = new();
    private readonly List<Vector3> usedSpawnPositions = new();  

    public override void Spawned()
    {
        if (!Runner.IsServer) return;
        FindTerrain();
        if (mapTerrain == null) return;

        foreach (PlayerRef player in Runner.ActivePlayers)
        {
            SpawnPlayerOnTerrain(player);
        }
    }

    private void FindTerrain()
    {
        if (mapTerrain != null) return;

        mapTerrain = Terrain.activeTerrain;

        if (mapTerrain == null)
            mapTerrain = FindAnyObjectByType<Terrain>();
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!Runner.IsServer) return;
        SpawnPlayerOnTerrain(player);
    }

    private void SpawnPlayerOnTerrain(PlayerRef player)
    {
        if (!Runner.IsServer || spawnedPlayers.Contains(player) || !playerPrefab.IsValid) return;
        FindTerrain();
        if (mapTerrain == null) return;

        if (!TryGetRandomSpawnPosition(out Vector3 spawnPosition))
        {
            spawnPosition = GetRandomFallbackPosition();
        }

        NetworkObject playerObject = Runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
        if (playerObject != null)
        {
            spawnedPlayers.Add(player);
            usedSpawnPositions.Add(spawnPosition);
        }
    }

    private bool TryGetRandomSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = Vector3.zero;
        if (mapTerrain == null || mapTerrain.terrainData == null) return false;

        Vector3 tPos = mapTerrain.transform.position;
        Vector3 tSize = mapTerrain.terrainData.size;

        float padX = Mathf.Min(edgePadding, tSize.x / 3f);
        float padZ = Mathf.Min(edgePadding, tSize.z / 3f);
        float minX = tPos.x + padX, maxX = tPos.x + tSize.x - padX;
        float minZ = tPos.z + padZ, maxZ = tPos.z + tSize.z - padZ;

        for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
        {
            Vector3 candidate = new Vector3(Random.Range(minX, maxX), 0f, Random.Range(minZ, maxZ));
            candidate.y = mapTerrain.SampleHeight(candidate) + tPos.y + yOffset;

            if (IsSpawnPositionSafe(candidate))
            {
                spawnPosition = candidate;
                return true;
            }
        }
        return false;
    }

    private bool IsSpawnPositionSafe(Vector3 position)
    {
        foreach (Vector3 used in usedSpawnPositions)
        {
            if (Vector3.Distance(position, used) < minSpawnDistance)
                return false;
        }

        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(FindObjectsInactive.Exclude);

        foreach (PlayerHealth p in players)
        {
            if (p != null && p.Object != null && p.Object.IsValid)
            {
                if (Vector3.Distance(position, p.transform.position) < minSpawnDistance)
                    return false;
            }
        }

        return true;
    }

    private Vector3 GetRandomFallbackPosition()
    {
        if (mapTerrain == null) return Vector3.zero;
        Vector3 tPos = mapTerrain.transform.position;
        Vector3 tSize = mapTerrain.terrainData.size;
        
        float padX = Mathf.Min(edgePadding, tSize.x / 3f);
        float padZ = Mathf.Min(edgePadding, tSize.z / 3f);

        Vector3 pos = new Vector3(Random.Range(tPos.x + padX, tPos.x + tSize.x - padX), 0f, Random.Range(tPos.z + padZ, tPos.z + tSize.z - padZ));
        pos.y = mapTerrain.SampleHeight(pos) + tPos.y + yOffset;
        return pos;
    }
}