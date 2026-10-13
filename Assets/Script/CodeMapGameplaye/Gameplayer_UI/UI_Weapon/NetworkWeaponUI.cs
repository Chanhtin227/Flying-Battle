using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class NetworkWeaponUI : MonoBehaviour
{
    // =========================================================
    // HUD SETTINGS
    // =========================================================

    [Header("HUD Settings")]

    public bool useNewHUD = true;


    // =========================================================
    // MAIN WEAPON SLOT
    // =========================================================

    [Header("Main Weapon Slot")]

    public Image mainWeaponIcon;

    public TMP_Text mainWeaponKeyText;

    public TMP_Text mainAmmoText;

    public TMP_Text mainWeaponNameText;


    // =========================================================
    // SMALL WEAPON SLOT 1
    // =========================================================

    [Header("Small Weapon Slot 1")]

    public Image smallWeaponIcon1;

    public TMP_Text smallWeaponKeyText1;

    public TMP_Text smallAmmoText1;


    // =========================================================
    // SMALL WEAPON SLOT 2
    // =========================================================

    [Header("Small Weapon Slot 2")]

    public Image smallWeaponIcon2;

    public TMP_Text smallWeaponKeyText2;

    public TMP_Text smallAmmoText2;


    // =========================================================
    // WEAPON SPRITES
    // =========================================================

    [Header("Weapon Sprites")]

    public Sprite rifleSprite;

    public Sprite pistolSprite;

    public Sprite batSprite;

    public Sprite shovelSprite;


    // =========================================================
    // WEAPON ICON SIZE
    // =========================================================

    [Header("Weapon Icon Size")]

    [Tooltip("Nếu bật, mọi vũ khí khi lên ô lớn sẽ dùng đúng Width/Height của MainWeaponIcon đã căn trong Inspector.")]
    public bool forceMainIconSameSize = true;

    [Tooltip("Nếu bật, vũ khí ở ô nhỏ sẽ dùng đúng Width/Height gốc của từng SmallWeaponIcon.")]
    public bool forceSmallIconSameSize = true;

    [Tooltip("Giữ đúng tỉ lệ hình vũ khí. Nếu muốn ảnh lấp đầy đúng cả Width và Height của ô thì tắt.")]
    public bool preserveWeaponAspect = true;


    // =========================================================
    // HEALTH UI
    // =========================================================

    [Header("Health UI")]

    public Image hpFillImage;

    public Slider hpSlider;

    public TMP_Text hpText;


    // =========================================================
    // RELOAD UI
    // =========================================================

    [Header("Reload UI")]

    public GameObject reloadUI;

    public TMP_Text reloadText;


    // =========================================================
    // OLD HUD
    // =========================================================

    [Header("Old HUD - Optional")]

    public GameObject rifleUI;

    public GameObject pistolUI;

    public GameObject batUI;

    public GameObject shovelUI;

    public TMP_Text rifleAmmoText;

    public TMP_Text pistolAmmoText;


    // =========================================================
    // LOCAL PLAYER
    // =========================================================

    private PlayerWeapon localPlayerWeapon;

    private PlayerHealth localPlayerHealth;

    private float nextSearchTime;


    // =========================================================
    // ORIGINAL ICON SIZES
    // =========================================================

    private Vector2 mainWeaponIconSize;
    private Vector2 smallWeaponIcon1Size;
    private Vector2 smallWeaponIcon2Size;

    private Vector2 mainWeaponIconPosition;
    private Vector2 smallWeaponIcon1Position;
    private Vector2 smallWeaponIcon2Position;

    private bool iconLayoutCached = false;


    // =========================================================
    // WEAPON SLOT DATA
    // =========================================================

    private struct WeaponSlotData
    {
        public PlayerWeapon.WeaponType weapon;

        public int slotNumber;

        public WeaponSlotData(
            PlayerWeapon.WeaponType weapon,
            int slotNumber)
        {
            this.weapon = weapon;

            this.slotNumber = slotNumber;
        }
    }


    private readonly List<WeaponSlotData> ownedWeapons =
        new List<WeaponSlotData>();


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        CacheIconLayout();

        HideLegacyWeaponObjects();

        HideReloadUI();

        ClearAllWeaponSlots();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // FIND LOCAL PLAYER
        // =====================================================

        if (localPlayerWeapon == null ||
            !localPlayerWeapon.HasInputAuthority)
        {
            localPlayerWeapon = null;

            localPlayerHealth = null;


            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime =
                    Time.unscaledTime + 0.5f;

                FindLocalPlayer();
            }


            if (localPlayerWeapon == null)
            {
                ClearAllWeaponSlots();

                HideReloadUI();

                return;
            }
        }


        // =====================================================
        // UPDATE HUD
        // =====================================================

        if (useNewHUD)
        {
            UpdateNewWeaponHUD();

            UpdateHealthHUD();
        }
        else
        {
            UpdateOldWeaponHUD();
        }


        UpdateReloadUI();
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        PlayerWeapon[] players =
            FindObjectsByType<PlayerWeapon>(
                FindObjectsInactive.Exclude
            );


        foreach (PlayerWeapon player in players)
        {
            if (player == null)
                continue;


            if (!player.HasInputAuthority)
                continue;


            localPlayerWeapon = player;

            localPlayerHealth =
                player.GetComponent<PlayerHealth>();


            Debug.Log(
                "[NetworkWeaponUI] Found Local Player: " +
                player.name
            );


            return;
        }
    }


    // =========================================================
    // UPDATE NEW HUD
    // =========================================================

    private void UpdateNewWeaponHUD()
    {
        if (localPlayerWeapon == null)
            return;


        // =====================================================
        // CLEAR LIST
        // =====================================================

        ownedWeapons.Clear();


        // =====================================================
        // READ NETWORK INVENTORY
        // =====================================================

        AddWeaponIfOwned(
            localPlayerWeapon.Slot1,
            1
        );

        AddWeaponIfOwned(
            localPlayerWeapon.Slot2,
            2
        );

        AddWeaponIfOwned(
            localPlayerWeapon.Slot3,
            3
        );

        AddWeaponIfOwned(
            localPlayerWeapon.Slot4,
            4
        );


        // =====================================================
        // CURRENT WEAPON
        // =====================================================

        PlayerWeapon.WeaponType currentWeapon =
            localPlayerWeapon.CurrentWeapon;


        // =====================================================
        // MAIN SLOT
        // =====================================================

        WeaponSlotData mainData =
            new WeaponSlotData(
                PlayerWeapon.WeaponType.None,
                0
            );


        foreach (WeaponSlotData data in ownedWeapons)
        {
            if (data.weapon == currentWeapon)
            {
                mainData = data;

                break;
            }
        }


        // =====================================================
        // IF CURRENT WEAPON IS NONE
        // =====================================================

        if (mainData.weapon ==
            PlayerWeapon.WeaponType.None)
        {
            ClearMainSlot();
        }
        else
        {
            SetWeaponSlot(
                mainWeaponIcon,
                mainWeaponKeyText,
                mainAmmoText,
                mainData
            );
        }


        // =====================================================
        // WEAPONS EXCEPT CURRENT
        // =====================================================

        List<WeaponSlotData> otherWeapons =
            new List<WeaponSlotData>();


        foreach (WeaponSlotData data in ownedWeapons)
        {
            if (data.weapon == currentWeapon)
                continue;


            otherWeapons.Add(data);
        }


        // =====================================================
        // SMALL SLOT 1
        // =====================================================

        if (otherWeapons.Count >= 1)
        {
            SetWeaponSlot(
                smallWeaponIcon1,
                smallWeaponKeyText1,
                smallAmmoText1,
                otherWeapons[0]
            );
        }
        else
        {
            ClearSmallSlot1();
        }


        // =====================================================
        // SMALL SLOT 2
        // =====================================================

        if (otherWeapons.Count >= 2)
        {
            SetWeaponSlot(
                smallWeaponIcon2,
                smallWeaponKeyText2,
                smallAmmoText2,
                otherWeapons[1]
            );
        }
        else
        {
            ClearSmallSlot2();
        }


        // =====================================================
        // MAIN WEAPON NAME
        // =====================================================

        if (mainWeaponNameText != null)
        {
            if (mainData.weapon ==
                PlayerWeapon.WeaponType.None)
            {
                mainWeaponNameText.text = "";
            }
            else
            {
                mainWeaponNameText.text =
                    mainData.weapon.ToString().ToUpper();
            }
        }
    }


    // =========================================================
    // ADD WEAPON IF OWNED
    // =========================================================

    private void AddWeaponIfOwned(
        PlayerWeapon.WeaponType weapon,
        int slotNumber)
    {
        if (weapon == PlayerWeapon.WeaponType.None)
            return;


        if (!HasWeapon(weapon))
            return;


        // =====================================================
        // AVOID DUPLICATES
        // =====================================================

        foreach (WeaponSlotData data in ownedWeapons)
        {
            if (data.weapon == weapon)
                return;
        }


        ownedWeapons.Add(
            new WeaponSlotData(
                weapon,
                slotNumber
            )
        );
    }


    // =========================================================
    // CHECK WEAPON OWNERSHIP
    // =========================================================

    private bool HasWeapon(
        PlayerWeapon.WeaponType weapon)
    {
        if (localPlayerWeapon == null)
            return false;


        switch (weapon)
        {
            case PlayerWeapon.WeaponType.Rifle:
                return localPlayerWeapon.HasRifle;


            case PlayerWeapon.WeaponType.Pistol:
                return localPlayerWeapon.HasPistol;


            case PlayerWeapon.WeaponType.Bat:
                return localPlayerWeapon.HasBat;


            case PlayerWeapon.WeaponType.Shovel:
                return localPlayerWeapon.HasShovel;
        }


        return false;
    }


    // =========================================================
    // SET WEAPON SLOT
    // =========================================================

    private void SetWeaponSlot(
        Image weaponIcon,
        TMP_Text keyText,
        TMP_Text ammoText,
        WeaponSlotData data)
    {
        // =====================================================
        // ICON
        // =====================================================

        SetIcon(
            weaponIcon,
            GetWeaponSprite(data.weapon)
        );


        // =====================================================
        // FIX WIDTH / HEIGHT
        //
        // Vũ khí Slot 2 hoặc Slot 3 khi được chọn sẽ hiển thị
        // bằng MainWeaponIcon, vì vậy Width/Height sẽ luôn đúng
        // bằng kích thước ô lớn đã căn trong Inspector.
        // =====================================================

        ApplyFixedIconLayout(
            weaponIcon
        );


        // =====================================================
        // SLOT KEY
        // =====================================================

        if (keyText != null)
        {
            keyText.text =
                data.slotNumber.ToString();

            keyText.gameObject.SetActive(true);
        }


        // =====================================================
        // AMMO
        // =====================================================

        if (ammoText != null)
        {
            ammoText.text =
                GetWeaponAmmoText(data.weapon);

            ammoText.gameObject.SetActive(true);
        }
    }


    // =========================================================
    // CACHE ICON LAYOUT
    // =========================================================

    private void CacheIconLayout()
    {
        if (mainWeaponIcon != null)
        {
            mainWeaponIconSize =
                mainWeaponIcon.rectTransform.sizeDelta;

            mainWeaponIconPosition =
                mainWeaponIcon.rectTransform.anchoredPosition;
        }


        if (smallWeaponIcon1 != null)
        {
            smallWeaponIcon1Size =
                smallWeaponIcon1.rectTransform.sizeDelta;

            smallWeaponIcon1Position =
                smallWeaponIcon1.rectTransform.anchoredPosition;
        }


        if (smallWeaponIcon2 != null)
        {
            smallWeaponIcon2Size =
                smallWeaponIcon2.rectTransform.sizeDelta;

            smallWeaponIcon2Position =
                smallWeaponIcon2.rectTransform.anchoredPosition;
        }


        iconLayoutCached =
            true;
    }


    // =========================================================
    // APPLY FIXED ICON LAYOUT
    // =========================================================

    private void ApplyFixedIconLayout(
        Image weaponIcon)
    {
        if (weaponIcon == null)
            return;


        if (!iconLayoutCached)
        {
            CacheIconLayout();
        }


        RectTransform rect =
            weaponIcon.rectTransform;


        // =====================================================
        // MAIN ICON
        // =====================================================

        if (weaponIcon == mainWeaponIcon)
        {
            if (forceMainIconSameSize)
            {
                rect.sizeDelta =
                    mainWeaponIconSize;
            }


            rect.anchoredPosition =
                mainWeaponIconPosition;
        }

        // =====================================================
        // SMALL ICON 1
        // =====================================================

        else if (weaponIcon == smallWeaponIcon1)
        {
            if (forceSmallIconSameSize)
            {
                rect.sizeDelta =
                    smallWeaponIcon1Size;
            }


            rect.anchoredPosition =
                smallWeaponIcon1Position;
        }

        // =====================================================
        // SMALL ICON 2
        // =====================================================

        else if (weaponIcon == smallWeaponIcon2)
        {
            if (forceSmallIconSameSize)
            {
                rect.sizeDelta =
                    smallWeaponIcon2Size;
            }


            rect.anchoredPosition =
                smallWeaponIcon2Position;
        }


        rect.localScale =
            Vector3.one;

        rect.localRotation =
            Quaternion.identity;


        weaponIcon.preserveAspect =
            preserveWeaponAspect;
    }


    // =========================================================
    // GET WEAPON SPRITE
    // =========================================================

    private Sprite GetWeaponSprite(
        PlayerWeapon.WeaponType weapon)
    {
        switch (weapon)
        {
            case PlayerWeapon.WeaponType.Rifle:
                return rifleSprite;


            case PlayerWeapon.WeaponType.Pistol:
                return pistolSprite;


            case PlayerWeapon.WeaponType.Bat:
                return batSprite;


            case PlayerWeapon.WeaponType.Shovel:
                return shovelSprite;


            default:
                return null;
        }
    }


    // =========================================================
    // GET WEAPON AMMO TEXT
    // =========================================================

    private string GetWeaponAmmoText(
        PlayerWeapon.WeaponType weapon)
    {
        if (localPlayerWeapon == null)
            return "";


        switch (weapon)
        {
            // =================================================
            // RIFLE
            // =================================================

            case PlayerWeapon.WeaponType.Rifle:

                return
                    localPlayerWeapon.RifleAmmo +
                    " / " +
                    localPlayerWeapon.RifleReserveAmmo;


            // =================================================
            // PISTOL
            // =================================================

            case PlayerWeapon.WeaponType.Pistol:

                return
                    localPlayerWeapon.PistolAmmo +
                    " / " +
                    localPlayerWeapon.PistolReserveAmmo;


            // =================================================
            // BAT
            // =================================================

            case PlayerWeapon.WeaponType.Bat:

                return "∞";


            // =================================================
            // SHOVEL
            // =================================================

            case PlayerWeapon.WeaponType.Shovel:

                return "∞";


            default:

                return "";
        }
    }


    // =========================================================
    // SET ICON
    // =========================================================

    private static void SetIcon(
        Image image,
        Sprite sprite)
    {
        if (image == null)
            return;


        image.sprite = sprite;

        image.enabled =
            sprite != null;

        // preserveAspect được xử lý trong ApplyFixedIconLayout().
    }


    // =========================================================
    // CLEAR MAIN SLOT
    // =========================================================

    private void ClearMainSlot()
    {
        SetIcon(mainWeaponIcon, null);


        if (mainWeaponKeyText != null)
        {
            mainWeaponKeyText.text = "";
        }


        if (mainAmmoText != null)
        {
            mainAmmoText.text = "";
        }


        if (mainWeaponNameText != null)
        {
            mainWeaponNameText.text = "";
        }
    }


    // =========================================================
    // CLEAR SMALL SLOT 1
    // =========================================================

    private void ClearSmallSlot1()
    {
        SetIcon(smallWeaponIcon1, null);


        if (smallWeaponKeyText1 != null)
        {
            smallWeaponKeyText1.text = "";
        }


        if (smallAmmoText1 != null)
        {
            smallAmmoText1.text = "";
        }
    }


    // =========================================================
    // CLEAR SMALL SLOT 2
    // =========================================================

    private void ClearSmallSlot2()
    {
        SetIcon(smallWeaponIcon2, null);


        if (smallWeaponKeyText2 != null)
        {
            smallWeaponKeyText2.text = "";
        }


        if (smallAmmoText2 != null)
        {
            smallAmmoText2.text = "";
        }
    }


    // =========================================================
    // CLEAR ALL
    // =========================================================

    private void ClearAllWeaponSlots()
    {
        ClearMainSlot();

        ClearSmallSlot1();

        ClearSmallSlot2();
    }


    // =========================================================
    // UPDATE HEALTH HUD
    // =========================================================

    private void UpdateHealthHUD()
    {
        if (localPlayerHealth == null)
            return;


        float maxHealth =
            Mathf.Max(
                1f,
                localPlayerHealth.maxHealth
            );


        float currentHealth =
            Mathf.Clamp(
                localPlayerHealth.CurrentHealth,
                0f,
                maxHealth
            );


        float fraction =
            currentHealth / maxHealth;


        // =====================================================
        // HP IMAGE
        // =====================================================

        if (hpFillImage != null)
        {
            hpFillImage.fillAmount =
                fraction;
        }


        // =====================================================
        // HP SLIDER
        // =====================================================

        if (hpSlider != null)
        {
            hpSlider.minValue = 0f;

            hpSlider.maxValue = 1f;

            hpSlider.value =
                fraction;
        }


        // =====================================================
        // HP TEXT
        // =====================================================

        if (hpText != null)
        {
            hpText.text =
                "HP " +
                Mathf.CeilToInt(currentHealth) +
                " / " +
                Mathf.CeilToInt(maxHealth);
        }
    }


    // =========================================================
    // UPDATE OLD WEAPON HUD
    // =========================================================

    private void UpdateOldWeaponHUD()
    {
        HideLegacyWeaponObjects();


        if (localPlayerWeapon == null)
            return;


        switch (localPlayerWeapon.CurrentWeapon)
        {
            case PlayerWeapon.WeaponType.Rifle:

                SetActive(rifleUI, true);

                if (rifleAmmoText != null)
                {
                    rifleAmmoText.gameObject.SetActive(true);

                    rifleAmmoText.text =
                        GetWeaponAmmoText(
                            PlayerWeapon.WeaponType.Rifle
                        );
                }

                break;


            case PlayerWeapon.WeaponType.Pistol:

                SetActive(pistolUI, true);

                if (pistolAmmoText != null)
                {
                    pistolAmmoText.gameObject.SetActive(true);

                    pistolAmmoText.text =
                        GetWeaponAmmoText(
                            PlayerWeapon.WeaponType.Pistol
                        );
                }

                break;


            case PlayerWeapon.WeaponType.Bat:

                SetActive(batUI, true);

                break;


            case PlayerWeapon.WeaponType.Shovel:

                SetActive(shovelUI, true);

                break;
        }
    }


    // =========================================================
    // HIDE OLD HUD
    // =========================================================

    private void HideLegacyWeaponObjects()
    {
        SetActive(rifleUI, false);

        SetActive(pistolUI, false);

        SetActive(batUI, false);

        SetActive(shovelUI, false);


        if (rifleAmmoText != null)
        {
            rifleAmmoText.gameObject.SetActive(false);
        }


        if (pistolAmmoText != null)
        {
            pistolAmmoText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // UPDATE RELOAD UI
    // =========================================================

    private void UpdateReloadUI()
    {
        if (localPlayerWeapon != null &&
            localPlayerWeapon.IsReloading)
        {
            ShowReloadUI();
        }
        else
        {
            HideReloadUI();
        }
    }


    // =========================================================
    // SHOW RELOAD
    // =========================================================

    private void ShowReloadUI()
    {
        SetActive(reloadUI, true);


        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(true);

            reloadText.text =
                "RELOADING...";
        }
    }


    // =========================================================
    // HIDE RELOAD
    // =========================================================

    private void HideReloadUI()
    {
        SetActive(reloadUI, false);


        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // SET ACTIVE
    // =========================================================

    private static void SetActive(
        GameObject obj,
        bool active)
    {
        if (obj == null)
            return;


        if (obj.activeSelf != active)
        {
            obj.SetActive(active);
        }
    }
}