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
            bool pickedUp = playerWeapon.PickupWeapon(weaponType);

            // Chỉ xóa vũ khí trên đất khi nhặt thành công
            if (pickedUp)
            {
                Destroy(gameObject);
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerWeapon = other.GetComponent<PlayerWeapon>();

            if (playerWeapon != null)
            {
                canPickUp = true;
                Debug.Log("Nhấn E để nhặt");
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canPickUp = false;
            playerWeapon = null;
        }
    }
}