using UnityEngine;
using Fusion;

public class MedkitPickup : NetworkBehaviour
{
    // =========================================================
    // MEDKIT TYPE
    // =========================================================

    public enum MedkitType
    {
        Small,
        Medium,
        Large
    }


    // =========================================================
    // MEDKIT SETTINGS
    // =========================================================

    [Header("Medkit Type")]

    public MedkitType medkitType = MedkitType.Small;


    // =========================================================
    // HEAL AMOUNT
    // =========================================================

    [Header("Heal Amount")]

    public float smallHealAmount = 50f;
    public float mediumHealAmount = 75f;
    public float largeHealAmount = 100f;


    // =========================================================
    // PICKUP
    // =========================================================

    [Header("Pickup")]

    public KeyCode pickupKey = KeyCode.F;

    public float pickupDistance = 3f;


    // =========================================================
    // GET HEAL AMOUNT
    // =========================================================

    private float GetHealAmount()
    {
        switch (medkitType)
        {
            case MedkitType.Small:
                return smallHealAmount;

            case MedkitType.Medium:
                return mediumHealAmount;

            case MedkitType.Large:
                return largeHealAmount;
        }

        return 50f;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Không có player local
        if (PlayerMovement.LocalPlayer == null)
            return;


        // Khoảng cách tới player
        float distance = Vector3.Distance(
            transform.position,
            PlayerMovement.LocalPlayer.transform.position
        );


        // Quá xa
        if (distance > pickupDistance)
            return;


        // Nhấn F
        if (Input.GetKeyDown(pickupKey))
        {
            NetworkObject playerObject =
                PlayerMovement.LocalPlayer.GetComponent<NetworkObject>();


            if (playerObject == null)
            {
                Debug.LogError(
                    "[MedkitPickup] PLAYER KHÔNG CÓ NETWORKOBJECT!"
                );

                return;
            }


            Debug.Log(
                "[MedkitPickup] NHẤN F - " +
                "Type: " +
                medkitType
            );


            RequestPickupRpc(playerObject);
        }
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
        // Chỉ State Authority xử lý
        if (!HasStateAuthority)
            return;


        // Kiểm tra medkit
        if (Object == null)
            return;


        // Kiểm tra player
        if (playerObject == null)
            return;


        // Lấy PlayerHealth
        PlayerHealth health =
            playerObject.GetComponent<PlayerHealth>();


        if (health == null)
        {
            Debug.LogError(
                "[MedkitPickup] PLAYER KHÔNG CÓ PLAYERHEALTH!"
            );

            return;
        }


        // =====================================================
        // KIỂM TRA KHOẢNG CÁCH LẠI TRÊN SERVER
        // =====================================================

        float distance = Vector3.Distance(
            transform.position,
            playerObject.transform.position
        );


        if (distance > pickupDistance)
        {
            Debug.LogWarning(
                "[MedkitPickup] PLAYER QUÁ XA: " +
                distance
            );

            return;
        }


        // =====================================================
        // LẤY HEAL
        // =====================================================

        float healAmount = GetHealAmount();


        // =====================================================
        // THÊM VÀO INVENTORY
        // =====================================================

        health.AddMedkit(
            medkitType,
            healAmount
        );


        // =====================================================
        // LOG
        // =====================================================

        Debug.Log(
            "====================================\n" +
            "[MedkitPickup] PICKUP THÀNH CÔNG\n" +
            "Player: " +
            playerObject.InputAuthority +
            "\nType: " +
            medkitType +
            "\nHeal: " +
            healAmount +
            "\n===================================="
        );


        // =====================================================
        // DESPAWN
        // =====================================================

        Runner.Despawn(Object);
    }
}