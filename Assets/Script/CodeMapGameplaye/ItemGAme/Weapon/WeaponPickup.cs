using UnityEngine;
using Fusion;

public class WeaponPickup : NetworkBehaviour
{
    // =========================================================
    // WEAPON
    // =========================================================

    [Header("Weapon")]
    public PlayerWeapon.WeaponType weaponType;


    // =========================================================
    // WORLD PICKUP
    // =========================================================

    [Header("World Pickup")]

    [Tooltip(
        "Bật nếu đây là prefab vũ khí nằm ngoài map / dưới đất " +
        "và Player được phép nhặt bằng F."
    )]
    public bool startAsWorldPickup = true;


    [Networked]
    public NetworkBool IsWorldPickup { get; set; }


    // =========================================================
    // DROPPED AMMO
    // =========================================================

    [Header("Dropped Ammo")]

    [Networked]
    public int DroppedRifleAmmo { get; set; }

    [Networked]
    public int DroppedRifleReserveAmmo { get; set; }

    [Networked]
    public int DroppedPistolAmmo { get; set; }

    [Networked]
    public int DroppedPistolReserveAmmo { get; set; }


    // =========================================================
    // PICKUP SETTINGS
    // =========================================================

    [Header("Pickup")]

    public float pickupDistance = 3f;


    // =========================================================
    // PRIVATE
    // =========================================================

    private bool pickupConsumed;


    // =========================================================
    // DISABLE ROTATION ON HELD WEAPONS
    // =========================================================

    private void Awake()
    {
        DisableEffectWhenHeld();
    }

    private void OnEnable()
    {
        DisableEffectWhenHeld();
    }

    private void OnTransformParentChanged()
    {
        DisableEffectWhenHeld();
    }

    private void DisableEffectWhenHeld()
    {
        // Chỉ tắt hiệu ứng khi vũ khí thuộc hierarchy Player.
        // Vũ khí nằm dưới đất vẫn được xoay bình thường.
        if (GetComponentInParent<PlayerWeapon>() == null)
            return;

        WeaponPickupEffect[] effects =
            GetComponentsInChildren<WeaponPickupEffect>(true);

        foreach (WeaponPickupEffect effect in effects)
        {
            if (effect != null)
                effect.enabled = false;
        }
    }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        pickupConsumed = false;
        // Chạy ở cả Host và Client để model trên tay không xoay.
        DisableEffectWhenHeld();


        if (!HasStateAuthority)
            return;


        // =====================================================
        // KIỂM TRA CÓ ĐANG NẰM TRONG PLAYER KHÔNG
        // =====================================================

        PlayerWeapon ownerPlayer =
            GetComponentInParent<PlayerWeapon>();


        if (ownerPlayer != null)
        {
            // Weapon này đang nằm trong hierarchy của Player.
            // Ví dụ:
            //
            // Player
            // └── Rifle
            //     └── WeaponPickup
            //
            // => TUYỆT ĐỐI KHÔNG CHO NHẶT.

            IsWorldPickup = false;


            Debug.Log(
                "[WeaponPickup] Weapon nằm trên Player -> " +
                "WorldPickup = FALSE | Weapon = " +
                weaponType
            );
        }
        else
        {
            // Weapon nằm ngoài map.
            IsWorldPickup = startAsWorldPickup;


            Debug.Log(
                "[WeaponPickup] World Weapon Spawned | Weapon = " +
                weaponType +
                " | WorldPickup = " +
                IsWorldPickup
            );
        }


        // =====================================================
        // DEFAULT AMMO
        // =====================================================

        DroppedRifleAmmo = -1;
        DroppedRifleReserveAmmo = -1;

