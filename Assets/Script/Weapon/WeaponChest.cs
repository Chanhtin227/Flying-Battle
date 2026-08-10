using UnityEngine;

public class WeaponChest : MonoBehaviour
{
    [Header("Weapons")]
    public GameObject riflePrefab;
    public GameObject pistolPrefab;
    public GameObject batPrefab;
    public GameObject shovelPrefab;

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

    void Start()
    {
        // Luôn bật cột sáng khi rương spawn
        if (lightBeam != null)
        {
            lightBeam.SetActive(true);
        }
        else
        {
            Debug.LogWarning(
                "Chest chưa được gán Light Beam!"
            );
        }
    }

    void Update()
    {
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

    void Fall()
    {
        transform.position +=
            Vector3.down * fallSpeed * Time.deltaTime;

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
                hit.point + Vector3.up * 0.5f;

            hasLanded = true;
        }
    }

    void CheckPlayer()
    {
        if (opened)
            return;

        GameObject player =
            GameObject.FindGameObjectWithTag("Player");

        if (player == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            player.transform.position
        );

        if (distance <= interactDistance)
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                OpenChest();
            }
        }
    }

    void OpenChest()
    {
        opened = true;

        // Tắt cột sáng
        if (lightBeam != null)
        {
            lightBeam.SetActive(false);
        }

        SpawnRandomWeapon();

        Destroy(gameObject);
    }

    void SpawnRandomWeapon()
    {
        GameObject weapon = null;

        int randomWeapon = Random.Range(0, 4);

        switch (randomWeapon)
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

        if (weapon != null)
        {
            Instantiate(
                weapon,
                transform.position + Vector3.up * 0.5f,
                Quaternion.identity
            );
        }
    }
}