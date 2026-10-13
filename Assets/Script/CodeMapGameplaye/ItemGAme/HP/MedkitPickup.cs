using UnityEngine;
using Fusion;

public class MedkitPickup : NetworkBehaviour
{
    public enum MedkitType
    {
        Small,
        Medium,
        Large
    }


    [Header("Medkit Type")]
    public MedkitType medkitType =
        MedkitType.Small;


    [Header("Heal Amount")]
    public float smallHealAmount =
        50f;

    public float mediumHealAmount =
        75f;

    public float largeHealAmount =
        100f;


    [Header("Pickup")]
    public KeyCode pickupKey =
        KeyCode.F;

    public float pickupDistance =
        3f;


    [Networked]
    public MedkitType NetworkMedkitType
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
            NetworkMedkitType =
                medkitType;
        }


        medkitType =
            NetworkMedkitType;
    }


    // =========================================================
    // RENDER
    // =========================================================

    public override void Render()
    {
        medkitType =
            NetworkMedkitType;
    }


    // =========================================================
    // SETUP DROPPED MEDKIT
    // =========================================================

    public void SetupDroppedMedkit(
        MedkitType type)
    {
        if (!HasStateAuthority)
            return;


        NetworkMedkitType =
            type;


        medkitType =
            type;
    }


    // =========================================================
    // GET HEAL AMOUNT
    // =========================================================

    private float GetHealAmount()
    {
        switch (
            NetworkMedkitType
        )
        {
            case MedkitType.Small:

                return
                    smallHealAmount;


            case MedkitType.Medium:

                return
                    mediumHealAmount;


            case MedkitType.Large:

                return
                    largeHealAmount;
        }


        return 50f;
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
                "[MedkitPickup] " +
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


        PlayerHealth health =
            playerObject
                .GetComponent<PlayerHealth>();


        if (health == null)
        {
            Debug.LogError(
                "[MedkitPickup] " +
                "PLAYER KHÔNG CÓ PLAYERHEALTH!"
            );

            return;
        }


        PlayerWeapon playerWeapon =
            playerObject
                .GetComponent<PlayerWeapon>();


        if (playerWeapon == null)
        {
            Debug.LogError(
                "[MedkitPickup] " +
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


        // =====================================================
        // ADD MEDKIT
        // =====================================================

        float healAmount =
            GetHealAmount();


        health.AddMedkit(
            NetworkMedkitType,
            healAmount
        );


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