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
    public GameObject largeMedkitUI;

    [Header("Amount Text")]
    public TMP_Text rifleAmmoText;
    public TMP_Text pistolAmmoText;
    public TMP_Text smallMedkitText;
    public TMP_Text largeMedkitText;

    [Header("Key")]
    public KeyCode inventoryKey = KeyCode.Tab;

    private bool isOpen = false;

    void Start()
    {
        inventoryPanel.SetActive(false);

        UpdateInventoryUI();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        // Mở / đóng balo
        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }

        // Cập nhật UI
        UpdateInventoryUI();
    }

    void ToggleInventory()
    {
        isOpen = !isOpen;

        inventoryPanel.SetActive(isOpen);

        if (isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    public void UpdateInventoryUI()
    {
        if (InventoryManager.Instance == null)
            return;

        InventoryManager inventory = InventoryManager.Instance;

        // =========================
        // WEAPONS
        // =========================

        rifleUI.SetActive(inventory.hasRifle);

        pistolUI.SetActive(inventory.hasPistol);

        batUI.SetActive(inventory.hasBat);

        shovelUI.SetActive(inventory.hasShovel);

        // =========================
        // AMMO
        // =========================

        rifleAmmoUI.SetActive(inventory.rifleAmmo > 0);

        pistolAmmoUI.SetActive(inventory.pistolAmmo > 0);

        // =========================
        // MEDKIT
        // =========================

        smallMedkitUI.SetActive(inventory.smallMedkitCount > 0);

        largeMedkitUI.SetActive(inventory.largeMedkitCount > 0);

        // =========================
        // AMOUNT
        // =========================

        if (rifleAmmoText != null)
        {
            rifleAmmoText.text = "x" + inventory.rifleAmmo;
        }

        if (pistolAmmoText != null)
        {
            pistolAmmoText.text = "x" + inventory.pistolAmmo;
        }

        if (smallMedkitText != null)
        {
            smallMedkitText.text = "x" + inventory.smallMedkitCount;
        }

        if (largeMedkitText != null)
        {
            largeMedkitText.text = "x" + inventory.largeMedkitCount;
        }
    }
}