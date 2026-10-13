using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class InventoryUI : MonoBehaviour
{
    // =========================================================
    // INVENTORY PANEL
    // =========================================================

    [Header("Inventory Panel")]
    public GameObject inventoryPanel;


    // =========================================================
    // INVENTORY CAPACITY
    // =========================================================

    [Header("Inventory Capacity")]
    public TMP_Text capacityText;
    public int maxInventorySlots = 20;


    // =========================================================
    // LEFT FRAME
    // =========================================================

    [Header("Permanent Left Frame")]
    public GameObject leftFrame;


    // =========================================================
    // CATEGORY BUTTONS
    // =========================================================

    [Header("Category Buttons")]
    public Button allButton;
    public Button weaponButton;
    public Button ammoButton;
    public Button medkitButton;


    // =========================================================
    // CONTENT
    // =========================================================

    [Header("Category Content")]
    public GameObject weaponContent;
    public GameObject ammoContent;
    public GameObject medkitContent;


    // =========================================================
    // WEAPON UI
    // =========================================================

    [Header("Weapon UI")]
    public GameObject rifleUI;
    public GameObject pistolUI;
    public GameObject batUI;
    public GameObject shovelUI;


    // =========================================================
    // AMMO UI
    // =========================================================

    [Header("Ammo UI")]
    public GameObject rifleAmmoUI;
    public GameObject pistolAmmoUI;


    // =========================================================
    // MEDKIT UI
    // =========================================================

    [Header("Medkit UI")]
    public GameObject smallMedkitUI;
    public GameObject mediumMedkitUI;
    public GameObject largeMedkitUI;


    // =========================================================
    // AMOUNT TEXT
    // =========================================================

    [Header("Amount Text")]
    public TMP_Text rifleAmmoText;
    public TMP_Text pistolAmmoText;

    public TMP_Text smallMedkitText;
    public TMP_Text mediumMedkitText;
    public TMP_Text largeMedkitText;


    // =========================================================
    // DYNAMIC SLOTS
    // =========================================================
    //
    // ItemUI
    // ItemUI (1)
    // ItemUI (2)
    // ItemUI (3)
    // ItemUI (4)
    //
    // =========================================================

    [Header("Dynamic Slots")]
    public RectTransform dynamicSlot01;
    public RectTransform dynamicSlot02;
    public RectTransform dynamicSlot03;
    public RectTransform dynamicSlot04;
    public RectTransform dynamicSlot05;


    // =========================================================
    // KEY
    // =========================================================

    [Header("Key")]
    public KeyCode inventoryKey = KeyCode.Tab;
    public KeyCode closeInventoryKey = KeyCode.Escape;


    // =========================================================
    // STATE
    // =========================================================

    private bool isOpen = false;

    public static bool IsInventoryOpen
    {
        get;
        private set;
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private PlayerWeapon playerWeapon;
    private PlayerHealth playerHealth;
    private PlayerMovement trackedPlayer;


    // =========================================================
    // DYNAMIC ITEM TYPE
    // =========================================================

    private enum DynamicItemType
    {
        RifleAmmo,
        PistolAmmo,
        SmallMedkit,
        MediumMedkit,
        LargeMedkit
    }


    // =========================================================
    // CATEGORY
    // =========================================================

    private enum InventoryCategory
    {
        All,
        Weapons,
        Ammo,
        Medkit
    }

    private InventoryCategory currentCategory =
        InventoryCategory.All;


    // =========================================================
    // PICKUP ORDER
    // =========================================================

    private readonly List<DynamicItemType> dynamicOrder =
        new List<DynamicItemType>();


    // =========================================================
    // PICKED FLAGS
    // =========================================================

    private bool rifleAmmoPicked;
    private bool pistolAmmoPicked;

    private bool smallMedkitPicked;
    private bool mediumMedkitPicked;
    private bool largeMedkitPicked;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log("[InventoryUI] START");


        // -----------------------------------------------------
        // CHECK PANEL
        // -----------------------------------------------------

        if (inventoryPanel == null)
        {
            Debug.LogError(
                "[InventoryUI] inventoryPanel chưa được gán!"
            );

            return;
        }


        // -----------------------------------------------------
        // CHECK HIERARCHY
        // -----------------------------------------------------

        if (inventoryPanel == gameObject)
        {
            Debug.LogError(
                "[InventoryUI] InventoryUI.cs không nên " +
                "nằm trên chính InventoryPanel!"
            );
        }


        if (transform.IsChildOf(inventoryPanel.transform))
        {
            Debug.LogError(
                "[InventoryUI] GameObject chứa InventoryUI " +
                "đang nằm bên trong InventoryPanel!"
            );

            Debug.LogError(
                "[InventoryUI] Hãy đưa InventoryUI ra ngoài."
            );
        }


        // -----------------------------------------------------
        // ĐÓNG INVENTORY
        // -----------------------------------------------------

        inventoryPanel.SetActive(false);

        isOpen = false;
        IsInventoryOpen = false;


        // -----------------------------------------------------
        // CURSOR
        // -----------------------------------------------------

        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;


        // -----------------------------------------------------
        // LEFT FRAME
        // -----------------------------------------------------

        SetGameObject(
            leftFrame,
            true
        );


        // -----------------------------------------------------
        // CONTENT
        // -----------------------------------------------------

        SetGameObject(
            weaponContent,
            true
        );

        SetGameObject(
            ammoContent,
            true
        );

        SetGameObject(
            medkitContent,
            true
        );


        // -----------------------------------------------------
        // FIND PLAYER
        // -----------------------------------------------------

        FindLocalPlayer();


        // -----------------------------------------------------
        // CHECK SLOT
        // -----------------------------------------------------

        CheckDynamicSlots();


        // -----------------------------------------------------
        // SETUP BUTTON
        // -----------------------------------------------------

        SetupCategoryButtons();


        // -----------------------------------------------------
        // DEFAULT CATEGORY
        // -----------------------------------------------------

        currentCategory =
            InventoryCategory.All;


        // -----------------------------------------------------
        // CAPACITY
        // -----------------------------------------------------

        UpdateInventoryCapacity();


        Debug.Log(
            "[InventoryUI] START DONE"
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // -----------------------------------------------------
        // FIND PLAYER
        // -----------------------------------------------------

        FindLocalPlayer();
        // -----------------------------------------------------
        // PLAYER ĐANG CHẾT / ĐANG CHỜ HỒI SINH
        //
        // Không cho mở balo.
        // Nếu balo đang mở thì đóng ngay.
        // -----------------------------------------------------

        if (IsLocalPlayerDead())
        {
            if (isOpen)
            {
                CloseInventory();
            }

            return;
        }


        // -----------------------------------------------------
        // TRACK ITEM
        // -----------------------------------------------------

        TrackDynamicItems();


        // -----------------------------------------------------
        // OPEN / CLOSE
        // -----------------------------------------------------

        if (Input.GetKeyDown(inventoryKey))
        {
            ToggleInventory();
        }


        // -----------------------------------------------------
        // ESC
        // -----------------------------------------------------

        if (
            isOpen &&
            Input.GetKeyDown(closeInventoryKey)
        )
        {
            CloseInventory();

            return;
        }


        // -----------------------------------------------------
        // UPDATE
        // -----------------------------------------------------

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
        PlayerMovement localPlayer =
            PlayerMovement.LocalPlayer;


        if (localPlayer == null)
        {
            return;
        }


        // -----------------------------------------------------
        // PLAYER MỚI
        // -----------------------------------------------------

        if (trackedPlayer != localPlayer)
        {
            trackedPlayer = localPlayer;


            playerWeapon =
                localPlayer.GetComponent<PlayerWeapon>();


            playerHealth =
                localPlayer.GetComponent<PlayerHealth>();


            ResetDynamicOrder();


            Debug.Log(
                "[InventoryUI] Local Player changed."
            );
        }


        // -----------------------------------------------------
        // FALLBACK
        // -----------------------------------------------------

        if (playerWeapon == null)
        {
            playerWeapon =
                localPlayer.GetComponent<PlayerWeapon>();
        }


        if (playerHealth == null)
        {
            playerHealth =
                localPlayer.GetComponent<PlayerHealth>();
        }
    }


    // =========================================================
    // CHECK LOCAL PLAYER DEAD
    // =========================================================

    private bool IsLocalPlayerDead()
    {
        // playerHealth đã được FindLocalPlayer() gán.
        if (playerHealth == null)
        {
            return false;
        }


        // Inventory này chỉ quan tâm Player local.
        if (!playerHealth.HasInputAuthority)
        {
            return false;
        }


        return playerHealth.IsDead;
    }


    // =========================================================
    // CHECK SLOTS
    // =========================================================

    private void CheckDynamicSlots()
    {
        bool valid = true;


        if (dynamicSlot01 == null)
        {
            Debug.LogError(
                "[InventoryUI] Dynamic Slot 01 chưa gán!"
            );

            valid = false;
        }


        if (dynamicSlot02 == null)
        {
            Debug.LogError(
                "[InventoryUI] Dynamic Slot 02 chưa gán!"
            );

            valid = false;
        }


        if (dynamicSlot03 == null)
        {
            Debug.LogError(
                "[InventoryUI] Dynamic Slot 03 chưa gán!"
            );

            valid = false;
        }


        if (dynamicSlot04 == null)
        {
            Debug.LogError(
                "[InventoryUI] Dynamic Slot 04 chưa gán!"
            );

            valid = false;
        }


        if (dynamicSlot05 == null)
        {
            Debug.LogError(
                "[InventoryUI] Dynamic Slot 05 chưa gán!"
            );

            valid = false;
        }


        if (valid)
        {
            Debug.Log(
                "[InventoryUI] Đã gán đủ 5 Dynamic Slot."
            );
        }
    }


    // =========================================================
    // SETUP BUTTONS
    // =========================================================

    private void SetupCategoryButtons()
    {
        if (allButton != null)
        {
            allButton.onClick.RemoveListener(ShowAll);
            allButton.onClick.AddListener(ShowAll);
            allButton.interactable = true;
        }
        else
        {
            Debug.LogError(
                "[InventoryUI] All Button chưa gán!"
            );
        }


        if (weaponButton != null)
        {
            weaponButton.onClick.RemoveListener(ShowWeapons);
            weaponButton.onClick.AddListener(ShowWeapons);
            weaponButton.interactable = true;
        }
        else
        {
            Debug.LogError(
                "[InventoryUI] Weapon Button chưa gán!"
            );
        }


        if (ammoButton != null)
        {
            ammoButton.onClick.RemoveListener(ShowAmmo);
            ammoButton.onClick.AddListener(ShowAmmo);
            ammoButton.interactable = true;
        }
        else
        {
            Debug.LogError(
                "[InventoryUI] Ammo Button chưa gán!"
            );
        }


        if (medkitButton != null)
        {
            medkitButton.onClick.RemoveListener(ShowMedkits);
            medkitButton.onClick.AddListener(ShowMedkits);
            medkitButton.interactable = true;
        }
        else
        {
            Debug.LogError(
                "[InventoryUI] Medkit Button chưa gán!"
            );
        }
    }


    // =========================================================
    // BUTTON ENABLE
    // =========================================================

    private void SetCategoryButtonsInteractable(
        bool state
    )
    {
        if (allButton != null)
        {
            allButton.gameObject.SetActive(true);
            allButton.interactable = state;
        }


        if (weaponButton != null)
        {
            weaponButton.gameObject.SetActive(true);
            weaponButton.interactable = state;
        }


        if (ammoButton != null)
        {
            ammoButton.gameObject.SetActive(true);
            ammoButton.interactable = state;
        }


        if (medkitButton != null)
        {
            medkitButton.gameObject.SetActive(true);
            medkitButton.interactable = state;
        }
    }


    // =========================================================
    // TOGGLE
    // =========================================================

    public void ToggleInventory()
    {
        if (inventoryPanel == null)
        {
            Debug.LogError(
                "[InventoryUI] inventoryPanel = NULL!"
            );

            return;
        }


        // Nếu Player đang chết / đang chờ hồi sinh:
        // tuyệt đối không cho mở Inventory.
        if (IsLocalPlayerDead())
        {
            if (isOpen)
            {
                CloseInventory();
            }

            Debug.Log(
                "[InventoryUI] Không thể mở balo khi Player đang chết."
            );

            return;
        }


        if (isOpen)
        {
            CloseInventory();
        }
        else
        {
            OpenInventory();
        }
    }


    // =========================================================
    // OPEN
    // =========================================================

    private void OpenInventory()
    {
        // Chặn thêm một lớp bảo vệ.
        // Kể cả nơi khác gọi OpenInventory thì Player chết
        // vẫn không thể mở balo.
        if (IsLocalPlayerDead())
        {
            return;
        }


        isOpen = true;
        IsInventoryOpen = true;


        inventoryPanel.SetActive(true);


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        SetGameObject(
            leftFrame,
            true
        );


        SetGameObject(
            weaponContent,
            true
        );


        SetCategoryButtonsInteractable(true);


        currentCategory =
            InventoryCategory.All;


        UpdateInventoryUI();


        Debug.Log(
            "[InventoryUI] INVENTORY OPEN"
        );
    }


    // =========================================================
    // CLOSE
    // =========================================================

    public void CloseInventory()
    {
        isOpen = false;
        IsInventoryOpen = false;


        if (inventoryPanel != null)
        {
            inventoryPanel.SetActive(false);
        }


        Cursor.lockState =
            CursorLockMode.Locked;

        Cursor.visible =
            false;


        Debug.Log(
            "[InventoryUI] INVENTORY CLOSE"
        );
    }


    // =========================================================
    // UPDATE INVENTORY UI
    // =========================================================

    public void UpdateInventoryUI()
    {
        if (inventoryPanel == null)
        {
            return;
        }


        // -----------------------------------------------------
        // FRAME
        // -----------------------------------------------------

        SetGameObject(
            leftFrame,
            true
        );


        // -----------------------------------------------------
        // BUTTONS
        // -----------------------------------------------------

        SetCategoryButtonsInteractable(true);


        // -----------------------------------------------------
        // CONTENT
        // -----------------------------------------------------

        SetGameObject(
            weaponContent,
            true
        );

        SetGameObject(
            ammoContent,
            true
        );

        SetGameObject(
            medkitContent,
            true
        );


        // -----------------------------------------------------
        // AMOUNT
        // -----------------------------------------------------

        UpdateAmountText();


        // -----------------------------------------------------
        // CAPACITY
        // -----------------------------------------------------

        UpdateInventoryCapacity();


        // -----------------------------------------------------
        // CATEGORY + ITEMS
        // -----------------------------------------------------

        ApplyCategoryVisibility();


        // -----------------------------------------------------
        // DYNAMIC SLOT
        // -----------------------------------------------------

        RefreshDynamicSlots();
    }


    // =========================================================
    // UPDATE AMOUNT TEXT
    // =========================================================

    private void UpdateAmountText()
    {
        if (playerWeapon != null)
        {
            int rifleAmmo =
                Mathf.Max(
                    0,
                    playerWeapon.RifleReserveAmmo
                );


            int pistolAmmo =
                Mathf.Max(
                    0,
                    playerWeapon.PistolReserveAmmo
                );


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
        }


        if (playerHealth != null)
        {
            int small =
                Mathf.Max(
                    0,
                    playerHealth.SmallMedkitCount
                );


            int medium =
                Mathf.Max(
                    0,
                    playerHealth.MediumMedkitCount
                );


            int large =
                Mathf.Max(
                    0,
                    playerHealth.LargeMedkitCount
                );


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


    // =========================================================
    // UPDATE WEAPON UI
    // =========================================================

    private void UpdateWeaponUI()
    {
        if (playerWeapon == null)
        {
            SetGameObject(rifleUI, false);
            SetGameObject(pistolUI, false);
            SetGameObject(batUI, false);
            SetGameObject(shovelUI, false);

            return;
        }


        SetGameObject(
            rifleUI,
            playerWeapon.HasRifle
        );


        SetGameObject(
            pistolUI,
            playerWeapon.HasPistol
        );


        SetGameObject(
            batUI,
            playerWeapon.HasBat
        );


        SetGameObject(
            shovelUI,
            playerWeapon.HasShovel
        );
    }


    // =========================================================
    // TRACK PICKUP
    // =========================================================

    private void TrackDynamicItems()
    {
        if (
            playerWeapon == null ||
            playerHealth == null
        )
        {
            return;
        }


        // -----------------------------------------------------
        // RIFLE AMMO
        // -----------------------------------------------------

        if (
            playerWeapon.HasPickedRifleAmmo &&
            !rifleAmmoPicked
        )
        {
            rifleAmmoPicked = true;


            AddDynamicItem(
                DynamicItemType.RifleAmmo
            );
        }


        // -----------------------------------------------------
        // PISTOL AMMO
        // -----------------------------------------------------

        if (
            playerWeapon.HasPickedPistolAmmo &&
            !pistolAmmoPicked
        )
        {
            pistolAmmoPicked = true;


            AddDynamicItem(
                DynamicItemType.PistolAmmo
            );
        }


        // -----------------------------------------------------
        // SMALL
        // -----------------------------------------------------

        if (
            playerHealth.SmallMedkitCount > 0 &&
            !smallMedkitPicked
        )
        {
            smallMedkitPicked = true;


            AddDynamicItem(
                DynamicItemType.SmallMedkit
            );
        }


        // -----------------------------------------------------
        // MEDIUM
        // -----------------------------------------------------

        if (
            playerHealth.MediumMedkitCount > 0 &&
            !mediumMedkitPicked
        )
        {
            mediumMedkitPicked = true;


            AddDynamicItem(
                DynamicItemType.MediumMedkit
            );
        }


        // -----------------------------------------------------
        // LARGE
        // -----------------------------------------------------

        if (
            playerHealth.LargeMedkitCount > 0 &&
            !largeMedkitPicked
        )
        {
            largeMedkitPicked = true;


            AddDynamicItem(
                DynamicItemType.LargeMedkit
            );
        }
    }


    // =========================================================
    // ADD PICKED ITEM
    // =========================================================

    private void AddDynamicItem(
        DynamicItemType item
    )
    {
        if (
            dynamicOrder.Contains(item)
        )
        {
            return;
        }


        if (
            dynamicOrder.Count >= 5
        )
        {
            return;
        }


        dynamicOrder.Add(item);


        Debug.Log(
            "[InventoryUI] PICKUP: " +
            item +
            " | ORDER: " +
            dynamicOrder.Count
        );


        RefreshDynamicSlots();
    }


    // =========================================================
    // CHECK ITEM OWNED
    // =========================================================

    private bool IsDynamicItemOwned(
        DynamicItemType type
    )
    {
        if (
            playerWeapon == null ||
            playerHealth == null
        )
        {
            return false;
        }


        switch (type)
        {
            case DynamicItemType.RifleAmmo:

                return
                    playerWeapon.RifleReserveAmmo > 0;


            case DynamicItemType.PistolAmmo:

                return
                    playerWeapon.PistolReserveAmmo > 0;


            case DynamicItemType.SmallMedkit:

                return
                    playerHealth.SmallMedkitCount > 0;


            case DynamicItemType.MediumMedkit:

                return
                    playerHealth.MediumMedkitCount > 0;


            case DynamicItemType.LargeMedkit:

                return
                    playerHealth.LargeMedkitCount > 0;
        }


        return false;
    }


    // =========================================================
    // CHECK ITEM BELONG CATEGORY
    // =========================================================

    private bool IsItemAllowedInCurrentCategory(
        DynamicItemType type
    )
    {
        switch (currentCategory)
        {
            case InventoryCategory.All:

                return true;


            case InventoryCategory.Weapons:

                return false;


            case InventoryCategory.Ammo:

                return
                    type == DynamicItemType.RifleAmmo ||
                    type == DynamicItemType.PistolAmmo;


            case InventoryCategory.Medkit:

                return
                    type == DynamicItemType.SmallMedkit ||
                    type == DynamicItemType.MediumMedkit ||
                    type == DynamicItemType.LargeMedkit;
        }


        return false;
    }


    // =========================================================
    // REFRESH DYNAMIC SLOTS
    // =========================================================

    private void RefreshDynamicSlots()
    {
        RectTransform[] slots =
        {
            dynamicSlot01,
            dynamicSlot02,
            dynamicSlot03,
            dynamicSlot04,
            dynamicSlot05
        };


        // -----------------------------------------------------
        // CHECK SLOT
        // -----------------------------------------------------

        for (
            int i = 0;
            i < slots.Length;
            i++
        )
        {
            if (slots[i] == null)
            {
                Debug.LogError(
                    "[InventoryUI] Thiếu Dynamic Slot!"
                );

                return;
            }
        }


        // =====================================================
        // ẨN TẤT CẢ ITEM ĐỘNG TRƯỚC
        // =====================================================

        SetGameObject(
            rifleAmmoUI,
            false
        );


        SetGameObject(
            pistolAmmoUI,
            false
        );


        SetGameObject(
            smallMedkitUI,
            false
        );


        SetGameObject(
            mediumMedkitUI,
            false
        );


        SetGameObject(
            largeMedkitUI,
            false
        );


        // =====================================================
        // LẤY ITEM THEO THỨ TỰ NHẶT
        // VÀ LỌC THEO CATEGORY
        // =====================================================

        List<DynamicItemType> visibleItems =
            new List<DynamicItemType>();


        for (
            int i = 0;
            i < dynamicOrder.Count;
            i++
        )
        {
            DynamicItemType type =
                dynamicOrder[i];


            // Không còn số lượng
            if (!IsDynamicItemOwned(type))
            {
                continue;
            }


            // Không thuộc category
            if (!IsItemAllowedInCurrentCategory(type))
            {
                continue;
            }


            if (!visibleItems.Contains(type))
            {
                visibleItems.Add(type);
            }
        }


        // =====================================================
        // ĐƯA ITEM VÀO SLOT
        // =====================================================

        for (
            int i = 0;
            i < visibleItems.Count &&
            i < slots.Length;
            i++
        )
        {
            DynamicItemType type =
                visibleItems[i];


            GameObject itemObject =
                GetDynamicObject(type);


            if (itemObject == null)
            {
                continue;
            }


            RectTransform itemRect =
                itemObject.GetComponent<RectTransform>();


            if (itemRect == null)
            {
                continue;
            }


            RectTransform slot =
                slots[i];


            // -------------------------------------------------
            // HIỆN
            // -------------------------------------------------

            itemObject.SetActive(true);


            // -------------------------------------------------
            // PARENT VÀO SLOT
            // -------------------------------------------------

            itemRect.SetParent(
                slot,
                false
            );


            // -------------------------------------------------
            // CĂN GIỮA
            // -------------------------------------------------

            itemRect.anchorMin =
                new Vector2(
                    0.5f,
                    0.5f
                );


            itemRect.anchorMax =
                new Vector2(
                    0.5f,
                    0.5f
                );


            itemRect.pivot =
                new Vector2(
                    0.5f,
                    0.5f
                );


            itemRect.anchoredPosition =
                Vector2.zero;


            itemRect.localRotation =
                Quaternion.identity;


            itemRect.localScale =
                Vector3.one;
        }
    }


    // =========================================================
    // GET DYNAMIC OBJECT
    // =========================================================

    private GameObject GetDynamicObject(
        DynamicItemType type
    )
    {
        switch (type)
        {
            case DynamicItemType.RifleAmmo:

                return rifleAmmoUI;


            case DynamicItemType.PistolAmmo:

                return pistolAmmoUI;


            case DynamicItemType.SmallMedkit:

                return smallMedkitUI;


            case DynamicItemType.MediumMedkit:

                return mediumMedkitUI;


            case DynamicItemType.LargeMedkit:

                return largeMedkitUI;
        }


        return null;
    }


    // =========================================================
    // APPLY CATEGORY
    // =========================================================

    private void ApplyCategoryVisibility()
    {
        // =====================================================
        // FRAME
        // =====================================================

        SetGameObject(
            leftFrame,
            true
        );


        // =====================================================
        // BUTTONS
        // =====================================================

        SetCategoryButtonsInteractable(true);


        // =====================================================
        // ALL
        // =====================================================

        if (
            currentCategory ==
            InventoryCategory.All
        )
        {
            ShowAllWeapons();

            return;
        }


        // =====================================================
        // WEAPON
        // =====================================================

        if (
            currentCategory ==
            InventoryCategory.Weapons
        )
        {
            ShowAllWeapons();

            return;
        }


        // =====================================================
        // AMMO
        // =====================================================

        if (
            currentCategory ==
            InventoryCategory.Ammo
        )
        {
            HideAllWeapons();

            return;
        }


        // =====================================================
        // MEDKIT
        // =====================================================

        if (
            currentCategory ==
            InventoryCategory.Medkit
        )
        {
            HideAllWeapons();

            return;
        }
    }


    // =========================================================
    // SHOW ALL WEAPONS
    // =========================================================

    private void ShowAllWeapons()
    {
        UpdateWeaponUI();
    }


    // =========================================================
    // HIDE ALL WEAPONS
    // =========================================================

    private void HideAllWeapons()
    {
        SetGameObject(
            rifleUI,
            false
        );


        SetGameObject(
            pistolUI,
            false
        );


        SetGameObject(
            batUI,
            false
        );


        SetGameObject(
            shovelUI,
            false
        );
    }


    // =========================================================
    // RESET ORDER
    // =========================================================

    public void ResetDynamicOrder()
    {
        dynamicOrder.Clear();


        rifleAmmoPicked = false;
        pistolAmmoPicked = false;

        smallMedkitPicked = false;
        mediumMedkitPicked = false;
        largeMedkitPicked = false;


        SetGameObject(
            rifleAmmoUI,
            false
        );


        SetGameObject(
            pistolAmmoUI,
            false
        );


        SetGameObject(
            smallMedkitUI,
            false
        );


        SetGameObject(
            mediumMedkitUI,
            false
        );


        SetGameObject(
            largeMedkitUI,
            false
        );


        Debug.Log(
            "[InventoryUI] RESET Dynamic Order"
        );
    }


    // =========================================================
    // CAPACITY
    // =========================================================

    private int GetUsedInventorySlots()
    {
        int usedSlots = 0;


        // -----------------------------------------------------
        // WEAPON
        // -----------------------------------------------------

        if (playerWeapon != null)
        {
            if (playerWeapon.HasRifle)
                usedSlots++;


            if (playerWeapon.HasPistol)
                usedSlots++;


            if (playerWeapon.HasBat)
                usedSlots++;


            if (playerWeapon.HasShovel)
                usedSlots++;


            // -------------------------------------------------
            // AMMO
            // -------------------------------------------------

            if (
                playerWeapon.RifleReserveAmmo > 0
            )
            {
                usedSlots++;
            }


            if (
                playerWeapon.PistolReserveAmmo > 0
            )
            {
                usedSlots++;
            }
        }


        // -----------------------------------------------------
        // MEDKIT
        // -----------------------------------------------------

        if (playerHealth != null)
        {
            if (
                playerHealth.SmallMedkitCount > 0
            )
            {
                usedSlots++;
            }


            if (
                playerHealth.MediumMedkitCount > 0
            )
            {
                usedSlots++;
            }


            if (
                playerHealth.LargeMedkitCount > 0
            )
            {
                usedSlots++;
            }
        }


        return usedSlots;
    }


    // =========================================================
    // CAPACITY TEXT
    // =========================================================

    private void UpdateInventoryCapacity()
    {
        if (capacityText == null)
        {
            return;
        }


        int used =
            GetUsedInventorySlots();


        used =
            Mathf.Clamp(
                used,
                0,
                maxInventorySlots
            );


        capacityText.text =
            used +
            " / " +
            maxInventorySlots;
    }


    // =========================================================
    // BUTTON ALL
    // =========================================================

    public void ShowAll()
    {
        if (!isOpen)
        {
            return;
        }


        currentCategory =
            InventoryCategory.All;


        Debug.Log(
            "[InventoryUI] CLICK ALL"
        );


        UpdateInventoryUI();
    }


    // =========================================================
    // BUTTON WEAPON
    // =========================================================

    public void ShowWeapons()
    {
        if (!isOpen)
        {
            return;
        }


        currentCategory =
            InventoryCategory.Weapons;


        Debug.Log(
            "[InventoryUI] CLICK WEAPON"
        );


        UpdateInventoryUI();
    }


    // =========================================================
    // BUTTON AMMO
    // =========================================================

    public void ShowAmmo()
    {
        if (!isOpen)
        {
            return;
        }


        currentCategory =
            InventoryCategory.Ammo;


        Debug.Log(
            "[InventoryUI] CLICK AMMO"
        );


        UpdateInventoryUI();
    }


    // =========================================================
    // BUTTON MEDKIT
    // =========================================================

    public void ShowMedkits()
    {
        if (!isOpen)
        {
            return;
        }


        currentCategory =
            InventoryCategory.Medkit;


        Debug.Log(
            "[InventoryUI] CLICK MEDKIT"
        );


        UpdateInventoryUI();
    }


    // =========================================================
    // HELPER
    // =========================================================

    private void SetGameObject(
        GameObject target,
        bool state
    )
    {
        if (target != null)
        {
            target.SetActive(state);
        }
    }
}