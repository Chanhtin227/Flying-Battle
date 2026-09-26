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
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        pickupConsumed = false;


        // Nếu là weapon spawn từ Chest / prefab bình thường
        // thì ammo mặc định sẽ là -1.
        //
        // Nếu là weapon được Drop từ Player,
        // PlayerWeapon sẽ gọi SetupDroppedWeapon()
        // ngay sau khi Spawn.
        if (HasStateAuthority)
        {
            DroppedRifleAmmo = -1;
            DroppedRifleReserveAmmo = -1;

            DroppedPistolAmmo = -1;
            DroppedPistolReserveAmmo = -1;
        }
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
        // Chỉ State Authority được ghi Networked State
        if (!HasStateAuthority)
            return;


        // =====================================================
        // WEAPON TYPE
        // =====================================================

        weaponType =
            droppedWeaponType;


        // =====================================================
        // AMMO
        // =====================================================

        DroppedRifleAmmo =
            rifleAmmo;

        DroppedRifleReserveAmmo =
            rifleReserveAmmo;

        DroppedPistolAmmo =
            pistolAmmo;

        DroppedPistolReserveAmmo =
            pistolReserveAmmo;
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
            return;


        // =====================================================
        // VALIDATE
        // =====================================================

        if (pickupConsumed)
            return;


        if (Object == null ||
            !Object.IsValid)
        {
            return;
        }


        if (playerWeapon == null)
            return;


        if (playerWeapon.Object == null ||
            !playerWeapon.Object.IsValid)
        {
            return;
        }


        // =====================================================
        // CHECK WEAPON
        // =====================================================

        if (weaponType == PlayerWeapon.WeaponType.None)
        {
            Debug.LogWarning(
                "[WeaponPickup] Weapon Type = None!"
            );

            return;
        }


        // =====================================================
        // CHECK DISTANCE
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
            return;


        // =====================================================
        // TRY ADD TO PLAYER INVENTORY
        // =====================================================

        bool success =
            playerWeapon.ServerPickupWeapon(
                weaponType,

                DroppedRifleAmmo,
                DroppedRifleReserveAmmo,

                DroppedPistolAmmo,
                DroppedPistolReserveAmmo
            );


        // Không đủ slot / đã có súng / reload...
        if (!success)
        {
            return;
        }


        // =====================================================
        // CONSUME
        // =====================================================

        pickupConsumed = true;


        // =====================================================
        // DESPAWN
        // =====================================================

        Runner.Despawn(
            Object
        );


        Debug.Log(
            "====================================\n" +
            "WEAPON PICKUP SUCCESS\n" +
            "Weapon: " +
            weaponType +
            "\n===================================="
        );
    }
}