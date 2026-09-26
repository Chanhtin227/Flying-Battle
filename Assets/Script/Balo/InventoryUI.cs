using UnityEngine;
using TMPro;

public class InventoryUI : MonoBehaviour
{
    [Header("Inventory Panel")]
    public GameObject inventoryPanel;

    [Header("Weapon UI")]
    public GameObject rifleUI;
    public GameObject pistolUI;
    public GameObject batUI;
    public GameObject shovelUI;

    [Header("Ammo UI")]
    public GameObject rifleAmmoUI;
    public GameObject pistolAmmoUI;

    [Header("Medkit UI")]
    public GameObject smallMedkitUI;
    public GameObject mediumMedkitUI;
    public GameObject largeMedkitUI;

    [Header("Amount Text")]
    public TMP_Text rifleAmmoText;
    public TMP_Text pistolAmmoText;

    public TMP_Text smallMedkitText;
    public TMP_Text mediumMedkitText;
    public TMP_Text largeMedkitText;

    [Header("Key")]
    public KeyCode inventoryKey = KeyCode.Tab;

    private bool isOpen = false;

    private PlayerWeapon playerWeapon;
    private PlayerHealth playerHealth;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log("[InventoryUI] START");

        if (inventoryPanel == null)
        {
            Debug.LogError(
                "[InventoryUI] INVENTORY PANEL CHƯA ĐƯỢC GÁN!"
            );

            return;
        }

        inventoryPanel.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        FindLocalPlayer();

        // =====================================================
        // TAB
        // =====================================================

        if (Input.GetKeyDown(inventoryKey))
        {
            Debug.Log(
                "[InventoryUI] ĐÃ NHẤN TAB"
            );

            ToggleInventory();
        }

        // =====================================================
        // UPDATE UI
        // =====================================================

        if (isOpen)
        {
            UpdateInventoryUI();
        }
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        if (PlayerMovement.LocalPlayer == null)
            return;

        if (playerWeapon == null)
        {
            playerWeapon =
                PlayerMovement.LocalPlayer
                .GetComponent<PlayerWeapon>();
        }

        if (playerHealth == null)
        {
            playerHealth =
                PlayerMovement.LocalPlayer
                .GetComponent<PlayerHealth>();
        }
    }


    // =========================================================
    // TOGGLE INVENTORY
    // =========================================================

    private void ToggleInventory()
    {
        isOpen = !isOpen;

        Debug.Log(
            "[InventoryUI] Inventory: " +
            (isOpen ? "OPEN" : "CLOSE")
        );

        if (inventoryPanel == null)
        {
            Debug.LogError(
                "[InventoryUI] inventoryPanel = NULL!"
            );

            return;
        }

        inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState =
                CursorLockMode.None;

            Cursor.visible = true;

            UpdateInventoryUI();
        }
        else
        {
            Cursor.lockState =
                CursorLockMode.Locked;

            Cursor.visible = false;
        }
    }


    // =========================================================
    // UPDATE INVENTORY UI
    // =========================================================

    public void UpdateInventoryUI()
    {
        if (playerWeapon == null ||
            playerHealth == null)
        {
            return;
        }


        // =====================================================
        // WEAPONS
        // =====================================================

        if (rifleUI != null)
        {
            rifleUI.SetActive(
                playerWeapon.HasRifle
            );
        }

        if (pistolUI != null)
        {
            pistolUI.SetActive(
                playerWeapon.HasPistol
            );
        }

        if (batUI != null)
        {
            batUI.SetActive(
                playerWeapon.HasBat
            );
        }

        if (shovelUI != null)
        {
            shovelUI.SetActive(
                playerWeapon.HasShovel
            );
        }


        // =====================================================
        // AMMO
        // =====================================================

        int rifleAmmo =
            playerWeapon.RifleReserveAmmo;

        int pistolAmmo =
            playerWeapon.PistolReserveAmmo;

        if (rifleAmmoUI != null)
        {
            rifleAmmoUI.SetActive(
                rifleAmmo > 0
            );
        }

        if (pistolAmmoUI != null)
        {
            pistolAmmoUI.SetActive(
                pistolAmmo > 0
            );
        }

        if (rifleAmmoText != null)
        {
            rifleAmmoText.text =
                "x" + rifleAmmo;
        }

        if (pistolAmmoText != null)
        {
            pistolAmmoText.text =
                "x" + pistolAmmo;
        }


        // =====================================================
        // MEDKIT
        // =====================================================

        int small =
            playerHealth.SmallMedkitCount;

        int medium =
            playerHealth.MediumMedkitCount;

        int large =
            playerHealth.LargeMedkitCount;


        if (smallMedkitUI != null)
        {
            smallMedkitUI.SetActive(
                small > 0
            );
        }

        if (mediumMedkitUI != null)
        {
            mediumMedkitUI.SetActive(
                medium > 0
            );
        }

        if (largeMedkitUI != null)
        {
            largeMedkitUI.SetActive(
                large > 0
            );
        }


        // =====================================================
        // TEXT
        // =====================================================

        if (smallMedkitText != null)
        {
            smallMedkitText.text =
                "x" + small;
        }

        if (mediumMedkitText != null)
        {
            mediumMedkitText.text =
                "x" + medium;
        }

        if (largeMedkitText != null)
        {
            largeMedkitText.text =
                "x" + large;
        }
    }
}