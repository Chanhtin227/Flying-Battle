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
    // PICKUP
    // =========================================================

    [Header("Pickup")]
    public float pickupDistance = 3f;

    public KeyCode pickupKey = KeyCode.E;


    // =========================================================
    // LOCAL PLAYER
    // =========================================================

    private PlayerWeapon localPlayerWeapon;

    private float nextSearchTime = 0f;


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // Chưa spawn
        if (Object == null)
            return;

        if (!Object.IsValid)
            return;


        // =====================================================
        // TÌM LOCAL PLAYER
        // =====================================================

        if (localPlayerWeapon == null)
        {
            if (Time.time >= nextSearchTime)
            {
                nextSearchTime = Time.time + 0.5f;

                FindLocalPlayer();
            }
        }


        if (localPlayerWeapon == null)
            return;


        // =====================================================
        // CHECK DISTANCE
        // =====================================================

        float distance = Vector3.Distance(
            transform.position,
            localPlayerWeapon.transform.position
        );


        // =====================================================
        // NHẤN E
        // =====================================================

        if (distance <= pickupDistance)
        {
            if (Input.GetKeyDown(pickupKey))
            {
                Debug.Log(
                    "[WeaponPickup] ==============================="
                );

                Debug.Log(
                    "[WeaponPickup] NHẤN E"
                );

                Debug.Log(
                    "[WeaponPickup] Weapon = " + weaponType
                );

                Debug.Log(
                    "[WeaponPickup] Distance = " + distance
                );

                Debug.Log(
                    "[WeaponPickup] Player = " +
                    localPlayerWeapon.name
                );

                Debug.Log(
                    "[WeaponPickup] PlayerRef = " +
                    localPlayerWeapon.Object.InputAuthority
                );

                Debug.Log(
                    "[WeaponPickup] Gửi RequestPickupRpc"
                );

                Debug.Log(
                    "[WeaponPickup] ==============================="
                );


                // Gửi request lên State Authority
                RequestPickupRpc(
                    localPlayerWeapon.Object.InputAuthority
                );
            }
        }
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        PlayerWeapon[] players =
            FindObjectsByType<PlayerWeapon>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        foreach (PlayerWeapon player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;


            // Chỉ lấy player local
            if (player.Object.HasInputAuthority)
            {
                localPlayerWeapon = player;

                Debug.Log(
                    "[WeaponPickup] LOCAL PLAYER = " +
                    player.name
                );

                return;
            }
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
        PlayerRef playerRef,
        RpcInfo info = default)
    {
        Debug.Log(
            "[WeaponPickup] RPC ĐÃ TỚI STATE AUTHORITY"
        );


        // =====================================================
        // CHECK STATE AUTHORITY
        // =====================================================

        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[WeaponPickup] Không phải State Authority!"
            );

            return;
        }


        // =====================================================
        // CHECK OBJECT
        // =====================================================

        if (Object == null || !Object.IsValid)
        {
            Debug.LogWarning(
                "[WeaponPickup] NetworkObject không hợp lệ!"
            );

            return;
        }


        // =====================================================
        // CHECK PLAYER REF
        // =====================================================

        if (!playerRef.IsValid)
        {
            Debug.LogWarning(
                "[WeaponPickup] PlayerRef không hợp lệ!"
            );

            return;
        }


        Debug.Log(
            "[WeaponPickup] PlayerRef = " +
            playerRef
        );


        // =====================================================
        // TÌM PLAYER
        // =====================================================

        PlayerWeapon playerWeapon =
            FindPlayerByAuthority(playerRef);


        if (playerWeapon == null)
        {
            Debug.LogWarning(
                "[WeaponPickup] KHÔNG TÌM THẤY PLAYER: " +
                playerRef
            );

            return;
        }


        Debug.Log(
            "[WeaponPickup] Tìm thấy PlayerWeapon = " +
            playerWeapon.name
        );


        // =====================================================
        // CHECK DISTANCE SERVER
        // =====================================================

        float distance = Vector3.Distance(
            transform.position,
            playerWeapon.transform.position
        );


        Debug.Log(
            "[WeaponPickup] SERVER DISTANCE = " +
            distance
        );


        if (distance > pickupDistance)
        {
            Debug.LogWarning(
                "[WeaponPickup] PLAYER QUÁ XA!"
            );

            return;
        }


        // =====================================================
        // PICKUP
        // =====================================================

        Debug.Log(
            "[WeaponPickup] Gọi ServerPickupWeapon()"
        );


        bool pickedUp =
            playerWeapon.ServerPickupWeapon(
                weaponType
            );


        // =====================================================
        // FAILED
        // =====================================================

        if (!pickedUp)
        {
            Debug.LogWarning(
                "[WeaponPickup] ServerPickupWeapon() = FALSE"
            );

            return;
        }


        // =====================================================
        // SUCCESS
        // =====================================================

        Debug.Log(
            "[WeaponPickup] ==============================="
        );

        Debug.Log(
            "[WeaponPickup] PICKUP SUCCESS"
        );

        Debug.Log(
            "[WeaponPickup] Player = " +
            playerRef
        );

        Debug.Log(
            "[WeaponPickup] Weapon = " +
            weaponType
        );

        Debug.Log(
            "[WeaponPickup] DESPAWN WEAPON"
        );

        Debug.Log(
            "[WeaponPickup] ==============================="
        );


        // =====================================================
        // DESPAWN
        // =====================================================

        Runner.Despawn(Object);
    }


    // =========================================================
    // FIND PLAYER BY AUTHORITY
    // =========================================================

    private PlayerWeapon FindPlayerByAuthority(
        PlayerRef playerRef)
    {
        PlayerWeapon[] players =
            FindObjectsByType<PlayerWeapon>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None
            );


        foreach (PlayerWeapon player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;


            if (player.Object.InputAuthority == playerRef)
            {
                return player;
            }
        }


        return null;
    }


    // =========================================================
    // TRIGGER ENTER
    // =========================================================

    private void OnTriggerEnter(Collider other)
    {
        PlayerWeapon weapon =
            other.GetComponent<PlayerWeapon>();


        if (weapon == null)
        {
            weapon =
                other.GetComponentInParent<PlayerWeapon>();
        }


        if (weapon == null)
            return;


        if (weapon.Object == null)
            return;


        if (!weapon.Object.HasInputAuthority)
            return;


        localPlayerWeapon = weapon;


        Debug.Log(
            "[WeaponPickup] PLAYER VÀO VÙNG NHẶT"
        );

        Debug.Log(
            "[WeaponPickup] Weapon = " +
            weaponType
        );

        Debug.Log(
            "[WeaponPickup] NHẤN E ĐỂ NHẶT"
        );
    }


    // =========================================================
    // TRIGGER EXIT
    // =========================================================

    private void OnTriggerExit(Collider other)
    {
        PlayerWeapon weapon =
            other.GetComponent<PlayerWeapon>();


        if (weapon == null)
        {
            weapon =
                other.GetComponentInParent<PlayerWeapon>();
        }


        if (weapon == localPlayerWeapon)
        {
            localPlayerWeapon = null;


            Debug.Log(
                "[WeaponPickup] PLAYER RỜI VÙNG NHẶT"
            );
        }
    }
}