        DroppedPistolAmmo = -1;
        DroppedPistolReserveAmmo = -1;
    }


    // =========================================================
    // ENABLE WORLD PICKUP
    // =========================================================

    public void EnableWorldPickup()
    {
        if (!HasStateAuthority)
            return;


        // =====================================================
        // KHÔNG CHO WEAPON TRÊN PLAYER THÀNH WORLD PICKUP
        // =====================================================

        PlayerWeapon ownerPlayer =
            GetComponentInParent<PlayerWeapon>();


        if (ownerPlayer != null)
        {
            IsWorldPickup = false;


            Debug.LogWarning(
                "[WeaponPickup] Không thể EnableWorldPickup! " +
                "Weapon này đang nằm trong Player."
            );


            return;
        }


        IsWorldPickup = true;


        Debug.Log(
            "[WeaponPickup] Enable World Pickup | Weapon = " +
            weaponType
        );
    }


    // =========================================================
    // DISABLE WORLD PICKUP
    // =========================================================

    public void DisableWorldPickup()
    {
        if (!HasStateAuthority)
            return;


        IsWorldPickup = false;


        Debug.Log(
            "[WeaponPickup] Disable World Pickup | Weapon = " +
            weaponType
        );
    }


    // =========================================================
    // SET DROPPED WEAPON
    // =========================================================

    public void SetupDroppedWeapon(
        PlayerWeapon.WeaponType droppedWeaponType,
        int rifleAmmo,
        int rifleReserveAmmo,
        int pistolAmmo,
        int pistolReserveAmmo
    )
    {
        if (!HasStateAuthority)
            return;


        // =====================================================
        // AN TOÀN:
        // DROP OBJECT KHÔNG ĐƯỢC NẰM TRONG PLAYER
        // =====================================================

        PlayerWeapon ownerPlayer =
            GetComponentInParent<PlayerWeapon>();


        if (ownerPlayer != null)
        {
            IsWorldPickup = false;


            Debug.LogError(
                "[WeaponPickup] SetupDroppedWeapon FAILED! " +
                "Drop prefab đang nằm trong hierarchy Player."
            );


            return;
        }


        // =====================================================
        // WEAPON TYPE
        // =====================================================

        weaponType =
            droppedWeaponType;


        // =====================================================
        // SAVE AMMO
        // =====================================================

        DroppedRifleAmmo =
            rifleAmmo;

        DroppedRifleReserveAmmo =
            rifleReserveAmmo;

        DroppedPistolAmmo =
            pistolAmmo;

        DroppedPistolReserveAmmo =
            pistolReserveAmmo;


        // =====================================================
        // ĐÃ VỨT XUỐNG ĐẤT
        // => CHO PHÉP NHẶT
        // =====================================================

        IsWorldPickup = true;


        Debug.Log(
            "[WeaponPickup] DROPPED -> WORLD PICKUP | Weapon = " +
            weaponType
        );
    }


    // =========================================================
    // TRY PICKUP
    // =========================================================

    public void TryPickupFrom(
        PlayerWeapon playerWeapon
    )
    {
        // =====================================================
        // ONLY STATE AUTHORITY
        // =====================================================

        if (!HasStateAuthority)
        {
            return;
        }


        // =====================================================
        // ALREADY PICKED
        // =====================================================

        if (pickupConsumed)
        {
            return;
        }


        // =====================================================
        // VALIDATE NETWORK OBJECT
        // =====================================================

        if (
            Object == null ||
            !Object.IsValid
        )
        {
            return;
        }


        // =====================================================
        // VALIDATE PLAYER
        // =====================================================

        if (playerWeapon == null)
        {
            return;
        }


        if (
            playerWeapon.Object == null ||
            !playerWeapon.Object.IsValid
        )
        {
            return;
        }


        // =====================================================
        // QUAN TRỌNG NHẤT
        //
        // NẾU PICKUP ĐANG NẰM TRONG PLAYER
        // => KHÔNG BAO GIỜ CHO NHẶT
        // =====================================================

        PlayerWeapon ownerPlayer =
            GetComponentInParent<PlayerWeapon>();


        if (ownerPlayer != null)
        {
            Debug.LogWarning(
                "[WeaponPickup] PICKUP BLOCKED! " +
                "Không thể lấy vũ khí trực tiếp từ Player khác."
            );


            return;
        }


        // =====================================================
        // WORLD PICKUP CHECK
        // =====================================================

        if (!IsWorldPickup)
        {
            Debug.LogWarning(
                "[WeaponPickup] KHÔNG THỂ NHẶT | " +
                weaponType +
                " | IsWorldPickup = FALSE"
            );


            return;
        }


        // =====================================================
        // WEAPON TYPE CHECK
        // =====================================================

        if (
            weaponType ==
            PlayerWeapon.WeaponType.None
        )
        {
            Debug.LogWarning(
                "[WeaponPickup] KHÔNG THỂ NHẶT | " +
                "Weapon Type = None"
            );


            return;
        }


        // =====================================================
        // DISTANCE CHECK
        // Chỉ kiểm tra X/Z
        // =====================================================

        Vector2 pickupPosition =
            new Vector2(
                transform.position.x,
                transform.position.z
            );


        Vector2 playerPosition =
            new Vector2(
                playerWeapon.transform.position.x,
                playerWeapon.transform.position.z
            );


        float distance =
            Vector2.Distance(
                pickupPosition,
                playerPosition
            );


        if (distance > pickupDistance)
        {
            Debug.LogWarning(
                "[WeaponPickup] QUÁ XA | Distance = " +
                distance +
                " | Max = " +
                pickupDistance
            );


            return;
        }


        // =====================================================
        // ADD WEAPON TO PLAYER
        // =====================================================

        bool success =
            playerWeapon.ServerPickupWeapon(
                weaponType,
                DroppedRifleAmmo,
                DroppedRifleReserveAmmo,
                DroppedPistolAmmo,
                DroppedPistolReserveAmmo
            );


        // =====================================================
        // FAILED
        // =====================================================

        if (!success)
        {
            Debug.LogWarning(
                "[WeaponPickup] ServerPickupWeapon FAILED | " +
                weaponType
            );


            return;
        }


        // =====================================================
        // SUCCESS
        // =====================================================

        pickupConsumed = true;


        Debug.Log(
            "====================================\n" +
            "WEAPON PICKUP SUCCESS\n" +
            "Weapon: " +
            weaponType +
            "\nDistance: " +
            distance +
            "\n===================================="
        );


        // =====================================================
        // DESPAWN
        // =====================================================

        if (
            Object != null &&
            Object.IsValid
        )
        {
            Runner.Despawn(
                Object
            );
        }
    }
}