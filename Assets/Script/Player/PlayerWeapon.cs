using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Fusion;

public class PlayerWeapon : NetworkBehaviour
{
    public enum WeaponType
    {
        None,
        Rifle,
        Pistol,
        Bat,
        Shovel
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction Settings")]

    public float interactionRadius = 3f;


    // =========================================================
    // SHOOT ROTATION
    // =========================================================

    [Header("Shoot Rotation")]

    public float shootRotateSpeed = 10f;


    // =========================================================
    // AMMO
    // =========================================================

    [Header("Ammo")]

    public int rifleMagazineSize = 30;
    public int pistolMagazineSize = 12;

    public int rifleStartAmmo = 30;
    public int pistolStartAmmo = 12;

    public int rifleStartReserveAmmo = 90;
    public int pistolStartReserveAmmo = 36;


    // =========================================================
    // NETWORK AMMO
    // =========================================================

    [Header("Network Ammo")]

    [Networked]
    public int RifleAmmo { get; set; }

    [Networked]
    public int PistolAmmo { get; set; }

    [Networked]
    public int RifleReserveAmmo { get; set; }

    [Networked]
    public int PistolReserveAmmo { get; set; }


    // =========================================================
    // AMMO PICKUP STATE
    // =========================================================

    [Header("Ammo Pickup State")]

    [Networked]
    public NetworkBool HasPickedRifleAmmo { get; set; }

    [Networked]
    public NetworkBool HasPickedPistolAmmo { get; set; }


    // =========================================================
    // RELOAD
    // =========================================================

    [Header("Reload")]

    [Networked]
    public NetworkBool IsReloading { get; set; }

    public float rifleReloadTime = 2f;
    public float pistolReloadTime = 1.5f;


    // =========================================================
    // WEAPONS
    // =========================================================

    [Header("Weapons")]

    public GameObject gun;
    public GameObject pistol;
    public GameObject bat;
    public GameObject shovel;


    // =========================================================
    // DROP WEAPON PREFABS
    // =========================================================

    [Header("Drop Weapon Prefabs")]

    public NetworkObject rifleDropPrefab;
    public NetworkObject pistolDropPrefab;
    public NetworkObject batDropPrefab;
    public NetworkObject shovelDropPrefab;


    // =========================================================
    // DROP SETTINGS
    // =========================================================

    [Header("Drop Settings")]

    public float dropDistance = 1.2f;
    public float dropHeight = 0.5f;


    // =========================================================
    // NETWORK WEAPON STATE
    // =========================================================

