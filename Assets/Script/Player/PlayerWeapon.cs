using UnityEngine;
using TMPro;
using System.Collections;
using System;

public class PlayerWeapon : MonoBehaviour
{
    public enum WeaponType { None, Rifle, Pistol, Bat, Shovel }

[Header("Ammo")]

    public int rifleMagazineSize = 30;
    public int rifleAmmo = 30;

    public int pistolMagazineSize = 12;
    public int pistolAmmo = 12;

    public int rifleReserveAmmo = 90;
    public int pistolReserveAmmo = 36;

    [Header("Ammo UI")]
    public TMP_Text rifleAmmoText;
    public TMP_Text pistolAmmoText;

    [Header("Melee UI")]
    public TMP_Text batUI;
    public TMP_Text shovelUI;

    bool isReloading = false;

    [Header("Reload Time")]
    public float rifleReloadTime = 2f;
    public float pistolReloadTime = 1.5f;

    [Header("Weapons")]
    public GameObject gun;
    public GameObject pistol;
    public GameObject bat;
    public GameObject shovel;

    public WeaponType currentWeapon = WeaponType.None;
    bool hasRifle = false;
    bool hasPistol = false;
    bool hasBat = false;
    bool hasShovel = false;

    [Header("Inventory Weapons")]
    public int maxWeaponSlots = 2;

    private int weaponCount = 0;

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

    public GameObject tracerPrefab;
    public Transform firePoint;

    float nextFireTime;

    PlayerAnimation playerAnim;

    void Start()
    {
        playerAnim = GetComponent<PlayerAnimation>();

        // Nếu player đã có vũ khí sẵn
        if (currentWeapon != WeaponType.None)
        {
            // Đưa vũ khí ban đầu vào Slot 1
            slot1 = currentWeapon;
            weaponCount = 1;

            // Đánh dấu đã sở hữu
            switch (currentWeapon)
            {
                case WeaponType.Rifle:
                    hasRifle = true;
                    break;

                case WeaponType.Pistol:
                    hasPistol = true;
                    break;

                case WeaponType.Bat:
                    hasBat = true;
                    break;

                case WeaponType.Shovel:
                    hasShovel = true;
                    break;
            }

            UpdateWeaponVisibility();
        }
        else
        {
            // Không có vũ khí ban đầu
            gun.SetActive(false);
            pistol.SetActive(false);
            bat.SetActive(false);
            shovel.SetActive(false);

            if (playerAnim != null)
                playerAnim.SetMelee(false);
        }
    }

    void Update()
    {
        SwitchWeapon();

        ShootInput();

        MeleeInput();

        UpdateAmmoUI();

        if (Input.GetKeyDown(KeyCode.R) && !isReloading)
        {
            StartCoroutine(Reload());
        }
    }

    private WeaponType slot1 = WeaponType.None;
    private WeaponType slot2 = WeaponType.None;

