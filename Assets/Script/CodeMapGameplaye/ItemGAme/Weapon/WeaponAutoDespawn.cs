using UnityEngine;
using Fusion;

// Chi gan script nay vao prefab vat pham NAM TREN MAT DAT.
// Tuyet doi khong gan vao ruong hoac Player prefab.
public class WeaponAutoDespawn : NetworkBehaviour
{
    [Header("Auto Despawn Settings")]
    [Tooltip("So giay vat pham ton tai sau khi duoc spawn")]
    [Min(0.1f)]
    public float despawnTime = 5f;

    [Networked]
    private TickTimer DespawnTimer { get; set; }

    private bool IsGroundPickupObject()
    {
        // Chest chua mo, player va cac object khac khong duoc tu xoa.
        if (GetComponent<WeaponChest>() != null) return false;
        if (GetComponentInParent<PlayerWeapon>() != null) return false;
        if (GetComponent<WeaponPickup>() == null) return false;
        if (Object == null || !Object.IsValid) return false;
        return Object.gameObject == gameObject;
    }

    public override void Spawned()
    {
        if (!HasStateAuthority) return;

        // Timer bat dau khi world weapon duoc Runner.Spawn.
        // WeaponChest chi spawn weapon sau khi player mo ruong.
        if (IsGroundPickupObject())
            DespawnTimer = TickTimer.CreateFromSeconds(Runner, despawnTime);
        else
            DespawnTimer = TickTimer.None;
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || !IsGroundPickupObject()) return;

        WeaponPickup pickup = GetComponent<WeaponPickup>();
        if (pickup == null || !pickup.IsWorldPickup) return;

        if (DespawnTimer.IsRunning && DespawnTimer.Expired(Runner))
        {
            DespawnTimer = TickTimer.None;
            Runner.Despawn(Object);
        }
    }
}
