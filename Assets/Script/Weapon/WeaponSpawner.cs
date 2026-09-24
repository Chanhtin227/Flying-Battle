using UnityEngine;
using UnityEngine.Serialization;
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
    // SPAWN SETTINGS
    // =========================================================

    [Header("Spawn Settings")]

    [Range(1, 4)]
    public int chestCount = 4;

    public float minSpawnDistance = 30f;


    // =========================================================
    // GROUND PLACEMENT
    // =========================================================

    private const float MaximumGroundOffset = 0.5f;

    [Header("Ground Placement")]
    [Tooltip("Small clearance above the sampled terrain. Chests are spawned already landed so every peer receives the same position.")]
    [FormerlySerializedAs("spawnHeight")]
    [Range(0f, MaximumGroundOffset)]
    public float groundOffset = 0.5f;


    // =========================================================
    // SPAWN DELAY
    // =========================================================

    [Header("Spawn Delay")]
    public float spawnDelay = 5f;


    // =========================================================
    // SPAWN POSITIONS
    // =========================================================

    private List<Vector3> spawnedPositions =
        new List<Vector3>();


    // =========================================================
    // START
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();


        // Chỉ State Authority được spawn rương
        if (!HasStateAuthority)
            return;


        StartCoroutine(SpawnChestRoutine());
    }


    // =========================================================
    // SPAWN ROUTINE
    // =========================================================

    private IEnumerator SpawnChestRoutine()
    {
        // Chờ 5 giây
        yield return new WaitForSeconds(spawnDelay);


        // Spawn từng rương
        for (int i = 0; i < chestCount; i++)
        {
            SpawnChest();


            // Nếu chưa phải rương cuối
            if (i < chestCount - 1)
            {
                yield return new WaitForSeconds(
                    spawnDelay
                );
            }
        }


        Debug.Log(
            "[WeaponSpawner] Đã spawn đủ " +
            chestCount +
            " rương!"
        );
    }


    // =========================================================
    // SPAWN CHEST
    // =========================================================

    private void SpawnChest()
    {
        // =====================================================
        // CHECK TERRAIN
        // =====================================================

        if (terrain == null)
        {
            Debug.LogError(
                "[WeaponSpawner] Chưa gán Terrain!"
            );

            return;
        }


        // =====================================================
        // CHECK PREFAB
        // =====================================================

        if (!chestPrefab.IsValid)
        {
            Debug.LogError(
                "[WeaponSpawner] Chest Prefab không hợp lệ!"
            );

            return;
        }


        // =====================================================
        // TERRAIN DATA
        // =====================================================

        TerrainData terrainData =
            terrain.terrainData;


        Vector3 terrainPosition =
            terrain.transform.position;


        Vector3 terrainSize =
            terrainData.size;


        Vector3 spawnPosition =
            Vector3.zero;


        bool validPosition =
            false;


        // =====================================================
        // FIND RANDOM POSITION
        // =====================================================

        for (int attempt = 0; attempt < 100; attempt++)
        {
            // Random X
            float randomX =
                Random.Range(
                    0f,
                    terrainSize.x
                );


            // Random Z
            float randomZ =
                Random.Range(
                    0f,
                    terrainSize.z
                );


            // World X
            float worldX =
                terrainPosition.x +
                randomX;


            // World Z
            float worldZ =
                terrainPosition.z +
                randomZ;


            // Ground Y
            float groundY =
                terrain.SampleHeight(
                    new Vector3(
                        worldX,
                        0f,
                        worldZ
                    )
                )
                +
                terrainPosition.y;


            // Spawn directly on terrain. A chest has no NetworkTransform, so
            // the authoritative peer must not spawn it in the air and move it
            // locally afterward; remote peers would keep the airborne position.
            float safeGroundOffset = Mathf.Clamp(
                groundOffset,
                0f,
                MaximumGroundOffset
            );

            spawnPosition =
                new Vector3(
                    worldX,
                    groundY + safeGroundOffset,
                    worldZ
                );


            // Check khoảng cách
            if (IsPositionValid(spawnPosition))
            {
                validPosition = true;
                break;
            }
        }


        // =====================================================
        // KHÔNG TÌM ĐƯỢC
        // =====================================================

        if (!validPosition)
        {
            Debug.LogWarning(
                "[WeaponSpawner] Không tìm được vị trí spawn!"
            );

            return;
        }


        // =====================================================
        // LƯU VỊ TRÍ
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


        if (chest == null)
        {
            Debug.LogError(
                "[WeaponSpawner] Runner.Spawn Chest FAILED!"
            );

            return;
        }


        Debug.Log(
            "[WeaponSpawner] Đã spawn rương " +
            spawnedPositions.Count +
            "/" +
            chestCount
        );
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


            if (distance < minSpawnDistance)
            {
                return false;
            }
        }


        return true;
    }
}
