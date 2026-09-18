using UnityEngine;
using System.Collections;
using Fusion;

public class PlayerWeapon : NetworkBehaviour
{
    // =========================================================
    // WEAPON TYPE
    // =========================================================

    public enum WeaponType
    {
        None,
        Rifle,
        Pistol,
        Bat,
        Shovel
    }


    // =========================================================
    // SHOOT ROTATION
    // =========================================================

    [Header("Shoot Rotation")]

    public float shootRotateSpeed = 10f;


    // =========================================================
    // AMMO SETTINGS
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
    // RELOAD
    // =========================================================

    [Header("Reload")]

    [Networked]
    public NetworkBool IsReloading { get; set; }

    public float rifleReloadTime = 2f;
    public float pistolReloadTime = 1.5f;


    // =========================================================
    // WEAPON OBJECTS
    // =========================================================

    [Header("Weapons")]

    public GameObject gun;
    public GameObject pistol;
    public GameObject bat;
    public GameObject shovel;


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
    // 4 INVENTORY SLOTS
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
    // CAMERA
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
    public float batRange = 2f;


    // =========================================================
    // SHOVEL
    // =========================================================

    [Header("Shovel")]

    public float shovelDamage = 40f;
    public float shovelRange = 2.5f;


    // =========================================================
    // MELEE
    // =========================================================

    [Header("Melee Settings")]

    public float meleeCooldown = 0.5f;

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
    // INTERNAL
    // =========================================================

    private float nextFireTime;

    private PlayerAnimation playerAnim;


    // Không cho bắn ngay khi vừa đổi / nhặt súng
    private bool canShoot = false;


    // =========================================================
    // SOUND ANTI DOUBLE
    // =========================================================

    private int lastShootSoundTick = -1;
    private int lastMeleeSoundTick = -1;
    private int lastReloadSoundTick = -1;


    // =========================================================
    // SPAWN
    // =========================================================