    void SwitchWeapon()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            if (slot1 != WeaponType.None)
            {
                currentWeapon = slot1;
                UpdateWeaponVisibility();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            if (slot2 != WeaponType.None)
            {
                currentWeapon = slot2;
                UpdateWeaponVisibility();
            }
        }
    }

    void ShootInput()
    {

        if (isReloading)
            return;

        if (currentWeapon != WeaponType.Rifle &&
            currentWeapon != WeaponType.Pistol)
            return;


        if (currentWeapon != WeaponType.Rifle &&
            currentWeapon != WeaponType.Pistol)
            return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            if (currentWeapon == WeaponType.Rifle)
            {
                if (rifleAmmo <= 0)
                    return;

                rifleAmmo--;
            }
            else
            {
                if (pistolAmmo <= 0)
                    return;

                pistolAmmo--;
            }

            float rate = currentWeapon == WeaponType.Rifle
                ? rifleFireRate
                : pistolFireRate;

            nextFireTime = Time.time + 1f / rate;

            playerAnim.Shoot();

            Shoot();

            UpdateAmmoUI();
        }
    }

    void MeleeInput()
    {
        if (currentWeapon != WeaponType.Bat &&
            currentWeapon != WeaponType.Shovel)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Animation
            if (playerAnim != null)
                playerAnim.MeleeAttack();

            // Âm thanh
            if (audioSource != null)
            {
                if (currentWeapon == WeaponType.Bat)
                {
                    if (batHitSound != null)
                        audioSource.PlayOneShot(batHitSound);
                }
                else
                {
                    if (shovelHitSound != null)
                        audioSource.PlayOneShot(shovelHitSound);
                }
            }

            // Gây damage
            MeleeAttack();
        }
    }
    void MeleeAttack()
    {
        float damage = 0f;
        float range = 0f;

        // Damage của từng vũ khí
        if (currentWeapon == WeaponType.Bat)
        {
            damage = batDamage;
            range = batRange;
        }
        else if (currentWeapon == WeaponType.Shovel)
        {
            damage = shovelDamage;
            range = shovelRange;
        }

        RaycastHit hit;

        if (Physics.Raycast(
            fpsCamera.transform.position,
            fpsCamera.transform.forward,
            out hit,
            range))
        {
            Debug.Log("Cận chiến trúng: " + hit.collider.name);

            // Tìm PlayerHealth
            PlayerHealth target =
                hit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null)
            {
                // Không đánh chính mình
                if (target.gameObject != gameObject)
                {
                    target.TakeDamage(damage);

                    Debug.Log(
                        currentWeapon +
                        " đánh Player, Damage: " +
                        damage
                    );
                }
            }
        }
    }

    void Shoot()
    {
        float currentDamage = 0f;
        float currentRange = 0f;

        switch (currentWeapon)
        {
            case WeaponType.Rifle:
                currentDamage = rifleDamage;
                currentRange = rifleRange;
                break;

            case WeaponType.Pistol:
                currentDamage = pistolDamage;
                currentRange = pistolRange;
                break;

            default:
                return;
        }

        // =========================
        // MUZZLE FLASH
        // =========================

        if (currentWeapon == WeaponType.Rifle && rifleMuzzleFlash != null)
        {
            rifleMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            rifleMuzzleFlash.Play();
        }

        if (currentWeapon == WeaponType.Pistol && pistolMuzzleFlash != null)
        {
            pistolMuzzleFlash.Stop(
                true,
                ParticleSystemStopBehavior.StopEmittingAndClear
            );

            pistolMuzzleFlash.Play();
        }

        // =========================
        // SOUND
        // =========================

        if (audioSource != null)
        {
            if (currentWeapon == WeaponType.Rifle &&
                rifleShotSound != null)
            {
                audioSource.PlayOneShot(rifleShotSound);
            }

            if (currentWeapon == WeaponType.Pistol &&
                pistolShotSound != null)
            {
                audioSource.PlayOneShot(pistolShotSound);
            }
        }

        // =========================
        // RAYCAST TỪ CAMERA
        // =========================

        RaycastHit cameraHit;

        Vector3 targetPoint;

        if (Physics.Raycast(
            fpsCamera.transform.position,
            fpsCamera.transform.forward,
            out cameraHit,
            currentRange))
        {
            targetPoint = cameraHit.point;

            Debug.Log("Camera trúng: " + cameraHit.collider.name);
        }
        else
        {
            targetPoint =
                fpsCamera.transform.position +
                fpsCamera.transform.forward * currentRange;
        }

        // =========================
        // HƯỚNG TỪ NÒNG SÚNG
        // =========================

        Vector3 shootDirection =
            (targetPoint - firePoint.position).normalized;

        // =========================
        // RAYCAST TỪ NÒNG SÚNG
        // =========================

        RaycastHit gunHit;

        if (Physics.Raycast(
            firePoint.position,
            shootDirection,
            out gunHit,
            currentRange))
        {
            targetPoint = gunHit.point;

            Debug.Log("Đạn trúng: " + gunHit.collider.name);

            // =========================
            // GÂY DAMAGE PLAYER
            // =========================

            PlayerHealth target =
                gunHit.collider.GetComponentInParent<PlayerHealth>();

            if (target != null)
            {
                // Không gây damage cho chính mình
                if (target.gameObject != gameObject)
                {
                    target.TakeDamage(currentDamage);

                    Debug.Log(
                        "Bắn trúng Player khác! Damage: "
                        + currentDamage
                    );
                }
            }
        }

        // =========================
        // BULLET TRACER
        // =========================

        if (tracerPrefab != null && firePoint != null)
        {
            GameObject tracer = Instantiate(
                tracerPrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDirection)
            );

            BulletTracer bulletTracer =
                tracer.GetComponent<BulletTracer>();

            if (bulletTracer != null)
            {
                bulletTracer.Fire(
                    firePoint.position,
                    targetPoint
                );
            }
        }
    }
    void UpdateWeaponVisibility()
    {
        if (gun != null)
            gun.SetActive(currentWeapon == WeaponType.Rifle);

        if (pistol != null)
            pistol.SetActive(currentWeapon == WeaponType.Pistol);

        if (bat != null)
            bat.SetActive(currentWeapon == WeaponType.Bat);

        if (shovel != null)
            shovel.SetActive(currentWeapon == WeaponType.Shovel);

        if (playerAnim != null)
        {
            playerAnim.SetMelee(
                currentWeapon == WeaponType.Bat ||
                currentWeapon == WeaponType.Shovel
            );
        }
    }

    bool HasWeapon(WeaponType weapon)
    {
        switch (weapon)
        {
            case WeaponType.Rifle:
                return hasRifle;

            case WeaponType.Pistol:
                return hasPistol;

            case WeaponType.Bat:
                return hasBat;

            case WeaponType.Shovel:
                return hasShovel;
        }

        return false;
    }
    void UpdateAmmoUI()
    {
        // Ẩn tất cả UI trước
        if (rifleAmmoText != null)
            rifleAmmoText.gameObject.SetActive(false);

        if (pistolAmmoText != null)
            pistolAmmoText.gameObject.SetActive(false);

        if (batUI != null)
            batUI.gameObject.SetActive(false);

        if (shovelUI != null)
            shovelUI.gameObject.SetActive(false);


        // =========================
        // RIFLE
        // =========================
        if (currentWeapon == WeaponType.Rifle)
        {
            if (rifleAmmoText != null)
            {
                rifleAmmoText.gameObject.SetActive(true);

                if (isReloading)
                    rifleAmmoText.text = "Reloading!";
                else
                    rifleAmmoText.text = rifleAmmo + " / " + rifleReserveAmmo;
            }
        }


        // =========================
        // PISTOL
        // =========================
        else if (currentWeapon == WeaponType.Pistol)
        {
            if (pistolAmmoText != null)
            {
                pistolAmmoText.gameObject.SetActive(true);

                if (isReloading)
                    pistolAmmoText.text = "Reloading!";
                else
                    pistolAmmoText.text = pistolAmmo + " / " + pistolReserveAmmo;
            }
        }


        // =========================
        // BAT - GẬY
        // =========================
        else if (currentWeapon == WeaponType.Bat)
        {
            if (batUI != null)
            {
                batUI.gameObject.SetActive(true);
                batUI.text = "BAT";
            }
        }


        // =========================
        // SHOVEL - XẺNG
        // =========================
        else if (currentWeapon == WeaponType.Shovel)
        {
            if (shovelUI != null)
            {
                shovelUI.gameObject.SetActive(true);
                shovelUI.text = "SHOVEL";
            }
        }
    }
    IEnumerator Reload()
    {
        // Chỉ Rifle hoặc Pistol mới được reload
        if (currentWeapon != WeaponType.Rifle &&
            currentWeapon != WeaponType.Pistol)
            yield break;

        // Kiểm tra Rifle
        if (currentWeapon == WeaponType.Rifle)
        {
            if (rifleAmmo >= rifleMagazineSize)
                yield break;

            if (rifleReserveAmmo <= 0)
                yield break;
        }

        // Kiểm tra Pistol
        if (currentWeapon == WeaponType.Pistol)
        {
            if (pistolAmmo >= pistolMagazineSize)
                yield break;

            if (pistolReserveAmmo <= 0)
                yield break;
        }

        isReloading = true;
        UpdateAmmoUI();

        // Âm thanh reload
        if (audioSource != null)
        {
            if (currentWeapon == WeaponType.Rifle &&
                rifleReloadSound != null)
            {
                audioSource.PlayOneShot(rifleReloadSound);
            }

            if (currentWeapon == WeaponType.Pistol &&
                pistolReloadSound != null)
            {
                audioSource.PlayOneShot(pistolReloadSound);
            }
        }

        float reloadTime = currentWeapon == WeaponType.Rifle
            ? rifleReloadTime
            : pistolReloadTime;

        yield return new WaitForSeconds(reloadTime);

        // Nạp Rifle
        if (currentWeapon == WeaponType.Rifle)
        {
            int need = rifleMagazineSize - rifleAmmo;
            int load = Mathf.Min(need, rifleReserveAmmo);

            rifleAmmo += load;
            rifleReserveAmmo -= load;
        }

        // Nạp Pistol
        else if (currentWeapon == WeaponType.Pistol)
        {
            int need = pistolMagazineSize - pistolAmmo;
            int load = Mathf.Min(need, pistolReserveAmmo);

            pistolAmmo += load;
            pistolReserveAmmo -= load;
        }

        isReloading = false;

        UpdateAmmoUI();
    }

    public bool PickupWeapon(WeaponType weapon)
    {
        // Đã có vũ khí này rồi
        if (HasWeapon(weapon))
        {
            Debug.Log("Bạn đã có " + weapon);
            return false;
        }

        // Kiểm tra đủ 2 slot
        if (slot1 != WeaponType.None && slot2 != WeaponType.None)
        {
            Debug.Log("Đã đủ 2 vũ khí!");
            return false;
        }

        // Thêm vào slot 1
        if (slot1 == WeaponType.None)
        {
            slot1 = weapon;
        }
        // Nếu slot 1 đầy thì thêm slot 2
        else if (slot2 == WeaponType.None)
        {
            slot2 = weapon;
        }

        // Đánh dấu đã sở hữu
        switch (weapon)
        {
            case WeaponType.Rifle:
                hasRifle = true;
                break;

            case WeaponType.Pistol:
                hasPistol = true;
                break;

            case WeaponType.Bat:
                hasBat = true;
                break;

            case WeaponType.Shovel:
                hasShovel = true;
                break;
        }

        weaponCount++;

        // Cầm ngay vũ khí vừa nhặt
        currentWeapon = weapon;

        UpdateWeaponVisibility();

        Debug.Log(
            "Đã nhặt: " + weapon +
            " | Slot 1: " + slot1 +
            " | Slot 2: " + slot2
        );

        return true;
    }

}
