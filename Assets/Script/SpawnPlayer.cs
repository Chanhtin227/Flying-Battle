using UnityEngine;
using Fusion;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined
{
    [Header("Player Settings")]
    public NetworkPrefabRef playerPrefab;

    [Tooltip("Raise the player a little to avoid clipping through the terrain on spawn.")]
    public float yOffset = 1.5f;

    [Tooltip("Desired distance between each pair of spawned players.")]
    public float spawnDistance = 10f;

    [Header("Map Settings")]
    public Terrain mapTerrain;

    private int spawnedPlayerCount;
    private Vector3 spawnPairCenter;
    private Vector3 spawnPairDirection = Vector3.forward;

    public override void Spawned()
    {
        if (Runner.IsServer)
        {
            spawnedPlayerCount = 0;

            Debug.Log("[TerrainSpawner] Map loaded. Spawning active players...");
            foreach (var player in Runner.ActivePlayers)
            {
                SpawnPlayerOnTerrain(player);
            }
        }
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (Runner.IsServer)
        {
            Debug.Log($"[TerrainSpawner] Player {player} joined. Spawning...");
            SpawnPlayerOnTerrain(player);
        }
    }

    private void SpawnPlayerOnTerrain(PlayerRef player)
    {
        if (mapTerrain == null)
            mapTerrain = Terrain.activeTerrain;

        Vector3 spawnPos = GetRandomPosition();
        Runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
    }

    private Vector3 GetRandomPosition()
    {
        float width = mapTerrain.terrainData.size.x;
        float length = mapTerrain.terrainData.size.z;
        Vector3 terrainPos = mapTerrain.transform.position;

        if (spawnedPlayerCount % 2 == 0)
        {
            float margin = 10f + spawnDistance * 0.5f;

            float randomX = Random.Range(terrainPos.x + margin, terrainPos.x + width - margin);
            float randomZ = Random.Range(terrainPos.z + margin, terrainPos.z + length - margin);

            float randomAngle = Random.Range(0f, Mathf.PI * 2f);
            spawnPairCenter = new Vector3(randomX, 0f, randomZ);
            spawnPairDirection = new Vector3(Mathf.Cos(randomAngle), 0f, Mathf.Sin(randomAngle)).normalized;
        }

        float side = spawnedPlayerCount % 2 == 0 ? -1f : 1f;
        Vector3 checkPos = spawnPairCenter + spawnPairDirection * (spawnDistance * 0.5f * side);
        float terrainHeightY = mapTerrain.SampleHeight(checkPos) + terrainPos.y;

        spawnedPlayerCount++;

        return new Vector3(checkPos.x, terrainHeightY + yOffset, checkPos.z);
    }
}