    [Header("Network Weapon State")]

    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))]
    public WeaponType CurrentWeapon { get; set; }

    [Networked]
    public NetworkBool HasRifle { get; set; }

    [Networked]
    public NetworkBool HasPistol { get; set; }

    [Networked]
    public NetworkBool HasBat { get; set; }

    [Networked]
    public NetworkBool HasShovel { get; set; }


    // =========================================================
    // INVENTORY WEAPONS
    // =========================================================

    [Header("Inventory Weapons")]

    public int maxWeaponSlots = 4;

    [Networked]
    public WeaponType Slot1 { get; set; }

    [Networked]
    public WeaponType Slot2 { get; set; }

    [Networked]
    public WeaponType Slot3 { get; set; }

    [Networked]
    public WeaponType Slot4 { get; set; }


    // =========================================================
    // GUN SETTINGS
    // =========================================================

    [Header("Gun Settings")]

    public Camera fpsCamera;


    // =========================================================
    // RIFLE
    // =========================================================

    [Header("Rifle")]

    public float rifleDamage = 25f;
    public float rifleFireRate = 10f;
    public float rifleRange = 100f;


    // =========================================================
    // PISTOL
    // =========================================================

    [Header("Pistol")]

    public float pistolDamage = 15f;
    public float pistolFireRate = 4f;
    public float pistolRange = 70f;


    // =========================================================
    // BAT
    // =========================================================

    [Header("Bat")]

    public float batDamage = 30f;

    public float batRange = 4f;


    // =========================================================
    // SHOVEL
    // =========================================================

    [Header("Shovel")]

    public float shovelDamage = 40f;

    public float shovelRange = 4f;


    // =========================================================
    // MELEE AOE
    // =========================================================

    [Header("Melee AOE")]

    [Tooltip("Thời gian giữa 2 lần đánh")]
    public float meleeCooldown = 0.5f;


    [Tooltip("Tầm đánh melee")]
    public float meleeRange = 4f;


    [Tooltip("Góc tổng của AOE")]
    [Range(1f, 180f)]
    public float meleeAOEAngle = 50f;


    [Tooltip("Độ cao tâm kiểm tra melee")]
    public float meleeHeight = 1f;


    private float nextMeleeTime = 0f;


    // =========================================================
    // EFFECTS
    // =========================================================

    [Header("Effects")]

    public ParticleSystem rifleMuzzleFlash;
    public ParticleSystem pistolMuzzleFlash;


    // =========================================================
    // SOUND
    // =========================================================

    [Header("Sound")]

    public AudioSource audioSource;

    public AudioClip rifleShotSound;
    public AudioClip pistolShotSound;

    public AudioClip batHitSound;
    public AudioClip shovelHitSound;

    public AudioClip rifleReloadSound;
    public AudioClip pistolReloadSound;


    // =========================================================
    // BULLET
    // =========================================================

    [Header("Bullet")]

    public GameObject tracerPrefab;

    public Transform firePoint;


    // =========================================================
    // PRIVATE
    // =========================================================

    private float nextFireTime;

    private PlayerAnimation playerAnim;

    private PlayerHealth playerHealth;

    private bool canShoot = false;

    private int lastShootSoundTick = -1;

    private int lastMeleeSoundTick = -1;

    private int lastReloadSoundTick = -1;


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        playerAnim =
            GetComponent<PlayerAnimation>();


        playerHealth =
            GetComponent<PlayerHealth>();


        // =====================================================
        // INITIAL NETWORK STATE
        // =====================================================

        if (HasStateAuthority)
        {
            RifleAmmo = 0;

            PistolAmmo = 0;

            RifleReserveAmmo = 0;

            PistolReserveAmmo = 0;

            HasPickedRifleAmmo = false;

            HasPickedPistolAmmo = false;

            IsReloading = false;

            CurrentWeapon =
                WeaponType.None;

            Slot1 =
                WeaponType.None;

            Slot2 =
                WeaponType.None;

            Slot3 =
                WeaponType.None;

            Slot4 =
                WeaponType.None;

            HasRifle = false;

            HasPistol = false;

            HasBat = false;

            HasShovel = false;
        }


        StopAllMuzzleFlash();

        UpdateWeaponVisibility();
    }


    // =========================================================
    // CHECK DEAD
    // =========================================================

    private bool IsPlayerDead()
    {
        return
            playerHealth != null &&
            playerHealth.IsDead;
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!HasInputAuthority)
            return;


        // =====================================================
        // DEAD
        // =====================================================

        if (IsPlayerDead())
            return;


        SwitchWeapon();

        SmoothRotateToCamera();

        ShootInput();

        MeleeInput();

        InteractInput();


        // =====================================================
        // DROP - Q
        // =====================================================

        if (
            Input.GetKeyDown(
                KeyCode.Q
            )
        )
        {
            if (!IsReloading)
            {
                DropCurrentWeapon();
            }
        }


        // =====================================================
        // RELOAD - R
        // =====================================================

        if (
            Input.GetKeyDown(
                KeyCode.R
            )
        )
        {
            if (!IsReloading)
            {
                RequestReloadRpc();
            }
        }
    }


    // =========================================================
    // DROP CURRENT WEAPON
    // =========================================================

    private void DropCurrentWeapon()
    {
        if (!HasInputAuthority)
            return;


        if (IsPlayerDead())
            return;


        if (IsReloading)
            return;


        if (
            CurrentWeapon ==
            WeaponType.None
        )
        {
            return;
        }


        RequestDropWeaponRpc(
            CurrentWeapon
        );
    }


    // =========================================================
    // RPC DROP
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestDropWeaponRpc(
        WeaponType weaponToDrop)
    {
        if (
            !HasStateAuthority ||
            IsPlayerDead()
        )
        {
            return;
        }


        if (IsReloading)
            return;


        if (
            weaponToDrop ==
            WeaponType.None
        )
        {
            return;
        }


        if (!HasWeapon(weaponToDrop))
            return;


        NetworkObject dropPrefab =
            GetDropPrefab(
                weaponToDrop
            );


        if (dropPrefab == null)
        {
            Debug.LogError(
                "[PlayerWeapon] " +
                "CHƯA GÁN DROP PREFAB: " +
                weaponToDrop
            );

            return;
        }


        int droppedRifleAmmo = -1;

        int droppedRifleReserveAmmo = -1;

        int droppedPistolAmmo = -1;

        int droppedPistolReserveAmmo = -1;


        if (
            weaponToDrop ==
            WeaponType.Rifle
        )
        {
            droppedRifleAmmo =
                RifleAmmo;

            droppedRifleReserveAmmo =
                RifleReserveAmmo;
        }


        if (
            weaponToDrop ==
            WeaponType.Pistol
        )
        {
            droppedPistolAmmo =
                PistolAmmo;

            droppedPistolReserveAmmo =
                PistolReserveAmmo;
        }


        Vector3 dropPosition =
            transform.position +
            transform.forward *
            dropDistance +
            Vector3.up *
            dropHeight;


        NetworkObject droppedObject =
            Runner.Spawn(
                dropPrefab,
                dropPosition,
                Quaternion.identity
            );


        if (droppedObject == null)
        {
            Debug.LogError(
                "[PlayerWeapon] " +
                "KHÔNG SPAWN ĐƯỢC DROP!"
            );

            return;
        }


        WeaponPickup pickup =
            droppedObject.GetComponent<WeaponPickup>();


        if (pickup != null)
        {
            pickup.SetupDroppedWeapon(
                weaponToDrop,
                droppedRifleAmmo,
                droppedRifleReserveAmmo,
                droppedPistolAmmo,
                droppedPistolReserveAmmo
            );
        }


        RemoveWeaponFromInventory(
            weaponToDrop
        );


        WeaponType nextWeapon =
            GetNextAvailableWeapon();


        CurrentWeapon =
            nextWeapon;


        canShoot =
            false;


        nextFireTime =
            Time.time + 0.2f;
    }


    // =========================================================
    // GET DROP PREFAB
    // =========================================================

    private NetworkObject GetDropPrefab(
        WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle:
                return rifleDropPrefab;

            case WeaponType.Pistol:
                return pistolDropPrefab;

            case WeaponType.Bat:
                return batDropPrefab;

            case WeaponType.Shovel:
                return shovelDropPrefab;
        }


        return null;
    }


    // =========================================================
    // REMOVE WEAPON
    // =========================================================

    private void RemoveWeaponFromInventory(
        WeaponType weapon)
    {
        if (Slot1 == weapon)
            Slot1 = WeaponType.None;


        if (Slot2 == weapon)
            Slot2 = WeaponType.None;


        if (Slot3 == weapon)
            Slot3 = WeaponType.None;


        if (Slot4 == weapon)
            Slot4 = WeaponType.None;


        switch (weapon)
        {
            case WeaponType.Rifle:

                HasRifle = false;

                break;


            case WeaponType.Pistol:

                HasPistol = false;

                break;


            case WeaponType.Bat:

                HasBat = false;

                break;


            case WeaponType.Shovel:

                HasShovel = false;

                break;
        }
    }


    // =========================================================
    // GET NEXT WEAPON
    // =========================================================

    private WeaponType GetNextAvailableWeapon()
    {
        if (
            Slot1 != WeaponType.None &&
            HasWeapon(Slot1)
        )
        {
            return Slot1;
        }


        if (
            Slot2 != WeaponType.None &&
            HasWeapon(Slot2)
        )
        {
            return Slot2;
        }


        if (
            Slot3 != WeaponType.None &&
            HasWeapon(Slot3)
        )
        {
            return Slot3;
        }


        if (
            Slot4 != WeaponType.None &&
            HasWeapon(Slot4)
        )
        {
            return Slot4;
        }


        return WeaponType.None;
    }


    // =========================================================
    // RESET INVENTORY ON RESPAWN
    // =========================================================

    public void ResetAllInventoryOnRespawn()
    {
        if (!HasStateAuthority)
            return;


        StopAllCoroutines();


        IsReloading =
            false;


        RifleAmmo =
            0;


        PistolAmmo =
            0;


        RifleReserveAmmo =
            0;


        PistolReserveAmmo =
            0;


        HasPickedRifleAmmo =
            false;


        HasPickedPistolAmmo =
            false;


        Slot1 =
            WeaponType.None;


        Slot2 =
            WeaponType.None;


        Slot3 =
            WeaponType.None;


        Slot4 =
            WeaponType.None;


        HasRifle =
            false;


        HasPistol =
            false;


        HasBat =
            false;


        HasShovel =
            false;


        CurrentWeapon =
            WeaponType.None;


        canShoot =
            false;


        nextFireTime =
            Time.time + 0.2f;


        nextMeleeTime =
            Time.time;


        UpdateWeaponVisibility();


        Debug.Log(
            "[PlayerWeapon] " +
            "Inventory RESET sau RESPawn"
        );
    }


    // =========================================================
    // CANCEL RELOAD ON DEATH
    // =========================================================

    public void CancelReloadOnDeath()
    {
        if (!HasStateAuthority)
            return;


        StopAllCoroutines();


        IsReloading =
            false;


        canShoot =
            false;


        nextFireTime =
            Time.time + 0.2f;


        nextMeleeTime =
            Time.time;
    }


    // =========================================================
    // REFRESH VISUAL
    // =========================================================

    public void RefreshWeaponVisuals()
    {
        UpdateWeaponVisibility();
    }


    // =========================================================
    // INTERACTION
    // =========================================================

    private void InteractInput()
    {
        if (
            !HasInputAuthority ||
            IsPlayerDead() ||
            IsReloading
        )
        {
            return;
        }


        if (
            !Input.GetKeyDown(
                KeyCode.F
            )
        )
        {
            return;
        }


        Collider[] colliders =
            Physics.OverlapSphere(
                transform.position,
                interactionRadius
            );


        float closestDist =
            float.MaxValue;


        WeaponChest closestChest = null;

        WeaponPickup closestPickup = null;


        foreach (
            Collider hit
            in colliders
        )
        {
            WeaponChest chest =
                hit.GetComponentInParent<WeaponChest>();


            if (chest != null)
            {
                float d =
                    Vector3.Distance(
                        transform.position,
                        chest.transform.position
                    );


                if (d < closestDist)
                {
                    closestDist =
                        d;

                    closestChest =
                        chest;

                    closestPickup =
                        null;
                }
            }


            WeaponPickup pickup =
                hit.GetComponentInParent<WeaponPickup>();


            if (pickup != null)
            {
                float d =
                    Vector3.Distance(
                        transform.position,
                        pickup.transform.position
                    );


                if (d < closestDist)
                {
                    closestDist =
                        d;

                    closestPickup =
                        pickup;

                    closestChest =
                        null;
                }
            }
        }


        // =====================================================
        // CHEST
        // =====================================================

        if (
            closestChest != null &&
            closestChest.Object != null &&
            closestChest.Object.IsValid
        )
        {
            if (HasStateAuthority)
            {
                closestChest.TryOpenFrom(this);
            }
            else
            {
                RequestChestOpenRpc(
                    closestChest.Object.Id
                );
            }


            return;
        }


        // =====================================================
        // WEAPON PICKUP
        // =====================================================

        if (
            closestPickup != null &&
            closestPickup.Object != null &&
            closestPickup.Object.IsValid
        )
        {
            if (HasStateAuthority)
            {
                closestPickup.TryPickupFrom(this);
            }
            else
            {
                RequestWeaponPickupRpc(
                    closestPickup.Object.Id
                );
            }
        }
    }


    // =========================================================
    // CHEST RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RequestChestOpenRpc(
        NetworkId chestId,
        RpcInfo info = default)
    {
        if (
            !HasStateAuthority ||
            IsPlayerDead()
        )
        {
            return;
        }


        if (
            Runner.TryFindObject(
                chestId,
                out NetworkObject chestObj
            )
        )
        {
            WeaponChest chest =
                chestObj.GetComponent<WeaponChest>();


            if (chest != null)
            {
                chest.TryOpenFrom(this);
            }
        }
    }


    // =========================================================
    // WEAPON PICKUP RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RequestWeaponPickupRpc(
        NetworkId pickupId,
        RpcInfo info = default)
    {
        if (
            !HasStateAuthority ||
            IsPlayerDead()
        )
        {
            return;
        }


        if (
            Runner.TryFindObject(
                pickupId,
                out NetworkObject pickupObj
            )
        )
        {
            WeaponPickup pickup =
                pickupObj.GetComponent<WeaponPickup>();


            if (pickup != null)
            {
                pickup.TryPickupFrom(this);
            }
        }
    }


    // =========================================================
    // SWITCH WEAPON
    // =========================================================

    private void SwitchWeapon()
    {
        if (
            !HasInputAuthority ||
            IsPlayerDead() ||
            IsReloading
        )
        {
            return;
        }


        if (
            Input.GetKeyDown(
                KeyCode.Alpha1
            )
        )
        {
            EquipSlot(Slot1);
        }


        if (
            Input.GetKeyDown(
                KeyCode.Alpha2
            )
        )
        {
            EquipSlot(Slot2);
        }


        if (
            Input.GetKeyDown(
                KeyCode.Alpha3
            )
        )
        {
            EquipSlot(Slot3);
        }


        if (
            Input.GetKeyDown(
                KeyCode.Alpha4
            )
        )
        {
            EquipSlot(Slot4);
        }
    }


    private void EquipSlot(
        WeaponType weapon)
    {
        if (weapon == WeaponType.None)
            return;


        if (!HasWeapon(weapon))
            return;


        canShoot =
            false;


        nextFireTime =
            Time.time + 0.1f;


        RequestSwitchWeaponRpc(
            weapon
        );
    }


    // =========================================================
    // SWITCH RPC
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestSwitchWeaponRpc(
        WeaponType weapon)
    {
        if (
            !HasStateAuthority ||
            IsReloading ||
            IsPlayerDead() ||
            !HasWeapon(weapon)
        )
        {
            return;
        }


        CurrentWeapon =
            weapon;
    }


    // =========================================================
    // SHOOT INPUT
    // =========================================================

    private void ShootInput()
    {
        if (
            !HasInputAuthority ||
            IsPlayerDead() ||
            IsReloading
        )
        {
            return;
        }


        if (
            CurrentWeapon != WeaponType.Rifle &&
            CurrentWeapon != WeaponType.Pistol
        )
        {
            return;
        }


        if (fpsCamera == null)
            return;


        // =====================================================
        // AUTO RELOAD RIFLE
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Rifle &&
            RifleAmmo <= 0
        )
        {
            if (
                RifleReserveAmmo > 0
            )
            {
                RequestReloadRpc();
            }


            return;
        }


        // =====================================================
        // AUTO RELOAD PISTOL
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Pistol &&
            PistolAmmo <= 0
        )
        {
            if (
                PistolReserveAmmo > 0
            )
            {
                RequestReloadRpc();
            }


            return;
        }


        // =====================================================
        // PREVENT INSTANT SHOOT
        // =====================================================

        if (!canShoot)
        {
            if (
                Input.GetMouseButtonUp(0)
            )
            {
                canShoot =
                    true;
            }


            return;
        }


        if (
            !Input.GetMouseButton(0)
        )
        {
            return;
        }


        float rate =
            CurrentWeapon ==
            WeaponType.Rifle
            ? rifleFireRate
            : pistolFireRate;


        if (
            Time.time <
            nextFireTime
        )
        {
            return;
        }


        nextFireTime =
            Time.time +
            1f / rate;


        // =====================================================
        // CROSSHAIR
        // =====================================================

        Vector3 screenCenter =
            new Vector3(
                Screen.width * 0.5f,
                Screen.height * 0.5f,
                0f
            );


        Ray cameraRay =
            fpsCamera.ScreenPointToRay(
                screenCenter
            );


        Vector3 targetPoint;


        if (
            Physics.Raycast(
                cameraRay,
                out RaycastHit hit,
                1000f,
                ~0,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            targetPoint =
                hit.point;
        }
        else
        {
            targetPoint =
                cameraRay.origin +
                cameraRay.direction *
                1000f;
        }


        if (firePoint == null)
            return;


        Vector3 shootDirection =
            (
                targetPoint -
                firePoint.position
            ).normalized;


        RequestShootRpc(
            shootDirection
        );
    }


    // =========================================================
    // SHOOT RPC
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestShootRpc(
        Vector3 shootDirection)
    {
        if (
            !HasStateAuthority ||
            IsReloading ||
            IsPlayerDead() ||
            firePoint == null
        )
        {
            return;
        }


        shootDirection.Normalize();


        // =====================================================
        // RIFLE
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Rifle
        )
        {
            if (RifleAmmo <= 0)
                return;


            RifleAmmo--;


            ServerShoot(
                shootDirection,
                rifleDamage,
                rifleRange
            );


            return;
        }


        // =====================================================
        // PISTOL
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Pistol
        )
        {
            if (PistolAmmo <= 0)
                return;


            PistolAmmo--;


            ServerShoot(
                shootDirection,
                pistolDamage,
                pistolRange
            );
        }
    }


    // =========================================================
    // SERVER SHOOT
    // =========================================================

    private void ServerShoot(
        Vector3 shootDirection,
        float damage,
        float range)
    {
        if (
            !HasStateAuthority ||
            IsPlayerDead() ||
            firePoint == null
        )
        {
            return;
        }


        shootDirection.Normalize();


        Vector3 origin =
            firePoint.position;


        Vector3 targetPoint =
            origin +
            shootDirection *
            range;


        Ray ray =
            new Ray(
                origin,
                shootDirection
            );


        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                range,
                ~0,
                QueryTriggerInteraction.Ignore
            )
        )
        {
            targetPoint =
                hit.point;


            PlayerHealth target =
                hit.collider.GetComponentInParent<PlayerHealth>();


            if (target != null)
            {
                PlayerHealth ownHealth =
                    GetComponentInParent<PlayerHealth>();


                if (
                    target != ownHealth &&
                    !target.IsDead
                )
                {
                    // =================================================
                    // TRUYỀN VỊ TRÍ NGƯỜI BẮN
                    // =================================================

                    target.TakeDamage(
                        damage,
                        origin
                    );
                }
            }
        }


        Rpc_PlayShootEffects(
            origin,
            targetPoint,
            CurrentWeapon
        );
    }


    // =========================================================
    // SHOOT EFFECTS
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayShootEffects(
        Vector3 startPoint,
        Vector3 targetPoint,
        WeaponType weapon)
    {
        if (playerAnim != null)
        {
            playerAnim.Shoot();
        }


        if (
            weapon ==
            WeaponType.Rifle
        )
        {
            if (
                rifleMuzzleFlash != null
            )
            {
                rifleMuzzleFlash.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );


                rifleMuzzleFlash.Play();
            }
        }
        else if (
            weapon ==
            WeaponType.Pistol
        )
        {
            if (
                pistolMuzzleFlash != null
            )
            {
                pistolMuzzleFlash.Stop(
                    true,
                    ParticleSystemStopBehavior
                        .StopEmittingAndClear
                );


                pistolMuzzleFlash.Play();
            }
        }


        PlayShootSoundOnce(
            weapon
        );


        // =====================================================
        // TRACER
        // =====================================================

        if (tracerPrefab != null)
        {
            Vector3 direction =
                targetPoint -
                startPoint;


            if (
                direction.sqrMagnitude >
                0.001f
            )
            {
                GameObject tracer =
                    Instantiate(
                        tracerPrefab,
                        startPoint,
                        Quaternion.LookRotation(
                            direction
                        )
                    );


                BulletTracer bulletTracer =
                    tracer.GetComponent<BulletTracer>();


                if (bulletTracer != null)
                {
                    bulletTracer.Fire(
                        startPoint,
                        targetPoint
                    );
                }
            }
        }
    }


    // =========================================================
    // SHOOT SOUND
    // =========================================================

    private void PlayShootSoundOnce(
        WeaponType weapon)
    {
        if (audioSource == null)
            return;


        int currentTick =
            Runner.Tick.Raw;


        if (
            lastShootSoundTick ==
            currentTick
        )
        {
            return;
        }


        lastShootSoundTick =
            currentTick;


        if (
            weapon ==
            WeaponType.Rifle &&
            rifleShotSound != null
        )
        {
            audioSource.PlayOneShot(
                rifleShotSound
            );
        }
        else if (
            weapon ==
            WeaponType.Pistol &&
            pistolShotSound != null
        )
        {
            audioSource.PlayOneShot(
                pistolShotSound
            );
        }
    }


    // =========================================================
    // MELEE INPUT
    // =========================================================

    private void MeleeInput()
    {
        if (
            !HasInputAuthority ||
            IsPlayerDead() ||
            IsReloading
        )
        {
            return;
        }


        // =====================================================
        // CHỈ BAT / SHOVEL
        // =====================================================

        if (
            CurrentWeapon !=
            WeaponType.Bat &&
            CurrentWeapon !=
            WeaponType.Shovel
        )
        {
            return;
        }


        // =====================================================
        // LEFT MOUSE
        // =====================================================

        if (
            !Input.GetMouseButtonDown(0)
        )
        {
            return;
        }


        // =====================================================
        // COOLDOWN
        // =====================================================

        if (
            Time.time <
            nextMeleeTime
        )
        {
            return;
        }


        if (fpsCamera == null)
            return;


        nextMeleeTime =
            Time.time +
            meleeCooldown;


        // =====================================================
        // ATTACK DIRECTION
        // =====================================================

        Vector3 attackDirection =
            fpsCamera.transform.forward;


        attackDirection.y =
            0f;


        if (
            attackDirection.sqrMagnitude <
            0.001f
        )
        {
            attackDirection =
                transform.forward;
        }


        attackDirection.Normalize();


        // =====================================================
        // RPC
        // =====================================================

        RequestMeleeRpc(
            attackDirection
        );
    }


    // =========================================================
    // MELEE RPC - AOE
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestMeleeRpc(
        Vector3 attackDirection)
    {
        if (
            !HasStateAuthority ||
            IsReloading ||
            IsPlayerDead()
        )
        {
            return;
        }


        // =====================================================
        // DAMAGE
        // =====================================================

        float damage;


        if (
            CurrentWeapon ==
            WeaponType.Bat
        )
        {
            damage =
                batDamage;
        }
        else if (
            CurrentWeapon ==
            WeaponType.Shovel
        )
        {
            damage =
                shovelDamage;
        }
        else
        {
            return;
        }


        // =====================================================
        // RANGE
        // =====================================================

        float range =
            meleeRange;


        // =====================================================
        // CLEAN ATTACK DIRECTION
        // =====================================================

        attackDirection.y =
            0f;


        if (
            attackDirection.sqrMagnitude <
            0.001f
        )
        {
            attackDirection =
                transform.forward;
        }


        attackDirection.Normalize();


        // =====================================================
        // ATTACK ORIGIN
        // =====================================================

        Vector3 attackOrigin =
            transform.position +
            Vector3.up *
            meleeHeight;


        // =====================================================
        // FIND ALL COLLIDERS
        // =====================================================

        Collider[] hits =
            Physics.OverlapSphere(
                attackOrigin,
                range,
                ~0,
                QueryTriggerInteraction.Ignore
            );


        // =====================================================
        // AVOID MULTIPLE DAMAGE
        // =====================================================

        HashSet<PlayerHealth> damagedPlayers =
            new HashSet<PlayerHealth>();


        // =====================================================
        // LOOP
        // =====================================================

        foreach (
            Collider hit
            in hits
        )
        {
            if (hit == null)
                continue;


            PlayerHealth target =
                hit.GetComponentInParent<PlayerHealth>();


            if (target == null)
                continue;


            // =================================================
            // KHÔNG ĐÁNH CHÍNH MÌNH
            // =================================================

            if (target == playerHealth)
                continue;


            // =================================================
            // TARGET DEAD
            // =================================================

            if (target.IsDead)
                continue;


            // =================================================
            // TRÁNH DAMAGE NHIỀU LẦN
            // =================================================

            if (
                damagedPlayers.Contains(
                    target
                )
            )
            {
                continue;
            }


            // =================================================
            // TARGET DIRECTION
            // =================================================

            Vector3 targetDirection =
                target.transform.position -
                attackOrigin;


            targetDirection.y =
                0f;


            float distance =
                targetDirection.magnitude;


            // =================================================
            // RANGE CHECK
            // =================================================

            if (
                distance >
                range
            )
            {
                continue;
            }


            if (
                distance <
                0.01f
            )
            {
                continue;
            }


            targetDirection.Normalize();


            // =================================================
            // ANGLE CHECK
            // =================================================

            float angle =
                Vector3.Angle(
                    attackDirection,
                    targetDirection
                );


            float halfAngle =
                meleeAOEAngle *
                0.5f;


            if (
                angle >
                halfAngle
            )
            {
                continue;
            }


            // =================================================
            // DAMAGE
            // =================================================

            target.TakeDamage(
                damage,
                transform.position
            );


            damagedPlayers.Add(
                target
            );


            Debug.Log(
                "[Melee AOE] " +
                "Weapon = " +
                CurrentWeapon +
                " | Target = " +
                target.Object.InputAuthority +
                " | Damage = " +
                damage +
                " | Distance = " +
                distance +
                " | Angle = " +
                angle
            );
        }


        // =====================================================
        // EFFECT
        // =====================================================

        Rpc_PlayMeleeEffects(
            CurrentWeapon
        );
    }


    // =========================================================
    // MELEE EFFECTS
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayMeleeEffects(
        WeaponType weapon)
    {
        if (playerAnim != null)
        {
            playerAnim.MeleeAttack();
        }


        PlayMeleeSoundOnce(
            weapon
        );
    }


    // =========================================================
    // MELEE SOUND
    // =========================================================

    private void PlayMeleeSoundOnce(
        WeaponType weapon)
    {
        if (audioSource == null)
            return;


        int currentTick =
            Runner.Tick.Raw;


        if (
            lastMeleeSoundTick ==
            currentTick
        )
        {
            return;
        }


        lastMeleeSoundTick =
            currentTick;


        if (
            weapon ==
            WeaponType.Bat &&
            batHitSound != null
        )
        {
            audioSource.PlayOneShot(
                batHitSound
            );
        }
        else if (
            weapon ==
            WeaponType.Shovel &&
            shovelHitSound != null
        )
        {
            audioSource.PlayOneShot(
                shovelHitSound
            );
        }
    }


    // =========================================================
    // WEAPON VISIBILITY
    // =========================================================

    private void UpdateWeaponVisibility()
    {
        if (gun != null)
        {
            gun.SetActive(
                CurrentWeapon ==
                WeaponType.Rifle
            );
        }


        if (pistol != null)
        {
            pistol.SetActive(
                CurrentWeapon ==
                WeaponType.Pistol
            );
        }


        if (bat != null)
        {
            bat.SetActive(
                CurrentWeapon ==
                WeaponType.Bat
            );
        }


        if (shovel != null)
        {
            shovel.SetActive(
                CurrentWeapon ==
                WeaponType.Shovel
            );
        }


        if (playerAnim != null)
        {
            playerAnim.SetMelee(
                CurrentWeapon ==
                    WeaponType.Bat ||
                CurrentWeapon ==
                    WeaponType.Shovel
            );
        }
    }


    private void OnCurrentWeaponChanged()
    {
        UpdateWeaponVisibility();
    }


    // =========================================================
    // HAS WEAPON
    // =========================================================

    private bool HasWeapon(
        WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle:
                return HasRifle;

            case WeaponType.Pistol:
                return HasPistol;

            case WeaponType.Bat:
                return HasBat;

            case WeaponType.Shovel:
                return HasShovel;
        }


        return false;
    }


    // =========================================================
    // RELOAD RPC
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestReloadRpc()
    {
        if (
            !HasStateAuthority ||
            IsReloading ||
            IsPlayerDead()
        )
        {
            return;
        }


        if (
            CurrentWeapon !=
            WeaponType.Rifle &&
            CurrentWeapon !=
            WeaponType.Pistol
        )
        {
            return;
        }


        // =====================================================
        // RIFLE
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Rifle
        )
        {
            if (
                RifleAmmo >=
                rifleMagazineSize ||
                RifleReserveAmmo <= 0
            )
            {
                return;
            }
        }


        // =====================================================
        // PISTOL
        // =====================================================

        if (
            CurrentWeapon ==
            WeaponType.Pistol
        )
        {
            if (
                PistolAmmo >=
                pistolMagazineSize ||
                PistolReserveAmmo <= 0
            )
            {
                return;
            }
        }


        IsReloading =
            true;


        WeaponType reloadWeapon =
            CurrentWeapon;


        Rpc_PlayReloadSound(
            reloadWeapon
        );


        StartCoroutine(
            ReloadStateAuthority(
                reloadWeapon
            )
        );
    }


    // =========================================================
    // RELOAD
    // =========================================================

    private IEnumerator ReloadStateAuthority(
        WeaponType reloadWeapon)
    {
        float reloadTime =
            reloadWeapon ==
            WeaponType.Rifle
            ? rifleReloadTime
            : pistolReloadTime;


        yield return new WaitForSeconds(
            reloadTime
        );


        if (
            !HasStateAuthority ||
            IsPlayerDead()
        )
        {
            IsReloading =
                false;


            yield break;
        }


        // =====================================================
        // RIFLE
        // =====================================================

        if (
            reloadWeapon ==
            WeaponType.Rifle
        )
        {
            int need =
                rifleMagazineSize -
                RifleAmmo;


            int load =
                Mathf.Min(
                    need,
                    RifleReserveAmmo
                );


            RifleAmmo +=
                load;


            RifleReserveAmmo -=
                load;
        }


        // =====================================================
        // PISTOL
        // =====================================================

        else if (
            reloadWeapon ==
            WeaponType.Pistol
        )
        {
            int need =
                pistolMagazineSize -
                PistolAmmo;


            int load =
                Mathf.Min(
                    need,
                    PistolReserveAmmo
                );


            PistolAmmo +=
                load;


            PistolReserveAmmo -=
                load;
        }


        IsReloading =
            false;
    }


    // =========================================================
    // RELOAD SOUND RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayReloadSound(
        WeaponType weapon)
    {
        PlayReloadSoundOnce(
            weapon
        );
    }


    private void PlayReloadSoundOnce(
        WeaponType weapon)
    {
        if (audioSource == null)
            return;


        int currentTick =
            Runner.Tick.Raw;


        if (
            lastReloadSoundTick ==
            currentTick
        )
        {
            return;
        }


        lastReloadSoundTick =
            currentTick;


        if (
            weapon ==
            WeaponType.Rifle &&
            rifleReloadSound != null
        )
        {
            audioSource.PlayOneShot(
                rifleReloadSound
            );
        }
        else if (
            weapon ==
            WeaponType.Pistol &&
            pistolReloadSound != null
        )
        {
            audioSource.PlayOneShot(
                pistolReloadSound
            );
        }
    }


    // =========================================================
    // PICKUP WEAPON
    // =========================================================

    public bool ServerPickupWeapon(
        WeaponType weapon,
        int droppedRifleAmmo = -1,
        int droppedRifleReserveAmmo = -1,
        int droppedPistolAmmo = -1,
        int droppedPistolReserveAmmo = -1)
    {
        if (
            !HasStateAuthority ||
            IsPlayerDead()
        )
        {
            return false;
        }


        if (weapon == WeaponType.None)
            return false;


        if (IsReloading)
            return false;


        if (HasWeapon(weapon))
            return false;


        // =====================================================
        // COUNT SLOTS
        // =====================================================

        int slotCount = 0;


        if (Slot1 != WeaponType.None)
            slotCount++;


        if (Slot2 != WeaponType.None)
            slotCount++;


        if (Slot3 != WeaponType.None)
            slotCount++;


        if (Slot4 != WeaponType.None)
            slotCount++;


        if (slotCount >= maxWeaponSlots)
            return false;


        // =====================================================
        // ADD SLOT
        // =====================================================

        if (Slot1 == WeaponType.None)
        {
            Slot1 =
                weapon;
        }
        else if (Slot2 == WeaponType.None)
        {
            Slot2 =
                weapon;
        }
        else if (Slot3 == WeaponType.None)
        {
            Slot3 =
                weapon;
        }
        else if (Slot4 == WeaponType.None)
        {
            Slot4 =
                weapon;
        }
        else
        {
            return false;
        }


        // =====================================================
        // OWN WEAPON
        // =====================================================

        SetWeaponOwned(
            weapon
        );


        // =====================================================
        // RIFLE AMMO
        // =====================================================

        if (
            weapon ==
            WeaponType.Rifle
        )
        {
            if (
                droppedRifleAmmo >= 0 &&
                droppedRifleReserveAmmo >= 0
            )
            {
                RifleAmmo =
                    Mathf.Clamp(
                        droppedRifleAmmo,
                        0,
                        rifleMagazineSize
                    );


                RifleReserveAmmo =
                    Mathf.Max(
                        0,
                        droppedRifleReserveAmmo
                    );


                HasPickedRifleAmmo =
                    true;
            }
            else if (
                !HasPickedRifleAmmo
            )
            {
                RifleAmmo =
                    rifleStartAmmo;


                RifleReserveAmmo =
                    rifleStartReserveAmmo;
            }
        }


        // =====================================================
        // PISTOL AMMO
        // =====================================================

        if (
            weapon ==
            WeaponType.Pistol
        )
        {
            if (
                droppedPistolAmmo >= 0 &&
                droppedPistolReserveAmmo >= 0
            )
            {
                PistolAmmo =
                    Mathf.Clamp(
                        droppedPistolAmmo,
                        0,
                        pistolMagazineSize
                    );


                PistolReserveAmmo =
                    Mathf.Max(
                        0,
                        droppedPistolReserveAmmo
                    );


                HasPickedPistolAmmo =
                    true;
            }
            else if (
                !HasPickedPistolAmmo
            )
            {
                PistolAmmo =
                    pistolStartAmmo;


                PistolReserveAmmo =
                    pistolStartReserveAmmo;
            }
        }


        // =====================================================
        // EQUIP
        // =====================================================

        CurrentWeapon =
            weapon;


        canShoot =
            false;


        nextFireTime =
            Time.time + 0.1f;


        return true;
    }


    // =========================================================
    // SET WEAPON OWNED
    // =========================================================

    private void SetWeaponOwned(
        WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle:

                HasRifle =
                    true;

                break;


            case WeaponType.Pistol:

                HasPistol =
                    true;

                break;


            case WeaponType.Bat:

                HasBat =
                    true;

                break;


            case WeaponType.Shovel:

                HasShovel =
                    true;

                break;
        }
    }


    // =========================================================
    // ROTATE
    // =========================================================

    private void SmoothRotateToCamera()
    {
        if (fpsCamera == null)
            return;


        Vector3 direction =
            fpsCamera.transform.forward;


        direction.y =
            0f;


        if (
            direction.sqrMagnitude <
            0.01f
        )
        {
            return;
        }


        Quaternion targetRotation =
            Quaternion.LookRotation(
                direction
            );


        transform.rotation =
            Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                shootRotateSpeed *
                Time.deltaTime
            );
    }


    // =========================================================
    // MUZZLE FLASH
    // =========================================================

    private void StopAllMuzzleFlash()
    {
        if (
            rifleMuzzleFlash != null
        )
        {
            var main =
                rifleMuzzleFlash.main;


            main.playOnAwake =
                false;


            rifleMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }


        if (
            pistolMuzzleFlash != null
        )
        {
            var main =
                pistolMuzzleFlash.main;


            main.playOnAwake =
                false;


            pistolMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior
                    .StopEmittingAndClear
            );
        }
    }


    // =========================================================
    // ADD RIFLE AMMO
    // =========================================================

    public void AddRifleAmmo(
        int amount)
    {
        if (
            !HasStateAuthority ||
            amount <= 0 ||
            IsPlayerDead()
        )
        {
            return;
        }


        RifleReserveAmmo +=
            amount;


        HasPickedRifleAmmo =
            true;
    }


    // =========================================================
    // ADD PISTOL AMMO
    // =========================================================

    public void AddPistolAmmo(
        int amount)
    {
        if (
            !HasStateAuthority ||
            amount <= 0 ||
            IsPlayerDead()
        )
        {
            return;
        }


        PistolReserveAmmo +=
            amount;


        HasPickedPistolAmmo =
            true;
    }


    // =========================================================
    // MELEE AOE GIZMOS
    // =========================================================

    private void OnDrawGizmosSelected()
    {
        Vector3 origin =
            transform.position +
            Vector3.up *
            meleeHeight;


        Vector3 forward =
            transform.forward;


        forward.y =
            0f;


        if (
            forward.sqrMagnitude <
            0.001f
        )
        {
            return;
        }


        forward.Normalize();


        // =====================================================
        // RANGE
        // =====================================================

        Gizmos.DrawWireSphere(
            origin,
            meleeRange
        );


        // =====================================================
        // ANGLE
        // =====================================================

        float halfAngle =
            meleeAOEAngle *
            0.5f;


        Vector3 leftDirection =
            Quaternion.Euler(
                0f,
                -halfAngle,
                0f
            ) *
            forward;


        Vector3 rightDirection =
            Quaternion.Euler(
                0f,
                halfAngle,
                0f
            ) *
            forward;


        // =====================================================
        // AOE LINES
        // =====================================================

        Gizmos.DrawLine(
            origin,
            origin +
            leftDirection *
            meleeRange
        );


        Gizmos.DrawLine(
            origin,
            origin +
            rightDirection *
            meleeRange
        );


        // =====================================================
        // CENTER
        // =====================================================

        Gizmos.DrawLine(
            origin,
            origin +
            forward *
            meleeRange
        );
    }
}