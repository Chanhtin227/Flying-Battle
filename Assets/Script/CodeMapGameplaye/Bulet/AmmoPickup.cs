using UnityEngine;
using Fusion;

public class AmmoPickup : NetworkBehaviour
{
    public enum AmmoType
    {
        Rifle,
        Pistol
    }


    [Header("Ammo Type")]
    public AmmoType ammoType =
        AmmoType.Rifle;


    [Header("Ammo Amount")]
    public int ammoAmount = 30;


    [Header("Pickup")]
    public KeyCode pickupKey =
        KeyCode.F;

    public float pickupDistance =
        3f;


    [Networked]
    public AmmoType NetworkAmmoType
    {
        get;
        set;
    }


    [Networked]
    public int NetworkAmmoAmount
    {
        get;
        set;
    }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        if (HasStateAuthority)
        {
            NetworkAmmoType =
                ammoType;


            NetworkAmmoAmount =
                Mathf.Max(
                    1,
                    ammoAmount
                );
        }


        ammoType =
            NetworkAmmoType;


        ammoAmount =
            NetworkAmmoAmount;
    }


    // =========================================================
    // RENDER
    // =========================================================

    public override void Render()
    {
        ammoType =
            NetworkAmmoType;


        ammoAmount =
            NetworkAmmoAmount;
    }


    // =========================================================
    // SETUP DROPPED AMMO
    // =========================================================

    public void SetupDroppedAmmo(
        AmmoType type,
        int amount)
    {
        if (!HasStateAuthority)
            return;


        NetworkAmmoType =
            type;


        NetworkAmmoAmount =
            Mathf.Max(
                1,
                amount
            );


        ammoType =
            NetworkAmmoType;


        ammoAmount =
            NetworkAmmoAmount;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (
            PlayerMovement.LocalPlayer ==
            null
        )
        {
            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                PlayerMovement.LocalPlayer
                    .transform.position
            );


        if (
            distance >
            pickupDistance
        )
        {
            return;
        }


        if (
            !Input.GetKeyDown(
                pickupKey
            )
        )
        {
            return;
        }


        NetworkObject playerObject =
            PlayerMovement.LocalPlayer
                .GetComponent<NetworkObject>();


        if (playerObject == null)
        {
            Debug.LogError(
                "[AmmoPickup] " +
                "PLAYER KHÔNG CÓ NETWORKOBJECT!"
            );

            return;
        }


        RequestPickupRpc(
            playerObject
        );
    }


    // =========================================================
    // PICKUP RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RequestPickupRpc(
        NetworkObject playerObject,
        RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;


        if (
            Object == null ||
            !Object.IsValid
        )
        {
            return;
        }


        if (
            playerObject == null ||
            !playerObject.IsValid
        )
        {
            return;
        }


        PlayerWeapon playerWeapon =
            playerObject
                .GetComponent<PlayerWeapon>();


        if (playerWeapon == null)
        {
            Debug.LogError(
                "[AmmoPickup] " +
                "PLAYER KHÔNG CÓ PLAYERWEAPON!"
            );

            return;
        }


        float distance =
            Vector3.Distance(
                transform.position,
                playerObject.transform.position
            );


        if (
            distance >
            pickupDistance
        )
        {
            return;
        }


        if (
            NetworkAmmoAmount <= 0
        )
        {
            return;
        }


        // =====================================================
        // ADD AMMO
        // =====================================================

        if (
            NetworkAmmoType ==
            AmmoType.Rifle
        )
        {
            playerWeapon.AddRifleAmmo(
                NetworkAmmoAmount
            );
        }
        else
        {
            playerWeapon.AddPistolAmmo(
                NetworkAmmoAmount
            );
        }


        // =====================================================
        // PLAY SOUND TRÊN PLAYER
        // =====================================================

        playerWeapon
            .ServerPlayItemPickupSound();


        // =====================================================
        // DESPAWN
        // =====================================================

        Runner.Despawn(
            Object
        );
    }
}