using UnityEngine;
using Fusion;

public class WeaponPickup : NetworkBehaviour
{
    [Header("Weapon")]
    public PlayerWeapon.WeaponType weaponType;

    [Header("Pickup")]
    public float pickupDistance = 3f;

    private bool pickupConsumed;

    public override void Spawned()
    {
        pickupConsumed = false;
    }

    public void TryPickupFrom(PlayerWeapon playerWeapon)
    {
        if (!HasStateAuthority || pickupConsumed || Object == null || !Object.IsValid || playerWeapon == null || playerWeapon.Object == null || !playerWeapon.Object.IsValid)
            return;

        Vector2 objectPos = new Vector2(transform.position.x, transform.position.z);
        Vector2 playerPos = new Vector2(playerWeapon.transform.position.x, playerWeapon.transform.position.z);
        float distance = Vector2.Distance(objectPos, playerPos);

        if (distance > pickupDistance) return;

        if (!playerWeapon.ServerPickupWeapon(weaponType)) return;

        pickupConsumed = true;
        Runner.Despawn(Object);
    }
}