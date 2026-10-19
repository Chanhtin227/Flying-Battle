using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class SpawnPlayer : NetworkBehaviour, IPlayerJoined, IPlayerLeft
{
    [Header("Player Prefab")]
    public NetworkPrefabRef playerPrefab;

    [Header("Waiting Map (small map)")]
    public Terrain waitingTerrain;
    [Min(0f)] public float waitingEdgePadding = 15f;

    [Header("Battle Map (large map)")]
    public Terrain mapTerrain;
    [Min(0f)] public float battleEdgePadding = 60f;

    [Header("Match Start")]
    [Tooltip("1v1 = 2, 2v2 = 4. All players spawn in the waiting map first.")]
    [Min(1)] public int requiredPlayers = 2;
    [Min(0f)] public float minWaitingTime = 5f;
    [Min(0f)] public float maxWaitingTime = 10f;

    [Header("Spawn Safety")]
    public float yOffset = 1.5f;
    [Min(0f)] public float minSpawnDistance = 40f;
    [Range(0f, 90f)] public float maxSpawnSlope = 30f;
    [Min(1)] public int maxSpawnAttempts = 200;
    public bool checkMinimumHeight = false;
    public float minTerrainHeight = 0f;

    [Networked] public TickTimer WaitingTimer { get; set; }
    [Networked] public NetworkBool CountdownStarted { get; set; }
    [Networked] public NetworkBool BattleStarted { get; set; }

    [Header("Fade Before Battle")]
    [Min(0.1f)] public float fadeToBlackSeconds = 1f;
    [Min(0f)] public float blackScreenHoldSeconds = 0.5f;
    [Networked] public NetworkBool TransitionStarted { get; set; }
    [Networked] public TickTimer TransitionTimer { get; set; }

    private readonly Dictionary<PlayerRef, NetworkObject> players = new();
    private readonly List<Vector3> waitingPositions = new();
    private readonly List<Vector3> battlePositions = new();

    public override void Spawned()
    {
        if (!HasStateAuthority) return;
        if (!ValidateTerrains()) return;

        // State is initialized only when this NetworkObject is first spawned.
        CountdownStarted = false;
        BattleStarted = false;
        TransitionStarted = false;
        TransitionTimer = TickTimer.None;
        WaitingTimer = TickTimer.None;

        foreach (PlayerRef player in Runner.ActivePlayers)
            SpawnForCurrentPhase(player);

        Debug.Log("[SpawnPlayer] Waiting for players before countdown.");
    }

    public void PlayerJoined(PlayerRef player)
    {
        if (!HasStateAuthority) return;
        if (!ValidateTerrains()) return;
        SpawnForCurrentPhase(player);
    }

    public void PlayerLeft(PlayerRef player)
    {
        if (!HasStateAuthority) return;

        if (players.TryGetValue(player, out NetworkObject obj))
        {
            players.Remove(player);
            if (obj != null && obj.IsValid) Runner.Despawn(obj);
        }

        // If someone leaves while waiting, stop the countdown until enough players reconnect.
        if (!BattleStarted && !TransitionStarted && CountdownStarted && CountConnectedPlayers() < requiredPlayers)
        {
            CountdownStarted = false;
            WaitingTimer = TickTimer.None;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || BattleStarted) return;

        // In case PlayerJoined fired before this scene NetworkObject became ready.
        foreach (PlayerRef player in Runner.ActivePlayers)
            if (!players.ContainsKey(player))
                SpawnForCurrentPhase(player);

        // Fade in on BOTH clients first; warp only after the blackout.
        if (TransitionStarted)
        {
            if (!TransitionTimer.Expired(Runner)) return;

            if (!ValidateTerrains()) return;
            var destinations = new Dictionary<PlayerRef, Vector3>();
            var reserved = new List<Vector3>();
            foreach (var pair in players)
            {
                if (pair.Value == null || !pair.Value.IsValid) continue;
                if (!TryFindPosition(mapTerrain, battleEdgePadding, reserved,
                                     out Vector3 destination))
                {
                    // Stay black and retry next tick rather than leave a player behind.
                    Debug.LogWarning($"[SpawnPlayer] Waiting for a safe battle position: {pair.Key}");
                    return;
                }
                destinations.Add(pair.Key, destination);
                reserved.Add(destination);
            }
            if (destinations.Count < requiredPlayers) return;

            battlePositions.Clear();
            foreach (var pair in destinations)
            {
                TeleportPlayer(players[pair.Key], pair.Value);
                battlePositions.Add(pair.Value);
                Debug.Log($"[SpawnPlayer] BATTLE TELEPORT {pair.Key} -> {pair.Value}");
            }
            waitingPositions.Clear();
            BattleStarted = true;
            Debug.Log("[SpawnPlayer] Battle started after fade; waiting map is locked.");
            return;
        }

        int connected = CountConnectedPlayers();
        if (connected < requiredPlayers)
        {
            if (CountdownStarted)
            {
                CountdownStarted = false;
                WaitingTimer = TickTimer.None;
            }
            return;
        }

        if (!CountdownStarted)
        {
            if (players.Count < requiredPlayers) return;
            float low = Mathf.Max(0f, minWaitingTime);
            float high = Mathf.Max(low, maxWaitingTime);
            float duration = Random.Range(low, high);
            WaitingTimer = TickTimer.CreateFromSeconds(Runner, duration);
            CountdownStarted = true;
            Debug.Log($"[SpawnPlayer] {connected} players ready. Battle starts in {duration:F1}s.");
            return;
        }

        if (!WaitingTimer.Expired(Runner)) return;
        if (!ValidateTerrains()) return;

        // Start a NETWORKED blackout first, not an immediate teleport.
        TransitionStarted = true;
        float blackoutDuration = Mathf.Max(0.1f, fadeToBlackSeconds) +
                                 Mathf.Max(0f, blackScreenHoldSeconds);
        TransitionTimer = TickTimer.CreateFromSeconds(Runner, blackoutDuration);
        Debug.Log("[SpawnPlayer] Fade to black started.");
    }

    private int CountConnectedPlayers()
    {
        int count = 0;
        foreach (PlayerRef p in Runner.ActivePlayers) count++;
        return count;
    }

    private bool ValidateTerrains()
    {
        if (waitingTerrain != null && waitingTerrain.terrainData != null &&
            mapTerrain != null && mapTerrain.terrainData != null) return true;
        Debug.LogError("[SpawnPlayer] Set Waiting Terrain = small map, Map Terrain = large map in Inspector.");
        return false;
    }

    private void SpawnForCurrentPhase(PlayerRef player)
    {
        if (players.TryGetValue(player, out NetworkObject existing))
        {
            if (existing != null && existing.IsValid) return;
            players.Remove(player);
        }
        if (!playerPrefab.IsValid) { Debug.LogError("[SpawnPlayer] Player Prefab is not assigned."); return; }

        Terrain target = BattleStarted ? mapTerrain : waitingTerrain;
        float padding = BattleStarted ? battleEdgePadding : waitingEdgePadding;
        List<Vector3> used = BattleStarted ? battlePositions : waitingPositions;

        if (!TryFindPosition(target, padding, used, out Vector3 pos))
        {
            Debug.LogError($"[SpawnPlayer] Could not spawn {player} on {target.name}. Reduce edge padding, slope restriction or separation.");
            return;
        }

        NetworkObject obj = Runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
        if (obj != null)
        {
            players[player] = obj;
            used.Add(pos);
            Debug.Log($"[SpawnPlayer] {player} spawned on {(BattleStarted ? "BATTLE" : "WAITING")} terrain at {pos}.");
        }
    }

    // Call from the existing server-side death system only if it creates a NEW player object.
    public NetworkObject RespawnPlayer(PlayerRef player)
    {
        if (!HasStateAuthority || !BattleStarted || !ValidateTerrains()) return null;
        if (!TryFindPosition(mapTerrain, battleEdgePadding, battlePositions, out Vector3 pos))
            return null;

        if (players.TryGetValue(player, out NetworkObject old))
        {
            players.Remove(player);
            if (old != null && old.IsValid) Runner.Despawn(old);
        }

        NetworkObject obj = Runner.Spawn(playerPrefab, pos, Quaternion.identity, player);
        if (obj != null)
        {
            players[player] = obj;
            battlePositions.Add(pos);
        }
        return obj;
    }

    // SpawnPlayer and PlayerHealth both move the existing Player object.
    // NetworkTransform.Teleport tells Fusion to synchronize the warp.
    private void TeleportPlayer(NetworkObject obj, Vector3 target)
    {
        if (!HasStateAuthority || obj == null || !obj.IsValid) return;
        CharacterController cc = obj.GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;

        PlayerMovement movement = obj.GetComponent<PlayerMovement>();
        if (movement != null) movement.ResetMovementState();

        NetworkTransform nt = obj.GetComponent<NetworkTransform>();
        if (nt != null) nt.Teleport(target);
        else obj.transform.position = target;

        if (cc != null) cc.enabled = true;
    }

    // The ONLY terrain permitted for health respawns after the battle starts.
    public bool TryGetBattleRespawnTerrain(out Terrain terrain)
    {
        terrain = null;
        if (!BattleStarted || mapTerrain == null || mapTerrain.terrainData == null)
            return false;
        terrain = mapTerrain;
        return true;
    }

    private bool TryFindPosition(Terrain terrain, float edge, List<Vector3> used, out Vector3 result)
    {
        result = Vector3.zero;
        if (terrain == null || terrain.terrainData == null) return false;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float padding = Mathf.Max(0f, edge);
        if (padding * 2f >= size.x || padding * 2f >= size.z)
        {
            Debug.LogError($"[SpawnPlayer] Padding {padding} is too big for terrain {terrain.name}: {size.x} x {size.z}.");
            return false;
        }

        // Preserve the edge restriction, but relax player-to-player spacing if needed.
        float[] separationFactors = { 1f, 0.75f, 0.5f, 0.25f, 0f };
        foreach (float factor in separationFactors)
        {
            for (int attempt = 0; attempt < maxSpawnAttempts; attempt++)
            {
                float x = Random.Range(origin.x + padding, origin.x + size.x - padding);
                float z = Random.Range(origin.z + padding, origin.z + size.z - padding);
                Vector3 candidate = new Vector3(x, 0f, z);
                float groundY = terrain.SampleHeight(candidate) + origin.y;
                candidate.y = groundY + yOffset;

                if (checkMinimumHeight && groundY < minTerrainHeight) continue;
                float nx = Mathf.Clamp01((x - origin.x) / size.x);
                float nz = Mathf.Clamp01((z - origin.z) / size.z);
                if (terrain.terrainData.GetSteepness(nx, nz) > maxSpawnSlope) continue;

                bool farEnough = true;
                foreach (Vector3 earlier in used)
                {
                    if (Vector2.Distance(new Vector2(x, z), new Vector2(earlier.x, earlier.z))
                        < minSpawnDistance * factor)
                    {
                        farEnough = false;
                        break;
                    }
                }
                if (!farEnough) continue;
                result = candidate;
                if (factor < 1f)
                    Debug.LogWarning($"[SpawnPlayer] Reduced player separation for {terrain.name} to find a valid spawn.");
                return true;
            }
        }
        return false;
    }
}
