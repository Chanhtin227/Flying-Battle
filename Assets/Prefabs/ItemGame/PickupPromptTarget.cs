using System.Collections.Generic;
using UnityEngine;
using Fusion;

public class PickupPromptTarget : MonoBehaviour
{
    public static readonly HashSet<PickupPromptTarget> Targets =
        new HashSet<PickupPromptTarget>();

    [Header("Prompt")]
    public KeyCode key = KeyCode.F;
    public string actionText = "Pick Up Ammo";
    public float distance = 3f;

    private NetworkObject networkObject;
    private WeaponChest chest;
    private WeaponPickup weapon;
    private MedkitPickup medkit;

    private void Awake()
    {
        networkObject = GetComponentInParent<NetworkObject>();
        chest = GetComponent<WeaponChest>();
        weapon = GetComponent<WeaponPickup>();
        medkit = GetComponent<MedkitPickup>();
    }

    private void OnEnable()
    {
        Targets.Add(this);
    }

    private void OnDisable()
    {
        Targets.Remove(this);
    }

    public bool CanShow()
    {
        if (!isActiveAndEnabled)
            return false;

        // Không đọc trạng thái Fusion trước khi spawn.
        if (networkObject != null && !networkObject.IsValid)
            return false;

        if (chest != null)
            return chest.CanShowInteractionPrompt;

        if (weapon != null)
        {
            return weapon.IsWorldPickup &&
                   weapon.weaponType != PlayerWeapon.WeaponType.None &&
                   GetComponentInParent<PlayerWeapon>() == null;
        }

        return true;
    }

    public float GetDistance()
    {
        if (chest != null)
            return chest.interactDistance;

        if (weapon != null)
            return weapon.pickupDistance;

        if (medkit != null)
            return medkit.pickupDistance;

        return distance;
    }

    public string GetPrompt()
    {
        if (chest != null)
            return $"[{chest.interactKey}] Open Chest";

        if (weapon != null)
            return $"[{key}] Pick Up {weapon.weaponType}";

        if (medkit != null)
            return $"[{medkit.pickupKey}] Pick Up {medkit.medkitType} Medkit";

        return $"[{key}] {actionText}";
    }

    public float GetPlayerDistance(Vector3 playerPosition)
    {
        Vector3 offset = transform.position - playerPosition;

        // Khớp kiểm tra X/Z trong WeaponPickup.
        if (weapon != null)
            offset.y = 0f;

        return offset.magnitude;
    }
}