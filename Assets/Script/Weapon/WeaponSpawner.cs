using UnityEngine;

public class WeaponSpawner : MonoBehaviour
{
    [Header("Weapon Prefabs")]
    public GameObject riflePrefab;
    public GameObject pistolPrefab;
    public GameObject batPrefab;
    public GameObject shovelPrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    void Start()
    {
        SpawnRandomWeapon();
    }

    void SpawnRandomWeapon()
    {
        if (spawnPoints.Length == 0)
            return;

        // Chọn vị trí ngẫu nhiên
        Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

        // Chọn vũ khí ngẫu nhiên
        GameObject weapon = null;

        switch (Random.Range(0, 4))
        {
            case 0:
                weapon = riflePrefab;
                break;

            case 1:
                weapon = pistolPrefab;
                break;

            case 2:
                weapon = batPrefab;
                break;

            case 3:
                weapon = shovelPrefab;
                break;
        }

        Instantiate(weapon, point.position, point.rotation);
    }
}