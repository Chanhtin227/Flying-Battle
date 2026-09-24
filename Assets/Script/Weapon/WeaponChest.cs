using UnityEngine;
using Fusion;

public class WeaponChest : NetworkBehaviour
{
    [Header("Weapons")]
    public NetworkPrefabRef riflePrefab;
    public NetworkPrefabRef pistolPrefab;
    public NetworkPrefabRef batPrefab;
    public NetworkPrefabRef shovelPrefab;

    [Header("Fall Settings")]
    public float fallSpeed = 8f;
    public float rotationSpeed = 100f;

    [Header("Ground Detection")]
    public LayerMask groundLayer;

    [Header("Interaction")]
    public float interactDistance = 3f;

    [Header("Light Beam")]
    public GameObject lightBeam;

    private bool hasLanded = false;
    private bool opened = false;

    public override void Spawned()
    {
        base.Spawned();
        hasLanded = true;

        if (lightBeam != null)
        {
            lightBeam.SetActive(true);
        }
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid || opened)
            return;

        if (HasStateAuthority)
        {
            if (!hasLanded)
            {
                Fall();
            }

            transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        }
    }

    private void Fall()
    {
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;
        Ray ray = new Ray(transform.position, Vector3.down);

        if (Physics.Raycast(ray, out RaycastHit hit, 2f, groundLayer))
        {
            transform.position = hit.point + Vector3.up * 0.5f;
            hasLanded = true;
        }
    }

    public void TryOpenFrom(PlayerWeapon player)
    {
        if (!HasStateAuthority || opened || Object == null || !Object.IsValid || player == null || player.Object == null || !player.Object.IsValid)
            return;

        Vector2 objectPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerPos = new Vector2(player.transform.position.x, player.transform.position.z);
        float distance = Vector2.Distance(objectPos, playerPos);

        if (distance > interactDistance) return;

        OpenChest();
    }

    private void OpenChest()
    {
        if (opened || !HasStateAuthority) return;

        opened = true;

        if (lightBeam != null)
        {
            lightBeam.SetActive(false);
        }

        SpawnRandomWeapon();
        Runner.Despawn(Object);
    }

    private void SpawnRandomWeapon()
    {
        int randomWeapon = Random.Range(0, 4);
        NetworkPrefabRef selectedPrefab = default;

        switch (randomWeapon)
        {
            case 0: selectedPrefab = riflePrefab; break;
            case 1: selectedPrefab = pistolPrefab; break;
            case 2: selectedPrefab = batPrefab; break;
            case 3: selectedPrefab = shovelPrefab; break;
        }

        if (!selectedPrefab.IsValid) return;

        Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
        NetworkObject weapon = Runner.Spawn(selectedPrefab, spawnPosition, Quaternion.identity);
    }
}