using UnityEngine;
using Fusion;

public class WeaponChest : NetworkBehaviour
{
    // =========================================================
    // WEAPONS
    // =========================================================

    [Header("Weapons")]
    public NetworkPrefabRef riflePrefab;
    public NetworkPrefabRef pistolPrefab;
    public NetworkPrefabRef batPrefab;
    public NetworkPrefabRef shovelPrefab;


    // =========================================================
    // FALL
    // =========================================================

    [Header("Fall Settings")]
    public float fallSpeed = 8f;
    public float rotationSpeed = 100f;


    // =========================================================
    // GROUND
    // =========================================================

    [Header("Ground Detection")]
    public LayerMask groundLayer;


    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]
    public float interactDistance = 3f;


    // =========================================================
    // LIGHT
    // =========================================================

    [Header("Light Beam")]
    public GameObject lightBeam;


    // =========================================================
    // STATE
    // =========================================================

    private bool hasLanded = false;
    private bool opened = false;


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();

        if (lightBeam != null)
        {
            lightBeam.SetActive(true);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Chỉ State Authority xử lý rương
        if (!HasStateAuthority)
            return;


        if (!hasLanded)
        {
            Fall();
        }


        // Xoay rương
        transform.Rotate(
            Vector3.up * rotationSpeed * Time.deltaTime,
            Space.World
        );


        CheckPlayer();
    }


    // =========================================================
    // FALL
    // =========================================================

    private void Fall()
    {
        transform.position +=
            Vector3.down *
            fallSpeed *
            Time.deltaTime;


        Ray ray = new Ray(
            transform.position,
            Vector3.down
        );


        if (Physics.Raycast(
            ray,
            out RaycastHit hit,
            2f,
            groundLayer
        ))
        {
            transform.position =
                hit.point +
                Vector3.up * 0.5f;

            hasLanded = true;
        }
    }


    // =========================================================
    // CHECK PLAYER
    // =========================================================

    private void CheckPlayer()
    {
        if (opened)
            return;


        // Không dùng GameObject.FindGameObjectWithTag
        // trong multiplayer


        PlayerWeapon[] players =
            FindObjectsByType<PlayerWeapon>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        foreach (PlayerWeapon player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;


            float distance = Vector3.Distance(
                transform.position,
                player.transform.position
            );


            if (distance <= interactDistance)
            {
                // Chỉ player có Input Authority xử lý input
                if (!player.Object.HasInputAuthority)
                    continue;


                if (Input.GetKeyDown(KeyCode.E))
                {
                    OpenChest();
                }


                return;
            }
        }
    }


    // =========================================================
    // OPEN CHEST
    // =========================================================

    private void OpenChest()
    {
        if (opened)
            return;


        if (!HasStateAuthority)
            return;


        opened = true;


        // Tắt light
        if (lightBeam != null)
        {
            lightBeam.SetActive(false);
        }


        // Spawn weapon
        SpawnRandomWeapon();


        // Despawn chest
        Runner.Despawn(Object);
    }


    // =========================================================
    // SPAWN RANDOM WEAPON
    // =========================================================

    private void SpawnRandomWeapon()
    {
        int randomWeapon =
            Random.Range(0, 4);


        NetworkPrefabRef selectedPrefab =
            default;


        switch (randomWeapon)
        {
            case 0:
                selectedPrefab = riflePrefab;
                break;

            case 1:
                selectedPrefab = pistolPrefab;
                break;

            case 2:
                selectedPrefab = batPrefab;
                break;

            case 3:
                selectedPrefab = shovelPrefab;
                break;
        }


        if (!selectedPrefab.IsValid)
        {
            Debug.LogError(
                "[WeaponChest] NetworkPrefabRef không hợp lệ!"
            );

            return;
        }


        Vector3 spawnPosition =
            transform.position +
            Vector3.up * 0.5f;


        NetworkObject weapon =
            Runner.Spawn(
                selectedPrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (weapon == null)
        {
            Debug.LogError(
                "[WeaponChest] Spawn weapon FAILED!"
            );

            return;
        }


        Debug.Log(
            "[WeaponChest] Spawn weapon SUCCESS!"
        );
    }
}