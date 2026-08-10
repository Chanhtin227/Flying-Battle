using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Chest")]
    public GameObject chestPrefab;

    [Header("Terrain")]
    public Terrain terrain;

    [Header("Spawn Settings")]
    [Range(1, 4)]
    public int chestCount = 4;

    // Khoảng cách tối thiểu giữa các rương
    public float minSpawnDistance = 30f;

    [Header("Spawn Height")]
    public float spawnHeight = 20f;

    [Header("Spawn Delay")]
    public float spawnDelay = 5f;

    [Header("Weapons")]
    public GameObject riflePrefab;
    public GameObject pistolPrefab;
    public GameObject batPrefab;
    public GameObject shovelPrefab;

    // Lưu các vị trí rương đã spawn
    private List<Vector3> spawnedPositions = new List<Vector3>();

    IEnumerator Start()
    {
        // Chờ 5 giây trước khi bắt đầu
        yield return new WaitForSeconds(spawnDelay);

        // Spawn từng rương
        for (int i = 0; i < chestCount; i++)
        {
            SpawnChest();

            // Nếu chưa phải rương cuối thì chờ 5 giây
            if (i < chestCount - 1)
            {
                yield return new WaitForSeconds(spawnDelay);
            }
        }

        Debug.Log("Đã spawn đủ " + chestCount + " rương!");
    }

    void SpawnChest()
    {
        if (terrain == null)
        {
            Debug.LogError("Chưa gán Terrain!");
            return;
        }

        if (chestPrefab == null)
        {
            Debug.LogError("Chưa gán Chest Prefab!");
            return;
        }

        TerrainData terrainData = terrain.terrainData;

        Vector3 terrainPosition = terrain.transform.position;
        Vector3 terrainSize = terrainData.size;

        Vector3 spawnPosition = Vector3.zero;

        bool validPosition = false;

        // Thử tối đa 100 lần tìm vị trí
        for (int attempt = 0; attempt < 100; attempt++)
        {
            // Random X
            float randomX = Random.Range(
                0f,
                terrainSize.x
            );

            // Random Z
            float randomZ = Random.Range(
                0f,
                terrainSize.z
            );

            // World position
            float worldX =
                terrainPosition.x + randomX;

            float worldZ =
                terrainPosition.z + randomZ;

            // Lấy độ cao Terrain
            float groundY = terrain.SampleHeight(
                new Vector3(worldX, 0f, worldZ)
            ) + terrainPosition.y;

            // Vị trí rương trên không
            spawnPosition = new Vector3(
                worldX,
                groundY + spawnHeight,
                worldZ
            );

            // Kiểm tra khoảng cách
            if (IsPositionValid(spawnPosition))
            {
                validPosition = true;
                break;
            }
        }

        // Không tìm được vị trí
        if (!validPosition)
        {
            Debug.LogWarning(
                "Không tìm được vị trí spawn phù hợp!"
            );

            return;
        }

        // Lưu vị trí
        spawnedPositions.Add(spawnPosition);

        // Tạo rương
        GameObject chest = Instantiate(
            chestPrefab,
            spawnPosition,
            Quaternion.identity
        );

        // Lấy WeaponChest
        WeaponChest chestScript =
            chest.GetComponent<WeaponChest>();

        if (chestScript != null)
        {
            chestScript.riflePrefab = riflePrefab;
            chestScript.pistolPrefab = pistolPrefab;
            chestScript.batPrefab = batPrefab;
            chestScript.shovelPrefab = shovelPrefab;
        }

        Debug.Log(
            "Đã spawn rương " +
            spawnedPositions.Count +
            "/" +
            chestCount
        );
    }

    bool IsPositionValid(Vector3 position)
    {
        foreach (Vector3 oldPosition in spawnedPositions)
        {
            // Chỉ kiểm tra khoảng cách X/Z
            Vector2 newPos = new Vector2(
                position.x,
                position.z
            );

            Vector2 oldPos = new Vector2(
                oldPosition.x,
                oldPosition.z
            );

            float distance =
                Vector2.Distance(newPos, oldPos);

            if (distance < minSpawnDistance)
            {
                return false;
            }
        }

        return true;
    }
}