    public override void Spawned()
    {
        playerAnim = GetComponent<PlayerAnimation>();

        if (HasStateAuthority)
        {
            RifleAmmo = 0;
            PistolAmmo = 0;

            RifleReserveAmmo = 0;
            PistolReserveAmmo = 0;

            IsReloading = false;

            CurrentWeapon = WeaponType.None;

            Slot1 = WeaponType.None;
            Slot2 = WeaponType.None;
            Slot3 = WeaponType.None;
            Slot4 = WeaponType.None;

            HasRifle = false;
            HasPistol = false;
            HasBat = false;
            HasShovel = false;
        }

        StopAllMuzzleFlash();

        UpdateWeaponVisibility();
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (!HasInputAuthority)
            return;

        SwitchWeapon();

        SmoothRotateToCamera();

        ShootInput();

        MeleeInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!IsReloading)
            {
                RequestReloadRpc();
            }
        }
    }


    // =========================================================
    // SWITCH WEAPON
    // =========================================================

    private void SwitchWeapon()
    {
        if (!HasInputAuthority)
            return;

        if (IsReloading)
            return;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            EquipSlot(Slot1);
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            EquipSlot(Slot2);
        }

        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            EquipSlot(Slot3);
        }

        if (Input.GetKeyDown(KeyCode.Alpha4))
        {
            EquipSlot(Slot4);
        }
    }


    // =========================================================
    // EQUIP SLOT
    // =========================================================

    private void EquipSlot(WeaponType weapon)
    {
        if (weapon == WeaponType.None)
            return;

        if (!HasWeapon(weapon))
            return;

        canShoot = false;

        nextFireTime = Time.time + 0.1f;

        RequestSwitchWeaponRpc(weapon);
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
        if (!HasStateAuthority)
            return;

        if (IsReloading)
            return;

        if (!HasWeapon(weapon))
            return;

        CurrentWeapon = weapon;

        Debug.Log(
            "Đổi vũ khí thành: " +
            weapon
        );
    }


    // =========================================================
    // SHOOT INPUT
    // =========================================================

    private void ShootInput()
    {
        if (!HasInputAuthority)
            return;

        if (IsReloading)
            return;

        if (
            CurrentWeapon != WeaponType.Rifle &&
            CurrentWeapon != WeaponType.Pistol
        )
        {
            return;
        }

        if (fpsCamera == null)
            return;


        // -----------------------------------------------------
        // PHẢI THẢ CHUỘT TRƯỚC
        // -----------------------------------------------------

        if (!canShoot)
        {
            if (Input.GetMouseButtonUp(0))
            {
                canShoot = true;
            }

            return;
        }


        // -----------------------------------------------------
        // GIỮ CHUỘT ĐỂ BẮN
        // -----------------------------------------------------

        if (!Input.GetMouseButton(0))
            return;


        float rate =
            CurrentWeapon == WeaponType.Rifle
            ? rifleFireRate
            : pistolFireRate;


        if (Time.time < nextFireTime)
            return;


        nextFireTime =
            Time.time +
            1f / rate;


        Vector3 shootDirection =
            fpsCamera.transform.forward;


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
        if (!HasStateAuthority)
            return;

        if (IsReloading)
            return;

        if (firePoint == null)
            return;


        shootDirection.Normalize();


        // =====================================================
        // RIFLE
        // =====================================================

        if (CurrentWeapon == WeaponType.Rifle)
        {
            if (RifleAmmo <= 0)
            {
                Debug.Log(
                    "Rifle hết đạn | Player: " +
                    Object.InputAuthority
                );

                return;
            }

            RifleAmmo--;


            Debug.Log(
                "PLAYER " +
                Object.InputAuthority +
                " BẮN RIFLE | Ammo: " +
                RifleAmmo +
                " / " +
                RifleReserveAmmo
            );


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

        if (CurrentWeapon == WeaponType.Pistol)
        {
            if (PistolAmmo <= 0)
            {
                Debug.Log(
                    "Pistol hết đạn | Player: " +
                    Object.InputAuthority
                );

                return;
            }

            PistolAmmo--;


            Debug.Log(
                "PLAYER " +
                Object.InputAuthority +
                " BẮN PISTOL | Ammo: " +
                PistolAmmo +
                " / " +
                PistolReserveAmmo
            );


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
        if (!HasStateAuthority)
            return;

        if (firePoint == null)
            return;


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
                range
            )
        )
        {
            targetPoint = hit.point;


            Debug.Log(
                "Đạn trúng: " +
                hit.collider.name
            );


            PlayerHealth target =
                hit.collider.GetComponentInParent<PlayerHealth>();


            if (target != null)
            {
                PlayerHealth ownHealth =
                    GetComponentInParent<PlayerHealth>();


                if (target != ownHealth)
                {
                    target.TakeDamage(damage);


                    Debug.Log(
                        "======================================"
                    );

                    Debug.Log(
                        "SERVER DAMAGE"
                    );

                    Debug.Log(
                        "Shooter: " +
                        Object.InputAuthority
                    );

                    Debug.Log(
                        "Target: " +
                        target.name
                    );

                    Debug.Log(
                        "Damage: " +
                        damage
                    );

                    Debug.Log(
                        "======================================"
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
    // SHOOT EFFECT RPC
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


        // -----------------------------------------------------
        // MUZZLE FLASH
        // -----------------------------------------------------

        if (weapon == WeaponType.Rifle)
        {
            if (rifleMuzzleFlash != null)
            {
                rifleMuzzleFlash.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                rifleMuzzleFlash.Play();
            }
        }
        else if (weapon == WeaponType.Pistol)
        {
            if (pistolMuzzleFlash != null)
            {
                pistolMuzzleFlash.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmittingAndClear
                );

                pistolMuzzleFlash.Play();
            }
        }


        // -----------------------------------------------------
        // SOUND
        // -----------------------------------------------------

        PlayShootSoundOnce(weapon);


        // -----------------------------------------------------
        // TRACER
        // -----------------------------------------------------

        if (tracerPrefab != null)
        {
            Vector3 direction =
                targetPoint -
                startPoint;


            if (direction.sqrMagnitude > 0.001f)
            {
                GameObject tracer =
                    Instantiate(
                        tracerPrefab,
                        startPoint,
                        Quaternion.LookRotation(direction)
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


        if (lastShootSoundTick == currentTick)
            return;


        lastShootSoundTick =
            currentTick;


        if (weapon == WeaponType.Rifle)
        {
            if (rifleShotSound != null)
            {
                audioSource.PlayOneShot(
                    rifleShotSound
                );
            }
        }
        else if (weapon == WeaponType.Pistol)
        {
            if (pistolShotSound != null)
            {
                audioSource.PlayOneShot(
                    pistolShotSound
                );
            }
        }
    }


    // =========================================================
    // MELEE INPUT
    // =========================================================

    private void MeleeInput()
    {
        if (!HasInputAuthority)
            return;

        if (IsReloading)
            return;


        if (
            CurrentWeapon != WeaponType.Bat &&
            CurrentWeapon != WeaponType.Shovel
        )
        {
            return;
        }


        if (!Input.GetMouseButtonDown(0))
            return;


        if (Time.time < nextMeleeTime)
            return;


        if (fpsCamera == null)
            return;


        nextMeleeTime =
            Time.time +
            meleeCooldown;


        Vector3 attackDirection =
            fpsCamera.transform.forward;


        RequestMeleeRpc(
            attackDirection
        );
    }


    // =========================================================
    // MELEE RPC
    // =========================================================

    [Rpc(
        RpcSources.InputAuthority,
        RpcTargets.StateAuthority
    )]
    private void RequestMeleeRpc(
        Vector3 attackDirection)
    {
        if (!HasStateAuthority)
            return;

        if (IsReloading)
            return;


        float damage = 0f;
        float range = 0f;


        if (CurrentWeapon == WeaponType.Bat)
        {
            damage = batDamage;
            range = batRange;
        }
        else if (CurrentWeapon == WeaponType.Shovel)
        {
            damage = shovelDamage;
            range = shovelRange;
        }
        else
        {
            return;
        }


        attackDirection.Normalize();


        Vector3 origin =
            firePoint != null
            ? firePoint.position
            : transform.position;


        Vector3 targetPoint =
            origin +
            attackDirection *
            range;


        Ray ray =
            new Ray(
                origin,
                attackDirection
            );


        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                range
            )
        )
        {
            targetPoint = hit.point;


            Debug.Log(
                "Cận chiến trúng: " +
                hit.collider.name
            );


            PlayerHealth target =
                hit.collider.GetComponentInParent<PlayerHealth>();


            if (target != null)
            {
                PlayerHealth ownHealth =
                    GetComponentInParent<PlayerHealth>();


                if (target != ownHealth)
                {
                    target.TakeDamage(damage);


                    Debug.Log(
                        "======================================"
                    );

                    Debug.Log(
                        "SERVER MELEE DAMAGE"
                    );

                    Debug.Log(
                        "Player: " +
                        Object.InputAuthority
                    );

                    Debug.Log(
                        "Weapon: " +
                        CurrentWeapon
                    );

                    Debug.Log(
                        "Damage: " +
                        damage
                    );

                    Debug.Log(
                        "======================================"
                    );
                }
            }
        }


        Rpc_PlayMeleeEffects(
            CurrentWeapon
        );
    }


    // =========================================================
    // MELEE EFFECT RPC
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


        PlayMeleeSoundOnce(weapon);
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


        if (lastMeleeSoundTick == currentTick)
            return;


        lastMeleeSoundTick =
            currentTick;


        if (weapon == WeaponType.Bat)
        {
            if (batHitSound != null)
            {
                audioSource.PlayOneShot(
                    batHitSound
                );
            }
        }
        else if (weapon == WeaponType.Shovel)
        {
            if (shovelHitSound != null)
            {
                audioSource.PlayOneShot(
                    shovelHitSound
                );
            }
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
                CurrentWeapon == WeaponType.Rifle
            );
        }


        if (pistol != null)
        {
            pistol.SetActive(
                CurrentWeapon == WeaponType.Pistol
            );
        }


        if (bat != null)
        {
            bat.SetActive(
                CurrentWeapon == WeaponType.Bat
            );
        }


        if (shovel != null)
        {
            shovel.SetActive(
                CurrentWeapon == WeaponType.Shovel
            );
        }


        if (playerAnim != null)
        {
            playerAnim.SetMelee(
                CurrentWeapon == WeaponType.Bat ||
                CurrentWeapon == WeaponType.Shovel
            );
        }
    }


    // =========================================================
    // WEAPON CHANGED
    // =========================================================

    private void OnCurrentWeaponChanged()
    {
        UpdateWeaponVisibility();
    }


    // =========================================================
    // CHECK WEAPON OWNED
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
        if (!HasStateAuthority)
            return;

        if (IsReloading)
            return;


        if (
            CurrentWeapon != WeaponType.Rifle &&
            CurrentWeapon != WeaponType.Pistol
        )
        {
            return;
        }


        // -----------------------------------------------------
        // RIFLE
        // -----------------------------------------------------

        if (CurrentWeapon == WeaponType.Rifle)
        {
            if (RifleAmmo >= rifleMagazineSize)
            {
                Debug.Log(
                    "Rifle đã đầy đạn!"
                );

                return;
            }


            if (RifleReserveAmmo <= 0)
            {
                Debug.Log(
                    "Không còn đạn Rifle dự trữ!"
                );

                return;
            }
        }


        // -----------------------------------------------------
        // PISTOL
        // -----------------------------------------------------

        if (CurrentWeapon == WeaponType.Pistol)
        {
            if (PistolAmmo >= pistolMagazineSize)
            {
                Debug.Log(
                    "Pistol đã đầy đạn!"
                );

                return;
            }


            if (PistolReserveAmmo <= 0)
            {
                Debug.Log(
                    "Không còn đạn Pistol dự trữ!"
                );

                return;
            }
        }


        // -----------------------------------------------------
        // START RELOAD
        // -----------------------------------------------------

        IsReloading = true;


        WeaponType reloadWeapon =
            CurrentWeapon;


        Debug.Log(
            "RELOAD START | Player: " +
            Object.InputAuthority +
            " | Weapon: " +
            reloadWeapon
        );


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
    // RELOAD STATE AUTHORITY
    // =========================================================

    private IEnumerator ReloadStateAuthority(
        WeaponType reloadWeapon)
    {
        float reloadTime =
            reloadWeapon == WeaponType.Rifle
            ? rifleReloadTime
            : pistolReloadTime;


        yield return new WaitForSeconds(
            reloadTime
        );


        if (!HasStateAuthority)
            yield break;


        // -----------------------------------------------------
        // RIFLE
        // -----------------------------------------------------

        if (reloadWeapon == WeaponType.Rifle)
        {
            int need =
                rifleMagazineSize -
                RifleAmmo;


            int load =
                Mathf.Min(
                    need,
                    RifleReserveAmmo
                );


            RifleAmmo += load;
            RifleReserveAmmo -= load;


            Debug.Log(
                "RIFLE RELOAD COMPLETE | Ammo: " +
                RifleAmmo +
                " / " +
                RifleReserveAmmo
            );
        }


        // -----------------------------------------------------
        // PISTOL
        // -----------------------------------------------------

        else if (reloadWeapon == WeaponType.Pistol)
        {
            int need =
                pistolMagazineSize -
                PistolAmmo;


            int load =
                Mathf.Min(
                    need,
                    PistolReserveAmmo
                );


            PistolAmmo += load;
            PistolReserveAmmo -= load;


            Debug.Log(
                "PISTOL RELOAD COMPLETE | Ammo: " +
                PistolAmmo +
                " / " +
                PistolReserveAmmo
            );
        }


        IsReloading = false;


        Debug.Log(
            "RELOAD END | Player: " +
            Object.InputAuthority
        );
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
        PlayReloadSoundOnce(weapon);
    }


    // =========================================================
    // RELOAD SOUND
    // =========================================================

    private void PlayReloadSoundOnce(
        WeaponType weapon)
    {
        if (audioSource == null)
            return;


        int currentTick =
            Runner.Tick.Raw;


        if (lastReloadSoundTick == currentTick)
            return;


        lastReloadSoundTick =
            currentTick;


        if (weapon == WeaponType.Rifle)
        {
            if (rifleReloadSound != null)
            {
                audioSource.PlayOneShot(
                    rifleReloadSound
                );
            }
        }
        else if (weapon == WeaponType.Pistol)
        {
            if (pistolReloadSound != null)
            {
                audioSource.PlayOneShot(
                    pistolReloadSound
                );
            }
        }
    }


    // =========================================================
    // PICKUP WEAPON
    // =========================================================
    //
    // Hàm này được WeaponPickup gọi ở SERVER.
    // Không cần tạo thêm Pickup RPC ở đây.
    //
    // =========================================================

    public bool ServerPickupWeapon(
        WeaponType weapon)
    {
        if (!HasStateAuthority)
            return false;


        if (weapon == WeaponType.None)
            return false;


        if (IsReloading)
            return false;


        // -----------------------------------------------------
        // ĐÃ CÓ VŨ KHÍ
        // -----------------------------------------------------

        if (HasWeapon(weapon))
        {
            Debug.Log(
                "Player đã có " +
                weapon
            );

            return false;
        }


        // -----------------------------------------------------
        // KIỂM TRA SLOT
        // -----------------------------------------------------

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
        {
            Debug.Log(
                "Đã đủ " +
                maxWeaponSlots +
                " vũ khí!"
            );

            return false;
        }


        // -----------------------------------------------------
        // THÊM SLOT TRỐNG ĐẦU TIÊN
        // -----------------------------------------------------

        if (Slot1 == WeaponType.None)
        {
            Slot1 = weapon;
        }
        else if (Slot2 == WeaponType.None)
        {
            Slot2 = weapon;
        }
        else if (Slot3 == WeaponType.None)
        {
            Slot3 = weapon;
        }
        else if (Slot4 == WeaponType.None)
        {
            Slot4 = weapon;
        }
        else
        {
            return false;
        }


        // -----------------------------------------------------
        // ĐÁNH DẤU OWNED
        // -----------------------------------------------------

        SetWeaponOwned(weapon);


        // -----------------------------------------------------
        // CẤP ĐẠN
        // -----------------------------------------------------

        if (weapon == WeaponType.Rifle)
        {
            RifleAmmo =
                rifleStartAmmo;

            RifleReserveAmmo =
                rifleStartReserveAmmo;
        }


        if (weapon == WeaponType.Pistol)
        {
            PistolAmmo =
                pistolStartAmmo;

            PistolReserveAmmo =
                pistolStartReserveAmmo;
        }


        // -----------------------------------------------------
        // CẦM NGAY
        // -----------------------------------------------------

        CurrentWeapon = weapon;


        // -----------------------------------------------------
        // CHỐNG BẮN NGAY
        // -----------------------------------------------------

        canShoot = false;

        nextFireTime =
            Time.time + 0.1f;


        // -----------------------------------------------------
        // INVENTORY MANAGER
        // -----------------------------------------------------
        //
        // Nếu InventoryManager của bạn vẫn đang dùng
        // thì giữ phần này.
        //
        // -----------------------------------------------------

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.AddWeapon(
                weapon.ToString()
            );

            Debug.Log(
                "Đã thêm " +
                weapon +
                " vào balo."
            );
        }


        // -----------------------------------------------------
        // LOG
        // -----------------------------------------------------

        Debug.Log(
            "======================================"
        );

        Debug.Log(
            "PICKUP WEAPON SUCCESS"
        );

        Debug.Log(
            "Player: " +
            Object.InputAuthority
        );

        Debug.Log(
            "Weapon: " +
            weapon
        );

        Debug.Log(
            "Slot 1: " +
            Slot1
        );

        Debug.Log(
            "Slot 2: " +
            Slot2
        );

        Debug.Log(
            "Slot 3: " +
            Slot3
        );

        Debug.Log(
            "Slot 4: " +
            Slot4
        );


        if (weapon == WeaponType.Rifle)
        {
            Debug.Log(
                "Rifle Ammo: " +
                RifleAmmo +
                " / " +
                RifleReserveAmmo
            );
        }


        if (weapon == WeaponType.Pistol)
        {
            Debug.Log(
                "Pistol Ammo: " +
                PistolAmmo +
                " / " +
                PistolReserveAmmo
            );
        }


        Debug.Log(
            "======================================"
        );


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
                HasRifle = true;
                break;

            case WeaponType.Pistol:
                HasPistol = true;
                break;

            case WeaponType.Bat:
                HasBat = true;
                break;

            case WeaponType.Shovel:
                HasShovel = true;
                break;
        }
    }


    // =========================================================
    // ROTATE PLAYER TO CAMERA
    // =========================================================

    private void SmoothRotateToCamera()
    {
        if (fpsCamera == null)
            return;


        Vector3 direction =
            fpsCamera.transform.forward;


        direction.y = 0f;


        if (direction.sqrMagnitude < 0.01f)
            return;


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
    // STOP MUZZLE FLASH
    // =========================================================

    private void StopAllMuzzleFlash()
    {
        if (rifleMuzzleFlash != null)
        {
            var main =
                rifleMuzzleFlash.main;

            main.playOnAwake = false;


            rifleMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }


        if (pistolMuzzleFlash != null)
        {
            var main =
                pistolMuzzleFlash.main;

            main.playOnAwake = false;


            pistolMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );
        }
    }


    // =========================================================
    // ADD RIFLE AMMO
    // =========================================================

    public void AddRifleAmmo(int amount)
    {
        if (!HasStateAuthority)
            return;


        if (amount <= 0)
            return;


        RifleReserveAmmo += amount;


        Debug.Log(
            "Nhặt " +
            amount +
            " đạn Rifle"
        );
    }


    // =========================================================
    // ADD PISTOL AMMO
    // =========================================================

    public void AddPistolAmmo(int amount)
    {
        if (!HasStateAuthority)
            return;


        if (amount <= 0)
            return;


        PistolReserveAmmo += amount;


        Debug.Log(
            "Nhặt " +
            amount +
            " đạn Pistol"
        );
    }
}