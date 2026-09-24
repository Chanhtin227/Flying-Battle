using UnityEngine;
using System.Collections;
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

    [Header("Interaction Settings")]
    public float interactionRadius = 3f;

    [Header("Shoot Rotation")]
    public float shootRotateSpeed = 10f;

    [Header("Ammo")]
    public int rifleMagazineSize = 30;
    public int pistolMagazineSize = 12;
    public int rifleStartAmmo = 30;
    public int pistolStartAmmo = 12;
    public int rifleStartReserveAmmo = 90;
    public int pistolStartReserveAmmo = 36;

    [Header("Network Ammo")]
    [Networked] public int RifleAmmo { get; set; }
    [Networked] public int PistolAmmo { get; set; }
    [Networked] public int RifleReserveAmmo { get; set; }
    [Networked] public int PistolReserveAmmo { get; set; }

    [Header("Reload")]
    [Networked] public NetworkBool IsReloading { get; set; }
    public float rifleReloadTime = 2f;
    public float pistolReloadTime = 1.5f;

    [Header("Weapons")]
    public GameObject gun;
    public GameObject pistol;
    public GameObject bat;
    public GameObject shovel;

    [Header("Network Weapon State")]
    [Networked, OnChangedRender(nameof(OnCurrentWeaponChanged))]
    public WeaponType CurrentWeapon { get; set; }
    [Networked] public NetworkBool HasRifle { get; set; }
    [Networked] public NetworkBool HasPistol { get; set; }
    [Networked] public NetworkBool HasBat { get; set; }
    [Networked] public NetworkBool HasShovel { get; set; }

    [Header("Inventory Weapons")]
    public int maxWeaponSlots = 4;
    [Networked] public WeaponType Slot1 { get; set; }
    [Networked] public WeaponType Slot2 { get; set; }
    [Networked] public WeaponType Slot3 { get; set; }
    [Networked] public WeaponType Slot4 { get; set; }

    [Header("Gun Settings")]
    public Camera fpsCamera;

    [Header("Rifle")]
    public float rifleDamage = 25f;
    public float rifleFireRate = 10f;
    public float rifleRange = 100f;

    [Header("Pistol")]
    public float pistolDamage = 15f;
    public float pistolFireRate = 4f;
    public float pistolRange = 70f;

    [Header("Bat")]
    public float batDamage = 30f;
    public float batRange = 2f;

    [Header("Shovel")]
    public float shovelDamage = 40f;
    public float shovelRange = 2.5f;

    [Header("Melee Settings")]
    public float meleeCooldown = 0.5f;
    private float nextMeleeTime = 0f;

    [Header("Effects")]
    public ParticleSystem rifleMuzzleFlash;
    public ParticleSystem pistolMuzzleFlash;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip rifleShotSound;
    public AudioClip pistolShotSound;
    public AudioClip batHitSound;
    public AudioClip shovelHitSound;
    public AudioClip rifleReloadSound;
    public AudioClip pistolReloadSound;

    [Header("Bullet")]
    public GameObject tracerPrefab;
    public Transform firePoint;

    private float nextFireTime;
    private PlayerAnimation playerAnim;
    private bool canShoot = false;

    private int lastShootSoundTick = -1;
    private int lastMeleeSoundTick = -1;
    private int lastReloadSoundTick = -1;

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

    private void Update()
    {
        if (!HasInputAuthority)
            return;

        SwitchWeapon();
        SmoothRotateToCamera();
        ShootInput();
        MeleeInput();
        InteractInput();

        if (Input.GetKeyDown(KeyCode.R))
        {
            if (!IsReloading)
            {
                RequestReloadRpc();
            }
        }
    }

    // =========================================================
    // INTERACTION INPUT (E)
    // =========================================================
    private void InteractInput()
    {
        if (!HasInputAuthority || IsReloading)
            return;

        if (Input.GetKeyDown(KeyCode.E))
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, interactionRadius);
            
            float closestDist = float.MaxValue;
            WeaponChest closestChest = null;
            WeaponPickup closestPickup = null;

            foreach (var hit in colliders)
            {
                WeaponChest chest = hit.GetComponentInParent<WeaponChest>();
                if (chest != null)
                {
                    float d = Vector3.Distance(transform.position, chest.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestChest = chest;
                        closestPickup = null;
                    }
                }

                WeaponPickup pickup = hit.GetComponentInParent<WeaponPickup>();
                if (pickup != null)
                {
                    float d = Vector3.Distance(transform.position, pickup.transform.position);
                    if (d < closestDist)
                    {
                        closestDist = d;
                        closestPickup = pickup;
                        closestChest = null;
                    }
                }
            }

            if (closestChest != null && closestChest.Object != null)
            {
                if (HasStateAuthority)
                {
                    // Host tự mở trực tiếp luôn, không gửi RPC mạng làm gì
                    closestChest.TryOpenFrom(this);
                }
                else
                {
                    // Client gửi tín hiệu nhờ Host mở, gửi NetworkId thay vì Object
                    RequestChestOpenRpc(closestChest.Object.Id);
                }
            }
            else if (closestPickup != null && closestPickup.Object != null)
            {
                if (HasStateAuthority)
                {
                    // Host tự nhặt trực tiếp luôn
                    closestPickup.TryPickupFrom(this);
                }
                else
                {
                    // Client gửi tín hiệu nhờ Host nhặt, gửi NetworkId thay vì Object
                    RequestWeaponPickupRpc(closestPickup.Object.Id);
                }
            }
        }
    }

    // =========================================================
    // RPC MỞ RƯƠNG (SỬ DỤNG NETWORK_ID ĐỂ TRÁNH LỖI)
    // =========================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestChestOpenRpc(NetworkId chestId, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        if (Runner.TryFindObject(chestId, out NetworkObject chestObj))
        {
            WeaponChest chest = chestObj.GetComponent<WeaponChest>();
            if (chest != null)
            {
                chest.TryOpenFrom(this);
            }
        }
    }

    // =========================================================
    // RPC NHẶT ĐỒ (SỬ DỤNG NETWORK_ID ĐỂ TRÁNH LỖI)
    // =========================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RequestWeaponPickupRpc(NetworkId pickupId, RpcInfo info = default)
    {
        if (!HasStateAuthority) return;

        if (Runner.TryFindObject(pickupId, out NetworkObject pickupObj))
        {
            WeaponPickup pickup = pickupObj.GetComponent<WeaponPickup>();
            if (pickup != null)
            {
                pickup.TryPickupFrom(this);
            }
        }
    }


    private void SwitchWeapon()
    {
        if (!HasInputAuthority) return;
        if (IsReloading) return;

        if (Input.GetKeyDown(KeyCode.Alpha1)) EquipSlot(Slot1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) EquipSlot(Slot2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) EquipSlot(Slot3);
        if (Input.GetKeyDown(KeyCode.Alpha4)) EquipSlot(Slot4);
    }

    private void EquipSlot(WeaponType weapon)
    {
        if (weapon == WeaponType.None) return;
        if (!HasWeapon(weapon)) return;

        canShoot = false;
        nextFireTime = Time.time + 0.1f;

        RequestSwitchWeaponRpc(weapon);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestSwitchWeaponRpc(WeaponType weapon)
    {
        if (!HasStateAuthority || IsReloading || !HasWeapon(weapon))
            return;

        CurrentWeapon = weapon;
    }

    private void ShootInput()
    {
        if (!HasInputAuthority || IsReloading) return;
        if (CurrentWeapon != WeaponType.Rifle && CurrentWeapon != WeaponType.Pistol) return;
        if (fpsCamera == null) return;

        if (!canShoot)
        {
            if (Input.GetMouseButtonUp(0))
            {
                canShoot = true;
            }
            return;
        }

        if (!Input.GetMouseButton(0)) return;

        float rate = CurrentWeapon == WeaponType.Rifle ? rifleFireRate : pistolFireRate;

        if (Time.time < nextFireTime) return;

        nextFireTime = Time.time + 1f / rate;
        Vector3 shootDirection = fpsCamera.transform.forward;

        RequestShootRpc(shootDirection);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestShootRpc(Vector3 shootDirection)
    {
        if (!HasStateAuthority || IsReloading || firePoint == null)
            return;

        shootDirection.Normalize();

        if (CurrentWeapon == WeaponType.Rifle)
        {
            if (RifleAmmo <= 0) return;
            RifleAmmo--;
            ServerShoot(shootDirection, rifleDamage, rifleRange);
            return;
        }

        if (CurrentWeapon == WeaponType.Pistol)
        {
            if (PistolAmmo <= 0) return;
            PistolAmmo--;
            ServerShoot(shootDirection, pistolDamage, pistolRange);
        }
    }

    private void ServerShoot(Vector3 shootDirection, float damage, float range)
    {
        if (!HasStateAuthority || firePoint == null)
            return;

        shootDirection.Normalize();
        Vector3 origin = firePoint.position;
        Vector3 targetPoint = origin + shootDirection * range;
        Ray ray = new Ray(origin, shootDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPoint = hit.point;
            PlayerHealth target = hit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null)
            {
                PlayerHealth ownHealth = GetComponentInParent<PlayerHealth>();
                if (target != ownHealth)
                {
                    target.TakeDamage(damage);
                }
            }
        }

        Rpc_PlayShootEffects(origin, targetPoint, CurrentWeapon);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayShootEffects(Vector3 startPoint, Vector3 targetPoint, WeaponType weapon)
    {
        if (playerAnim != null)
        {
            playerAnim.Shoot();
        }

        if (weapon == WeaponType.Rifle)
        {
            if (rifleMuzzleFlash != null)
            {
                rifleMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                rifleMuzzleFlash.Play();
            }
        }
        else if (weapon == WeaponType.Pistol)
        {
            if (pistolMuzzleFlash != null)
            {
                pistolMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                pistolMuzzleFlash.Play();
            }
        }

        PlayShootSoundOnce(weapon);

        if (tracerPrefab != null)
        {
            Vector3 direction = targetPoint - startPoint;
            if (direction.sqrMagnitude > 0.001f)
            {
                GameObject tracer = Instantiate(tracerPrefab, startPoint, Quaternion.LookRotation(direction));
                BulletTracer bulletTracer = tracer.GetComponent<BulletTracer>();
                if (bulletTracer != null)
                {
                    bulletTracer.Fire(startPoint, targetPoint);
                }
            }
        }
    }

    private void PlayShootSoundOnce(WeaponType weapon)
    {
        if (audioSource == null) return;

        int currentTick = Runner.Tick.Raw;
        if (lastShootSoundTick == currentTick) return;

        lastShootSoundTick = currentTick;

        if (weapon == WeaponType.Rifle && rifleShotSound != null)
        {
            audioSource.PlayOneShot(rifleShotSound);
        }
        else if (weapon == WeaponType.Pistol && pistolShotSound != null)
        {
            audioSource.PlayOneShot(pistolShotSound);
        }
    }

    private void MeleeInput()
    {
        if (!HasInputAuthority || IsReloading) return;
        if (CurrentWeapon != WeaponType.Bat && CurrentWeapon != WeaponType.Shovel) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (Time.time < nextMeleeTime) return;
        if (fpsCamera == null) return;

        nextMeleeTime = Time.time + meleeCooldown;
        Vector3 attackDirection = fpsCamera.transform.forward;

        RequestMeleeRpc(attackDirection);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestMeleeRpc(Vector3 attackDirection)
    {
        if (!HasStateAuthority || IsReloading) return;

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
        else return;

        attackDirection.Normalize();

        Vector3 origin = firePoint != null ? firePoint.position : transform.position;
        Vector3 targetPoint = origin + attackDirection * range;
        Ray ray = new Ray(origin, attackDirection);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            targetPoint = hit.point;
            PlayerHealth target = hit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null)
            {
                PlayerHealth ownHealth = GetComponentInParent<PlayerHealth>();
                if (target != ownHealth)
                {
                    target.TakeDamage(damage);
                }
            }
        }

        Rpc_PlayMeleeEffects(CurrentWeapon);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayMeleeEffects(WeaponType weapon)
    {
        if (playerAnim != null)
        {
            playerAnim.MeleeAttack();
        }
        PlayMeleeSoundOnce(weapon);
    }

    private void PlayMeleeSoundOnce(WeaponType weapon)
    {
        if (audioSource == null) return;

        int currentTick = Runner.Tick.Raw;
        if (lastMeleeSoundTick == currentTick) return;

        lastMeleeSoundTick = currentTick;

        if (weapon == WeaponType.Bat && batHitSound != null)
        {
            audioSource.PlayOneShot(batHitSound);
        }
        else if (weapon == WeaponType.Shovel && shovelHitSound != null)
        {
            audioSource.PlayOneShot(shovelHitSound);
        }
    }

    private void UpdateWeaponVisibility()
    {
        if (gun != null) gun.SetActive(CurrentWeapon == WeaponType.Rifle);
        if (pistol != null) pistol.SetActive(CurrentWeapon == WeaponType.Pistol);
        if (bat != null) bat.SetActive(CurrentWeapon == WeaponType.Bat);
        if (shovel != null) shovel.SetActive(CurrentWeapon == WeaponType.Shovel);
        if (playerAnim != null) playerAnim.SetMelee(CurrentWeapon == WeaponType.Bat || CurrentWeapon == WeaponType.Shovel);
    }

    private void OnCurrentWeaponChanged()
    {
        UpdateWeaponVisibility();
    }

    private bool HasWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle: return HasRifle;
            case WeaponType.Pistol: return HasPistol;
            case WeaponType.Bat: return HasBat;
            case WeaponType.Shovel: return HasShovel;
        }
        return false;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RequestReloadRpc()
    {
        if (!HasStateAuthority || IsReloading) return;
        if (CurrentWeapon != WeaponType.Rifle && CurrentWeapon != WeaponType.Pistol) return;

        if (CurrentWeapon == WeaponType.Rifle)
        {
            if (RifleAmmo >= rifleMagazineSize || RifleReserveAmmo <= 0) return;
        }

        if (CurrentWeapon == WeaponType.Pistol)
        {
            if (PistolAmmo >= pistolMagazineSize || PistolReserveAmmo <= 0) return;
        }

        IsReloading = true;
        WeaponType reloadWeapon = CurrentWeapon;

        Rpc_PlayReloadSound(reloadWeapon);
        StartCoroutine(ReloadStateAuthority(reloadWeapon));
    }

    private IEnumerator ReloadStateAuthority(WeaponType reloadWeapon)
    {
        float reloadTime = reloadWeapon == WeaponType.Rifle ? rifleReloadTime : pistolReloadTime;
        yield return new WaitForSeconds(reloadTime);

        if (!HasStateAuthority) yield break;

        if (reloadWeapon == WeaponType.Rifle)
        {
            int need = rifleMagazineSize - RifleAmmo;
            int load = Mathf.Min(need, RifleReserveAmmo);
            RifleAmmo += load;
            RifleReserveAmmo -= load;
        }
        else if (reloadWeapon == WeaponType.Pistol)
        {
            int need = pistolMagazineSize - PistolAmmo;
            int load = Mathf.Min(need, PistolReserveAmmo);
            PistolAmmo += load;
            PistolReserveAmmo -= load;
        }

        IsReloading = false;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayReloadSound(WeaponType weapon)
    {
        PlayReloadSoundOnce(weapon);
    }

    private void PlayReloadSoundOnce(WeaponType weapon)
    {
        if (audioSource == null) return;

        int currentTick = Runner.Tick.Raw;
        if (lastReloadSoundTick == currentTick) return;

        lastReloadSoundTick = currentTick;

        if (weapon == WeaponType.Rifle && rifleReloadSound != null)
        {
            audioSource.PlayOneShot(rifleReloadSound);
        }
        else if (weapon == WeaponType.Pistol && pistolReloadSound != null)
        {
            audioSource.PlayOneShot(pistolReloadSound);
        }
    }

    public bool ServerPickupWeapon(WeaponType weapon)
    {
        if (!HasStateAuthority) return false;
        if (weapon == WeaponType.None) return false;
        if (IsReloading) return false;
        if (HasWeapon(weapon)) return false;

        int slotCount = 0;
        if (Slot1 != WeaponType.None) slotCount++;
        if (Slot2 != WeaponType.None) slotCount++;
        if (Slot3 != WeaponType.None) slotCount++;
        if (Slot4 != WeaponType.None) slotCount++;

        if (slotCount >= maxWeaponSlots) return false;

        if (Slot1 == WeaponType.None) Slot1 = weapon;
        else if (Slot2 == WeaponType.None) Slot2 = weapon;
        else if (Slot3 == WeaponType.None) Slot3 = weapon;
        else if (Slot4 == WeaponType.None) Slot4 = weapon;
        else return false;

        SetWeaponOwned(weapon);

        if (weapon == WeaponType.Rifle)
        {
            RifleAmmo = rifleStartAmmo;
            RifleReserveAmmo = rifleStartReserveAmmo;
        }

        if (weapon == WeaponType.Pistol)
        {
            PistolAmmo = pistolStartAmmo;
            PistolReserveAmmo = pistolStartReserveAmmo;
        }

        CurrentWeapon = weapon;
        canShoot = false;
        nextFireTime = Time.time + 0.1f;

        return true;
    }

    private void SetWeaponOwned(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle: HasRifle = true; break;
            case WeaponType.Pistol: HasPistol = true; break;
            case WeaponType.Bat: HasBat = true; break;
            case WeaponType.Shovel: HasShovel = true; break;
        }
    }

    private void SmoothRotateToCamera()
    {
        if (fpsCamera == null) return;
        Vector3 direction = fpsCamera.transform.forward;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, shootRotateSpeed * Time.deltaTime);
    }

    private void StopAllMuzzleFlash()
    {
        if (rifleMuzzleFlash != null)
        {
            var main = rifleMuzzleFlash.main;
            main.playOnAwake = false;
            rifleMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        if (pistolMuzzleFlash != null)
        {
            var main = pistolMuzzleFlash.main;
            main.playOnAwake = false;
            pistolMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void AddRifleAmmo(int amount)
    {
        if (!HasStateAuthority || amount <= 0) return;
        RifleReserveAmmo += amount;
    }

    public void AddPistolAmmo(int amount)
    {
        if (!HasStateAuthority || amount <= 0) return;
        PistolReserveAmmo += amount;
    }
}