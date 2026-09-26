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
    public AmmoType ammoType = AmmoType.Rifle;

    [Header("Ammo Amount")]
    public int ammoAmount = 30;

    [Header("Pickup")]
    public KeyCode pickupKey = KeyCode.F;
    public float pickupDistance = 3f;

    private void Update()
    {
        // Chỉ player local được phép nhận input
        if (PlayerMovement.LocalPlayer == null)
            return;

        float distance = Vector3.Distance(
            transform.position,
            PlayerMovement.LocalPlayer.transform.position
        );

        if (distance > pickupDistance)
            return;

        if (Input.GetKeyDown(pickupKey))
        {
            NetworkObject playerObject =
                PlayerMovement.LocalPlayer.GetComponent<NetworkObject>();

            if (playerObject == null)
            {
                Debug.LogError(
                    "[AmmoPickup] PLAYER KHÔNG CÓ NETWORKOBJECT!"
                );

                return;
            }

            Debug.Log(
                "[AmmoPickup] NHẤN F - " +
                "Ammo: " + ammoType +
                " +" + ammoAmount
            );

            RequestPickupRpc(playerObject);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestPickupRpc(
        NetworkObject playerObject,
        RpcInfo info = default)
    {
        if (!HasStateAuthority)
            return;

        if (Object == null || !Object.IsValid)
            return;

        if (playerObject == null || !playerObject.IsValid)
            return;

        PlayerWeapon playerWeapon =
            playerObject.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
        {
            Debug.LogError(
                "[AmmoPickup] PLAYER KHÔNG CÓ PLAYERWEAPON!"
            );

            return;
        }

        // Kiểm tra khoảng cách lại ở Host/State Authority
        float distance = Vector3.Distance(
            transform.position,
            playerObject.transform.position
        );

        if (distance > pickupDistance)
        {
            Debug.LogWarning(
                "[AmmoPickup] PLAYER QUÁ XA: " +
                distance
            );

            return;
        }

        // ==============================
        // CỘNG ĐẠN
        // ==============================

        if (ammoType == AmmoType.Rifle)
        {
            playerWeapon.AddRifleAmmo(ammoAmount);
        }
        else if (ammoType == AmmoType.Pistol)
        {
            playerWeapon.AddPistolAmmo(ammoAmount);
        }

        Debug.Log(
            "====================================\n" +
            "NHẶT ĐẠN THÀNH CÔNG\n" +
            "Player: " + playerObject.InputAuthority +
            "\nAmmo Type: " + ammoType +
            "\nAmount: +" + ammoAmount +
            "\n===================================="
        );

        // Xóa hộp đạn khỏi map
        Runner.Despawn(Object);
    }
}