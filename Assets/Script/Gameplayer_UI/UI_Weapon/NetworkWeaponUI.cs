using UnityEngine;
using TMPro;
using Fusion;

public class NetworkWeaponUI : MonoBehaviour
{
    // =========================================================
    // WEAPON UI
    // =========================================================

    [Header("Weapon UI")]

    public GameObject rifleUI;
    public GameObject pistolUI;
    public GameObject batUI;
    public GameObject shovelUI;


    // =========================================================
    // AMMO TEXT
    // =========================================================

    [Header("Ammo Text")]

    public TMP_Text rifleAmmoText;
    public TMP_Text pistolAmmoText;


    // =========================================================
    // RELOAD UI
    // =========================================================

    [Header("Reload UI")]

    public GameObject reloadUI;
    public TMP_Text reloadText;


    // =========================================================
    // LOCAL PLAYER
    // =========================================================

    private PlayerWeapon localPlayerWeapon;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        HideAllWeaponUI();
        HideReloadUI();
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        Debug.Log(
            "========== NETWORK WEAPON UI START =========="
        );

        InvokeRepeating(
            nameof(FindLocalPlayer),
            0.2f,
            0.5f
        );
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (localPlayerWeapon == null)
            return;


        // =====================================================
        // UPDATE WEAPON UI
        // =====================================================

        UpdateWeaponUI();


        // =====================================================
        // UPDATE RELOAD UI
        // =====================================================

        UpdateReloadUI();
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        // Đã tìm thấy rồi thì không tìm lại
        if (localPlayerWeapon != null)
            return;


        PlayerWeapon[] players =
            FindObjectsByType<PlayerWeapon>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        Debug.Log(
            "NetworkWeaponUI: tìm thấy PlayerWeapon = "
            + players.Length
        );


        foreach (PlayerWeapon player in players)
        {
            if (player == null)
                continue;


            Debug.Log(
                "Player: "
                + player.name
                + " | InputAuthority = "
                + player.HasInputAuthority
            );


            // =================================================
            // CHỈ LẤY PLAYER LOCAL
            // =================================================

            if (player.HasInputAuthority)
            {
                localPlayerWeapon = player;


                Debug.Log(
                    ">>> ĐÃ TÌM THẤY LOCAL PLAYER: "
                    + player.name
                );


                // Cập nhật ngay
                UpdateWeaponUI();
                UpdateReloadUI();


                // Không cần tìm nữa
                CancelInvoke(
                    nameof(FindLocalPlayer)
                );


                return;
            }
        }
    }


    // =========================================================
    // UPDATE WEAPON UI
    // =========================================================

    private void UpdateWeaponUI()
    {
        if (localPlayerWeapon == null)
            return;


        // =====================================================
        // ĐẦU TIÊN ẨN TẤT CẢ
        // =====================================================

        HideAllWeaponUI();


        // =====================================================
        // KIỂM TRA CURRENT WEAPON
        // =====================================================

        switch (localPlayerWeapon.CurrentWeapon)
        {
            // =================================================
            // NONE
            // =================================================

            case PlayerWeapon.WeaponType.None:

                // Không cầm gì
                // Không hiện bất kỳ UI vũ khí nào

                break;


            // =================================================
            // RIFLE
            // =================================================

            case PlayerWeapon.WeaponType.Rifle:

                if (rifleUI != null)
                {
                    rifleUI.SetActive(true);
                }


                if (rifleAmmoText != null)
                {
                    rifleAmmoText.gameObject.SetActive(true);

                    rifleAmmoText.text =
                        localPlayerWeapon.RifleAmmo
                        + " / "
                        + localPlayerWeapon.RifleReserveAmmo;
                }

                break;


            // =================================================
            // PISTOL
            // =================================================

            case PlayerWeapon.WeaponType.Pistol:

                if (pistolUI != null)
                {
                    pistolUI.SetActive(true);
                }


                if (pistolAmmoText != null)
                {
                    pistolAmmoText.gameObject.SetActive(true);

                    pistolAmmoText.text =
                        localPlayerWeapon.PistolAmmo
                        + " / "
                        + localPlayerWeapon.PistolReserveAmmo;
                }

                break;


            // =================================================
            // BAT
            // =================================================

            case PlayerWeapon.WeaponType.Bat:

                if (batUI != null)
                {
                    batUI.SetActive(true);
                }

                break;


            // =================================================
            // SHOVEL
            // =================================================

            case PlayerWeapon.WeaponType.Shovel:

                if (shovelUI != null)
                {
                    shovelUI.SetActive(true);
                }

                break;
        }
    }


    // =========================================================
    // UPDATE RELOAD UI
    // =========================================================

    private void UpdateReloadUI()
    {
        if (localPlayerWeapon == null)
            return;


        // =====================================================
        // ĐANG RELOAD
        // =====================================================

        if (localPlayerWeapon.IsReloading)
        {
            ShowReloadUI();
        }

        // =====================================================
        // KHÔNG RELOAD
        // =====================================================

        else
        {
            HideReloadUI();
        }
    }


    // =========================================================
    // SHOW RELOAD UI
    // =========================================================

    private void ShowReloadUI()
    {
        if (reloadUI != null)
        {
            reloadUI.SetActive(true);
        }


        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(true);

            reloadText.text = "Đang nạp...";
        }
    }


    // =========================================================
    // HIDE RELOAD UI
    // =========================================================

    private void HideReloadUI()
    {
        if (reloadUI != null)
        {
            reloadUI.SetActive(false);
        }


        if (reloadText != null)
        {
            reloadText.gameObject.SetActive(false);
        }
    }


    // =========================================================
    // HIDE ALL WEAPON UI
    // =========================================================

    private void HideAllWeaponUI()
    {
        // =====================================================
        // ICON RIFLE
        // =====================================================

        if (rifleUI != null)
        {
            rifleUI.SetActive(false);
        }


        // =====================================================
        // ICON PISTOL
        // =====================================================

        if (pistolUI != null)
        {
            pistolUI.SetActive(false);
        }


        // =====================================================
        // ICON BAT
        // =====================================================

        if (batUI != null)
        {
            batUI.SetActive(false);
        }


        // =====================================================
        // ICON SHOVEL
        // =====================================================

        if (shovelUI != null)
        {
            shovelUI.SetActive(false);
        }


        // =====================================================
        // RIFLE AMMO
        // =====================================================

        if (rifleAmmoText != null)
        {
            rifleAmmoText.gameObject.SetActive(false);
            rifleAmmoText.text = "";
        }


        // =====================================================
        // PISTOL AMMO
        // =====================================================

        if (pistolAmmoText != null)
        {
            pistolAmmoText.gameObject.SetActive(false);
            pistolAmmoText.text = "";
        }
    }
}