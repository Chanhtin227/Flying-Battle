using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponInfoUI : MonoBehaviour
{
    // =========================================================
    // INFO PANEL
    // =========================================================

    [Header("Info Panel")]
    public GameObject infoPanel;


    // =========================================================
    // BASIC INFO
    // =========================================================

    [Header("Basic Info")]
    public TMP_Text weaponNameText;

    [Tooltip("Chỉ dùng để hiển thị icon vũ khí: Rifle, Pistol, Bat, Shovel")]
    public Image weaponIcon;

    [Tooltip("Chỉ dùng để hiển thị icon item: Ammo, Medkit")]
    public Image itemIcon;

    public TMP_Text descriptionText;


    // =========================================================
    // AMMO / ITEM INFO TEXT
    // =========================================================

    [Header("Ammo / Item Info")]
    public TMP_Text ammoText;


    // =========================================================
    // STAT BARS
    // =========================================================

    [Header("Stat Bars")]
    public Slider damageBar;
    public Slider fireRateBar;
    public Slider rangeBar;


    // =========================================================
    // WEAPON BUTTONS
    // =========================================================

    [Header("Weapon Buttons")]
    public Button rifleButton;
    public Button pistolButton;
    public Button batButton;
    public Button shovelButton;


    // =========================================================
    // AMMO BUTTONS
    // =========================================================

    [Header("Ammo Buttons")]
    public Button rifleAmmoButton;
    public Button pistolAmmoButton;


    // =========================================================
    // MEDKIT BUTTONS
    // =========================================================

    [Header("Medkit Buttons")]
    public Button smallMedkitButton;
    public Button mediumMedkitButton;
    public Button largeMedkitButton;


    // =========================================================
    // ACTION BUTTONS
    // =========================================================

    [Header("Action Buttons")]

    [Tooltip("Object chứa 2 nút SỬ DỤNG và BỎ ĐI")]
    public GameObject actionButtons;

    [Tooltip("Nút SỬ DỤNG")]
    public Button useButton;

    [Tooltip("Nút BỎ ĐI")]
    public Button dropButton;


    // =========================================================
    // WEAPON ICONS
    // =========================================================

    [Header("Weapon Icons")]
    public Sprite rifleIcon;
    public Sprite pistolIcon;
    public Sprite batIcon;
    public Sprite shovelIcon;


    // =========================================================
    // AMMO ICONS
    // =========================================================

    [Header("Ammo Icons")]
    public Sprite rifleAmmoIcon;
    public Sprite pistolAmmoIcon;


    // =========================================================
    // MEDKIT ICONS
    // =========================================================

    [Header("Medkit Icons")]
    public Sprite smallMedkitIcon;
    public Sprite mediumMedkitIcon;
    public Sprite largeMedkitIcon;


    // =========================================================
    // WEAPON DESCRIPTIONS
    // =========================================================

    [Header("Weapon Descriptions")]

    [TextArea(2, 5)]
    public string rifleDescription =
        "Súng trường tấn công mạnh mẽ, " +
        "phù hợp cho giao tranh tầm xa và tầm trung.";

    [TextArea(2, 5)]
    public string pistolDescription =
        "Súng ngắn nhỏ gọn, tốc độ bắn nhanh " +
        "và phù hợp cho chiến đấu tầm trung.";

    [TextArea(2, 5)]
    public string batDescription =
        "Vũ khí cận chiến đơn giản, " +
        "gây sát thương mạnh ở khoảng cách gần.";

    [TextArea(2, 5)]
    public string shovelDescription =
        "Xẻng có sát thương cận chiến cao, " +
        "phù hợp khi chiến đấu ở khoảng cách gần.";


    // =========================================================
    // ITEM DESCRIPTIONS
    // =========================================================

    [Header("Ammo Descriptions")]

    [TextArea(2, 5)]
    public string rifleAmmoDescription =
        "Đạn súng trường, sử dụng cho vũ khí Rifle.";

    [TextArea(2, 5)]
    public string pistolAmmoDescription =
        "Đạn súng lục, sử dụng cho vũ khí Pistol.";


    [Header("Medkit Descriptions")]

    [TextArea(2, 5)]
    public string smallMedkitDescription =
        "Bình máu nhỏ, giúp hồi một lượng máu cho người chơi.";

    [TextArea(2, 5)]
    public string mediumMedkitDescription =
        "Bình máu trung bình, giúp hồi nhiều máu hơn bình nhỏ.";

    [TextArea(2, 5)]
    public string largeMedkitDescription =
        "Bình máu lớn, giúp hồi một lượng máu rất cao.";


    // =========================================================
    // PLAYER
    // =========================================================

    private PlayerWeapon playerWeapon;
    private PlayerHealth playerHealth;


    // =========================================================
    // SELECTED WEAPON
    // =========================================================

    private PlayerWeapon.WeaponType selectedWeapon =
        PlayerWeapon.WeaponType.None;


    // =========================================================
    // INFO TYPE
    // =========================================================

    private enum InfoType
    {
        None,

        Weapon,

        RifleAmmo,
        PistolAmmo,

        SmallMedkit,
        MediumMedkit,
        LargeMedkit
    }

    private InfoType selectedInfoType =
        InfoType.None;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log(
            "[WeaponInfoUI] START"
        );

        FindLocalPlayer();

        SetupButtons();

        SetupInfoPanelRaycast();

        HideInfo();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // INVENTORY ĐÓNG
        // =====================================================

        if (!InventoryUI.IsInventoryOpen)
        {
            if (
                infoPanel != null &&
                infoPanel.activeSelf
            )
            {
                HideInfo();
            }

            return;
        }


        // =====================================================
        // TÌM LOCAL PLAYER
        // =====================================================

        FindLocalPlayer();


        // Panel chưa mở
        if (
            infoPanel == null ||
            !infoPanel.activeSelf
        )
        {
            return;
        }


        // =====================================================
        // ĐANG XEM WEAPON
        // =====================================================

        if (selectedInfoType == InfoType.Weapon)
        {
            if (!IsSelectedWeaponOwned())
            {
                Debug.Log(
                    "[WeaponInfoUI] " +
                    "Vũ khí đang xem không còn trong inventory."
                );

                HideInfo();

                return;
            }

            UpdateAmmo();

            return;
        }


        // =====================================================
        // ĐANG XEM AMMO / MEDKIT
        // =====================================================

        UpdateSelectedItemInfo();
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        if (PlayerMovement.LocalPlayer == null)
        {
            return;
        }


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
    // SETUP BUTTONS
    // =========================================================

    private void SetupButtons()
    {
        // =====================================================
        // WEAPONS
        // =====================================================

        if (rifleButton != null)
        {
            rifleButton.onClick.RemoveListener(
                ShowRifle
            );

            rifleButton.onClick.AddListener(
                ShowRifle
            );
        }


        if (pistolButton != null)
        {
            pistolButton.onClick.RemoveListener(
                ShowPistol
            );

            pistolButton.onClick.AddListener(
                ShowPistol
            );
        }


        if (batButton != null)
        {
            batButton.onClick.RemoveListener(
                ShowBat
            );

            batButton.onClick.AddListener(
                ShowBat
            );
        }


        if (shovelButton != null)
        {
            shovelButton.onClick.RemoveListener(
                ShowShovel
            );

            shovelButton.onClick.AddListener(
                ShowShovel
            );
        }


        // =====================================================
        // AMMO
        // =====================================================

        if (rifleAmmoButton != null)
        {
            rifleAmmoButton.onClick.RemoveListener(
                ShowRifleAmmo
            );

            rifleAmmoButton.onClick.AddListener(
                ShowRifleAmmo
            );
        }


        if (pistolAmmoButton != null)
        {
            pistolAmmoButton.onClick.RemoveListener(
                ShowPistolAmmo
            );

            pistolAmmoButton.onClick.AddListener(
                ShowPistolAmmo
            );
        }


        // =====================================================
        // MEDKIT
        // =====================================================

        if (smallMedkitButton != null)
        {
            smallMedkitButton.onClick.RemoveListener(
                ShowSmallMedkit
            );

            smallMedkitButton.onClick.AddListener(
                ShowSmallMedkit
            );
        }


        if (mediumMedkitButton != null)
        {
            mediumMedkitButton.onClick.RemoveListener(
                ShowMediumMedkit
            );

            mediumMedkitButton.onClick.AddListener(
                ShowMediumMedkit
            );
        }


        if (largeMedkitButton != null)
        {
            largeMedkitButton.onClick.RemoveListener(
                ShowLargeMedkit
            );

            largeMedkitButton.onClick.AddListener(
                ShowLargeMedkit
            );
        }


        // =====================================================
        // ACTION BUTTONS
        // =====================================================

        if (useButton != null)
        {
            useButton.onClick.RemoveListener(
                OnUseClicked
            );

            useButton.onClick.AddListener(
                OnUseClicked
            );
        }


        if (dropButton != null)
        {
            dropButton.onClick.RemoveListener(
                OnDropClicked
            );

            dropButton.onClick.AddListener(
                OnDropClicked
            );
        }
    }


    // =========================================================
    // FIX RAYCAST
    // =========================================================

    private void SetupInfoPanelRaycast()
    {
        if (infoPanel == null)
        {
            return;
        }


        Graphic[] graphics =
            infoPanel.GetComponentsInChildren<Graphic>(
                true
            );


        foreach (Graphic graphic in graphics)
        {
            if (graphic == null)
            {
                continue;
            }


            Selectable selectable =
                graphic.GetComponentInParent<Selectable>();


            if (selectable != null)
            {
                graphic.raycastTarget = true;
            }
            else
            {
                graphic.raycastTarget = false;
            }
        }
    }


    // =========================================================
    // SHOW WEAPON BUTTONS
    // =========================================================

    public void ShowRifle()
    {
        ShowWeapon(
            PlayerWeapon.WeaponType.Rifle
        );
    }


    public void ShowPistol()
    {
        ShowWeapon(
            PlayerWeapon.WeaponType.Pistol
        );
    }


    public void ShowBat()
    {
        ShowWeapon(
            PlayerWeapon.WeaponType.Bat
        );
    }


    public void ShowShovel()
    {
        ShowWeapon(
            PlayerWeapon.WeaponType.Shovel
        );
    }


    // =========================================================
    // SHOW WEAPON
    // =========================================================

    private void ShowWeapon(
        PlayerWeapon.WeaponType weaponType)
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        if (infoPanel == null)
        {
            Debug.LogError(
                "[WeaponInfoUI] infoPanel chưa được gán!"
            );

            return;
        }


        FindLocalPlayer();


        if (!IsWeaponOwned(weaponType))
        {
            Debug.Log(
                "[WeaponInfoUI] Không sở hữu vũ khí này."
            );

            HideInfo();

            return;
        }


        // =====================================================
        // LƯU LOẠI ĐANG XEM
        // =====================================================

        selectedInfoType =
            InfoType.Weapon;

        selectedWeapon =
            weaponType;


        // =====================================================
        // HIỆN PANEL
        // =====================================================

        infoPanel.SetActive(true);


        // =====================================================
        // HIỆN STAT BARS
        // =====================================================

        SetWeaponStatsVisible(true);


        // =====================================================
        // HIỆN ACTION BUTTONS
        // =====================================================

        if (actionButtons != null)
        {
            actionButtons.SetActive(true);
        }


        // =====================================================
        // SHOW INFO
        // =====================================================

        if (
            weaponType ==
            PlayerWeapon.WeaponType.Rifle
        )
        {
            ShowRifleInfo();

            return;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Pistol
        )
        {
            ShowPistolInfo();

            return;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Bat
        )
        {
            ShowBatInfo();

            return;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Shovel
        )
        {
            ShowShovelInfo();

            return;
        }
    }


    // =========================================================
    // RIFLE INFO
    // =========================================================

    private void ShowRifleInfo()
    {
        SetText(
            weaponNameText,
            "Rifle"
        );

        SetWeaponIcon(
            rifleIcon
        );

        SetText(
            descriptionText,
            rifleDescription
        );


        float damage = 25f;
        float fireRate = 10f;
        float range = 100f;


        SetSlider(
            damageBar,
            damage / 100f
        );

        SetSlider(
            fireRateBar,
            fireRate / 20f
        );

        SetSlider(
            rangeBar,
            range / 100f
        );


        UpdateAmmo();
    }


    // =========================================================
    // PISTOL INFO
    // =========================================================

    private void ShowPistolInfo()
    {
        SetText(
            weaponNameText,
            "Pistol"
        );

        SetWeaponIcon(
            pistolIcon
        );

        SetText(
            descriptionText,
            pistolDescription
        );


        float damage = 15f;
        float fireRate = 4f;
        float range = 70f;


        SetSlider(
            damageBar,
            damage / 100f
        );

        SetSlider(
            fireRateBar,
            fireRate / 20f
        );

        SetSlider(
            rangeBar,
            range / 100f
        );


        UpdateAmmo();
    }


    // =========================================================
    // BAT INFO
    // =========================================================

    private void ShowBatInfo()
    {
        SetText(
            weaponNameText,
            "Bat"
        );

        SetWeaponIcon(
            batIcon
        );

        SetText(
            descriptionText,
            batDescription
        );


        float damage = 30f;
        float attackSpeed = 2f;
        float range = 4f;


        SetSlider(
            damageBar,
            damage / 100f
        );

        SetSlider(
            fireRateBar,
            attackSpeed / 20f
        );

        SetSlider(
            rangeBar,
            range / 100f
        );


        SetText(
            ammoText,
            "Don't use bullets"
        );
    }


    // =========================================================
    // SHOVEL INFO
    // =========================================================

    private void ShowShovelInfo()
    {
        SetText(
            weaponNameText,
            "Shovel"
        );

        SetWeaponIcon(
            shovelIcon
        );

        SetText(
            descriptionText,
            shovelDescription
        );


        float damage = 40f;
        float attackSpeed = 2f;
        float range = 4f;


        SetSlider(
            damageBar,
            damage / 100f
        );

        SetSlider(
            fireRateBar,
            attackSpeed / 20f
        );

        SetSlider(
            rangeBar,
            range / 100f
        );


        SetText(
            ammoText,
            "Don't use bullets"
        );
    }


    // =========================================================
    // SHOW RIFLE AMMO
    // =========================================================

    public void ShowRifleAmmo()
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        FindLocalPlayer();


        if (
            playerWeapon == null ||
            playerWeapon.RifleReserveAmmo <= 0
        )
        {
            return;
        }


        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.RifleAmmo;


        ShowItemInfo(
            "Rifle Ammo",
            rifleAmmoIcon,
            rifleAmmoDescription,
            "Ammo: " +
            playerWeapon.RifleReserveAmmo +
            " viên"
        );
    }


    // =========================================================
    // SHOW PISTOL AMMO
    // =========================================================

    public void ShowPistolAmmo()
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        FindLocalPlayer();


        if (
            playerWeapon == null ||
            playerWeapon.PistolReserveAmmo <= 0
        )
        {
            return;
        }


        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.PistolAmmo;


        ShowItemInfo(
            "Pistol Ammo",
            pistolAmmoIcon,
            pistolAmmoDescription,
            "Ammo: " +
            playerWeapon.PistolReserveAmmo +
            " viên"
        );
    }


    // =========================================================
    // SHOW SMALL MEDKIT
    // =========================================================

    public void ShowSmallMedkit()
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        FindLocalPlayer();


        if (
            playerHealth == null ||
            playerHealth.SmallMedkitCount <= 0
        )
        {
            return;
        }


        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.SmallMedkit;


        ShowItemInfo(
            "Small Medkit",
            smallMedkitIcon,
            smallMedkitDescription,
            "Heal: +" +
            playerHealth.smallHealAmount +
            " HP | x" +
            playerHealth.SmallMedkitCount
        );
    }


    // =========================================================
    // SHOW MEDIUM MEDKIT
    // =========================================================

    public void ShowMediumMedkit()
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        FindLocalPlayer();


        if (
            playerHealth == null ||
            playerHealth.MediumMedkitCount <= 0
        )
        {
            return;
        }


        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.MediumMedkit;


        ShowItemInfo(
            "Medium Medkit",
            mediumMedkitIcon,
            mediumMedkitDescription,
            "Heal: +" +
            playerHealth.mediumHealAmount +
            " HP | x" +
            playerHealth.MediumMedkitCount
        );
    }


    // =========================================================
    // SHOW LARGE MEDKIT
    // =========================================================

    public void ShowLargeMedkit()
    {
        if (!InventoryUI.IsInventoryOpen)
        {
            return;
        }


        FindLocalPlayer();


        if (
            playerHealth == null ||
            playerHealth.LargeMedkitCount <= 0
        )
        {
            return;
        }


        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.LargeMedkit;


        ShowItemInfo(
            "Large Medkit",
            largeMedkitIcon,
            largeMedkitDescription,
            "Heal: +" +
            playerHealth.largeHealAmount +
            " HP | x" +
            playerHealth.LargeMedkitCount
        );
    }


    // =========================================================
    // SHOW ITEM INFO
    // =========================================================

    private void ShowItemInfo(
        string itemName,
        Sprite itemSprite,
        string itemDescription,
        string itemInfo)
    {
        if (infoPanel == null)
        {
            return;
        }


        infoPanel.SetActive(true);


        // Tên
        SetText(
            weaponNameText,
            itemName
        );


        // Icon item: Ammo / Medkit
        SetItemIcon(
            itemSprite
        );


        // Mô tả
        SetText(
            descriptionText,
            itemDescription
        );


        // Thông tin
        SetText(
            ammoText,
            itemInfo
        );


        // =====================================================
        // AMMO / MEDKIT KHÔNG CÓ WEAPON STAT
        // =====================================================

        SetWeaponStatsVisible(false);


        // =====================================================
        // HIỆN USE / THROW CHO AMMO + MEDKIT
        // =====================================================

        if (actionButtons != null)
        {
            actionButtons.SetActive(true);
        }
        else
        {
            Debug.LogError("[WeaponInfoUI] actionButtons chưa được gán trong Inspector!");
        }

        if (useButton != null)
        {
            useButton.gameObject.SetActive(true);
            useButton.interactable = true;
        }
        else
        {
            Debug.LogError("[WeaponInfoUI] useButton chưa được gán trong Inspector!");
        }

        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(true);
            dropButton.interactable = true;
        }
        else
        {
            Debug.LogError("[WeaponInfoUI] dropButton chưa được gán trong Inspector!");
        }

        Debug.Log("[WeaponInfoUI] SHOW ITEM: " + itemName + " | USE + THROW = ON");
    }


    // =========================================================
    // UPDATE SELECTED ITEM
    // =========================================================

    private void UpdateSelectedItemInfo()
    {
        FindLocalPlayer();


        // =====================================================
        // RIFLE AMMO
        // =====================================================

        if (
            selectedInfoType ==
            InfoType.RifleAmmo
        )
        {
            if (
                playerWeapon == null ||
                playerWeapon.RifleReserveAmmo <= 0
            )
            {
                HideInfo();

                return;
            }


            SetText(
                ammoText,
                "Ammo: " +
                playerWeapon.RifleReserveAmmo +
                " viên"
            );

            return;
        }


        // =====================================================
        // PISTOL AMMO
        // =====================================================

        if (
            selectedInfoType ==
            InfoType.PistolAmmo
        )
        {
            if (
                playerWeapon == null ||
                playerWeapon.PistolReserveAmmo <= 0
            )
            {
                HideInfo();

                return;
            }


            SetText(
                ammoText,
                "Ammo: " +
                playerWeapon.PistolReserveAmmo +
                " viên"
            );

            return;
        }


        // =====================================================
        // SMALL MEDKIT
        // =====================================================

        if (
            selectedInfoType ==
            InfoType.SmallMedkit
        )
        {
            if (
                playerHealth == null ||
                playerHealth.SmallMedkitCount <= 0
            )
            {
                HideInfo();

                return;
            }


            SetText(
                ammoText,
                "Heal: +" +
                playerHealth.smallHealAmount +
                " HP | x" +
                playerHealth.SmallMedkitCount
            );

            return;
        }


        // =====================================================
        // MEDIUM MEDKIT
        // =====================================================

        if (
            selectedInfoType ==
            InfoType.MediumMedkit
        )
        {
            if (
                playerHealth == null ||
                playerHealth.MediumMedkitCount <= 0
            )
            {
                HideInfo();

                return;
            }


            SetText(
                ammoText,
                "Heal: +" +
                playerHealth.mediumHealAmount +
                " HP | x" +
                playerHealth.MediumMedkitCount
            );

            return;
        }


        // =====================================================
        // LARGE MEDKIT
        // =====================================================

        if (
            selectedInfoType ==
            InfoType.LargeMedkit
        )
        {
            if (
                playerHealth == null ||
                playerHealth.LargeMedkitCount <= 0
            )
            {
                HideInfo();

                return;
            }


            SetText(
                ammoText,
                "Heal: +" +
                playerHealth.largeHealAmount +
                " HP | x" +
                playerHealth.LargeMedkitCount
            );
        }
    }


    // =========================================================
    // USE WEAPON
    // =========================================================

    private void OnUseClicked()
    {
        if (!InventoryUI.IsInventoryOpen)
            return;

        FindLocalPlayer();

        switch (selectedInfoType)
        {
            case InfoType.Weapon:
                if (playerWeapon == null ||
                    selectedWeapon == PlayerWeapon.WeaponType.None ||
                    !IsSelectedWeaponOwned())
                {
                    HideInfo();
                    return;
                }

                playerWeapon.EquipWeaponFromInventoryUI(selectedWeapon);
                HideInfo();
                return;

            case InfoType.RifleAmmo:
                if (playerWeapon != null)
                    playerWeapon.UseAmmoFromInventoryUI(
                        PlayerWeapon.WeaponType.Rifle);
                return;

            case InfoType.PistolAmmo:
                if (playerWeapon != null)
                    playerWeapon.UseAmmoFromInventoryUI(
                        PlayerWeapon.WeaponType.Pistol);
                return;

            case InfoType.SmallMedkit:
                if (playerHealth != null)
                    playerHealth.UseMedkitFromInventoryUI(1);
                return;

            case InfoType.MediumMedkit:
                if (playerHealth != null)
                    playerHealth.UseMedkitFromInventoryUI(2);
                return;

            case InfoType.LargeMedkit:
                if (playerHealth != null)
                    playerHealth.UseMedkitFromInventoryUI(3);
                return;
        }
    }


    // =========================================================
    // DROP WEAPON
    // =========================================================

    private void OnDropClicked()
    {
        if (!InventoryUI.IsInventoryOpen)
            return;

        FindLocalPlayer();

        switch (selectedInfoType)
        {
            case InfoType.Weapon:
                if (playerWeapon == null ||
                    selectedWeapon == PlayerWeapon.WeaponType.None ||
                    !IsSelectedWeaponOwned())
                {
                    HideInfo();
                    return;
                }

                playerWeapon.DropWeaponFromInventoryUI(selectedWeapon);
                HideInfo();
                return;

            case InfoType.RifleAmmo:
                if (playerWeapon != null)
                    playerWeapon.ThrowAmmoFromInventoryUI(
                        PlayerWeapon.WeaponType.Rifle);
                return;

            case InfoType.PistolAmmo:
                if (playerWeapon != null)
                    playerWeapon.ThrowAmmoFromInventoryUI(
                        PlayerWeapon.WeaponType.Pistol);
                return;

            case InfoType.SmallMedkit:
                if (playerHealth != null)
                    playerHealth.ThrowMedkitFromInventoryUI(1);
                return;

            case InfoType.MediumMedkit:
                if (playerHealth != null)
                    playerHealth.ThrowMedkitFromInventoryUI(2);
                return;

            case InfoType.LargeMedkit:
                if (playerHealth != null)
                    playerHealth.ThrowMedkitFromInventoryUI(3);
                return;
        }
    }


    // =========================================================
    // CHECK SELECTED WEAPON OWNED
    // =========================================================

    private bool IsSelectedWeaponOwned()
    {
        return IsWeaponOwned(
            selectedWeapon
        );
    }


    // =========================================================
    // CHECK WEAPON OWNED
    // =========================================================

    private bool IsWeaponOwned(
        PlayerWeapon.WeaponType weaponType)
    {
        if (playerWeapon == null)
        {
            return false;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Rifle
        )
        {
            return playerWeapon.HasRifle;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Pistol
        )
        {
            return playerWeapon.HasPistol;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Bat
        )
        {
            return playerWeapon.HasBat;
        }


        if (
            weaponType ==
            PlayerWeapon.WeaponType.Shovel
        )
        {
            return playerWeapon.HasShovel;
        }


        return false;
    }


    // =========================================================
    // UPDATE WEAPON AMMO TYPE
    // =========================================================

    private void UpdateAmmo()
    {
        if (ammoText == null)
        {
            return;
        }


        if (
            selectedWeapon ==
            PlayerWeapon.WeaponType.Rifle
        )
        {
            ammoText.text =
                "Rifle bullet";

            return;
        }


        if (
            selectedWeapon ==
            PlayerWeapon.WeaponType.Pistol
        )
        {
            ammoText.text =
                "Pistol bullet";

            return;
        }


        if (
            selectedWeapon ==
            PlayerWeapon.WeaponType.Bat
        )
        {
            ammoText.text =
                "None";

            return;
        }


        if (
            selectedWeapon ==
            PlayerWeapon.WeaponType.Shovel
        )
        {
            ammoText.text =
                "None";

            return;
        }


        ammoText.text = "";
    }


    // =========================================================
    // SHOW / HIDE WEAPON STATS
    // =========================================================

    private void SetWeaponStatsVisible(
        bool visible)
    {
        if (damageBar != null)
        {
            damageBar.gameObject.SetActive(
                visible
            );
        }


        if (fireRateBar != null)
        {
            fireRateBar.gameObject.SetActive(
                visible
            );
        }


        if (rangeBar != null)
        {
            rangeBar.gameObject.SetActive(
                visible
            );
        }
    }


    // =========================================================
    // SET WEAPON ICON
    // =========================================================

    private void SetWeaponIcon(
        Sprite icon)
    {
        // Ẩn icon item khi đang xem vũ khí
        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }


        if (weaponIcon == null)
        {
            return;
        }


        weaponIcon.sprite =
            icon;

        weaponIcon.enabled =
            icon != null;
    }


    // =========================================================
    // SET ITEM ICON
    // =========================================================

    private void SetItemIcon(
        Sprite icon)
    {
        // Ẩn icon vũ khí khi đang xem Ammo / Medkit
        if (weaponIcon != null)
        {
            weaponIcon.sprite = null;
            weaponIcon.enabled = false;
        }


        if (itemIcon == null)
        {
            return;
        }


        itemIcon.sprite =
            icon;

        itemIcon.enabled =
            icon != null;
    }


    // =========================================================
    // SET TEXT
    // =========================================================

    private void SetText(
        TMP_Text target,
        string value)
    {
        if (target != null)
        {
            target.text =
                value;
        }
    }


    // =========================================================
    // SET SLIDER
    // =========================================================

    private void SetSlider(
        Slider target,
        float value)
    {
        if (target == null)
        {
            return;
        }


        target.minValue = 0f;
        target.maxValue = 1f;

        target.value =
            Mathf.Clamp01(value);
    }


    // =========================================================
    // HIDE INFO
    // =========================================================

    public void HideInfo()
    {
        // =====================================================
        // RESET SELECTION
        // =====================================================

        selectedWeapon =
            PlayerWeapon.WeaponType.None;

        selectedInfoType =
            InfoType.None;


        // =====================================================
        // RESET TEXT
        // =====================================================

        SetText(
            weaponNameText,
            ""
        );

        SetText(
            descriptionText,
            ""
        );

        SetText(
            ammoText,
            ""
        );


        // =====================================================
        // RESET ICON
        // =====================================================

        if (weaponIcon != null)
        {
            weaponIcon.sprite = null;
            weaponIcon.enabled = false;
        }


        if (itemIcon != null)
        {
            itemIcon.sprite = null;
            itemIcon.enabled = false;
        }


        // =====================================================
        // RESET SLIDERS
        // =====================================================

        SetSlider(
            damageBar,
            0f
        );

        SetSlider(
            fireRateBar,
            0f
        );

        SetSlider(
            rangeBar,
            0f
        );


        // Chuẩn bị sẵn cho lần xem weapon tiếp theo
        SetWeaponStatsVisible(true);


        // =====================================================
        // HIDE ACTION BUTTONS
        // =====================================================

        if (actionButtons != null)
        {
            actionButtons.SetActive(false);
        }


        // =====================================================
        // HIDE PANEL
        // =====================================================

        if (infoPanel != null)
        {
            infoPanel.SetActive(false);
        }
    }
}