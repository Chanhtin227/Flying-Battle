using UnityEngine;

public class WeaponPickup : MonoBehaviour
{
    public PlayerWeapon.WeaponType weaponType;

    bool canPickUp = false;
    PlayerWeapon playerWeapon;

    void Update()
    {
        if (canPickUp && Input.GetKeyDown(KeyCode.E))
        {
            playerWeapon.PickupWeapon(weaponType);

            Destroy(gameObject);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canPickUp = true;
            playerWeapon = other.GetComponent<PlayerWeapon>();

            Debug.Log("Nhấn E để nhặt");
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canPickUp = false;
        }
    }
}