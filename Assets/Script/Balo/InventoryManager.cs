using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Weapons")]
    public bool hasRifle = false;
    public bool hasPistol = false;
    public bool hasBat = false;
    public bool hasShovel = false;

    [Header("Ammo")]
    public int rifleAmmo = 0;
    public int pistolAmmo = 0;

    [Header("Medkits")]
    public int smallMedkitCount = 0;
    public int largeMedkitCount = 0;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // WEAPON
    // =========================

    public void AddWeapon(string weaponName)
    {
        switch (weaponName)
        {
            case "Rifle":
                hasRifle = true;
                break;

            case "Pistol":
                hasPistol = true;
                break;

            case "Bat":
                hasBat = true;
                break;

            case "Shovel":
                hasShovel = true;
                break;
        }

        Debug.Log("Inventory: Đã thêm " + weaponName);
    }

    // =========================
    // AMMO
    // =========================

    public void AddRifleAmmo(int amount)
    {
        rifleAmmo += amount;

        Debug.Log("Inventory: Rifle Ammo +" + amount +
                  " | Tổng: " + rifleAmmo);
    }

    public void AddPistolAmmo(int amount)
    {
        pistolAmmo += amount;

        Debug.Log("Inventory: Pistol Ammo +" + amount +
                  " | Tổng: " + pistolAmmo);
    }

    // =========================
    // MEDKIT
    // =========================

    public void AddSmallMedkit(int amount)
    {
        smallMedkitCount += amount;

        Debug.Log("Inventory: Small Medkit +" + amount +
                  " | Tổng: " + smallMedkitCount);
    }

    public void AddLargeMedkit(int amount)
    {
        largeMedkitCount += amount;

        Debug.Log("Inventory: Large Medkit +" + amount +
                  " | Tổng: " + largeMedkitCount);
    }
}