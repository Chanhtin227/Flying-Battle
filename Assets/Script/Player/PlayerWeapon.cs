using UnityEngine;

public class PlayerWeapon : MonoBehaviour
{
    public enum WeaponType
    {
        None,
        Rifle,
        Pistol,
        Bat,
        Shovel
    }

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

    public GameObject tracerPrefab;
    public Transform firePoint;

    float nextFireTime;

    PlayerAnimation playerAnim;

    void Start()
    {
        playerAnim = GetComponent<PlayerAnimation>();

        gun.SetActive(false);
        pistol.SetActive(false);
        bat.SetActive(false);
        shovel.SetActive(false);

        playerAnim.SetMelee(false);
    }

    void Update()
    {
        SwitchWeapon();

        ShootInput();

        MeleeInput();

        if (Input.GetKeyDown(KeyCode.H))
            playerAnim.Hit();

        if (Input.GetKeyDown(KeyCode.K))
            playerAnim.Die();
    }

    void SwitchWeapon()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1) && hasRifle)
            currentWeapon = WeaponType.Rifle;

        if (Input.GetKeyDown(KeyCode.Alpha2) && hasPistol)
            currentWeapon = WeaponType.Pistol;

        if (Input.GetKeyDown(KeyCode.Alpha3) && hasBat)
            currentWeapon = WeaponType.Bat;

        if (Input.GetKeyDown(KeyCode.Alpha4) && hasShovel)
            currentWeapon = WeaponType.Shovel;

        gun.SetActive(currentWeapon == WeaponType.Rifle);
        pistol.SetActive(currentWeapon == WeaponType.Pistol);
        bat.SetActive(currentWeapon == WeaponType.Bat);
        shovel.SetActive(currentWeapon == WeaponType.Shovel);

        playerAnim.SetMelee(
            currentWeapon == WeaponType.Bat ||
            currentWeapon == WeaponType.Shovel);
    }

    void ShootInput()
    {
        if (currentWeapon != WeaponType.Rifle &&
            currentWeapon != WeaponType.Pistol)
            return;

        if (Input.GetMouseButton(0) && Time.time >= nextFireTime)
        {
            float rate = currentWeapon == WeaponType.Rifle
                ? rifleFireRate
                : pistolFireRate;

            nextFireTime = Time.time + 1f / rate;

            playerAnim.Shoot();

            Shoot();
        }
    }

    void MeleeInput()
    {
        if (currentWeapon != WeaponType.Bat &&
            currentWeapon != WeaponType.Shovel)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            playerAnim.MeleeAttack();

            if (audioSource != null)
            {
                if (currentWeapon == WeaponType.Bat)
                    audioSource.PlayOneShot(batHitSound);
                else
                    audioSource.PlayOneShot(shovelHitSound);
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
        }
        // Hiệu ứng đầu nòng
        if (currentWeapon == WeaponType.Rifle && rifleMuzzleFlash != null)
        {
            rifleMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            rifleMuzzleFlash.Play();
        }

        if (currentWeapon == WeaponType.Pistol && pistolMuzzleFlash != null)
        {
            pistolMuzzleFlash.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            pistolMuzzleFlash.Play();
        }

        // Phát âm thanh
        if (audioSource != null)
        {
            if (currentWeapon == WeaponType.Rifle && rifleShotSound != null)
                audioSource.PlayOneShot(rifleShotSound);

            if (currentWeapon == WeaponType.Pistol && pistolShotSound != null)
                audioSource.PlayOneShot(pistolShotSound);
        }

        RaycastHit cameraHit;
        Vector3 targetPoint;

        // Raycast từ camera để xác định điểm ngắm
        if (Physics.Raycast(fpsCamera.transform.position,
                            fpsCamera.transform.forward,
                            out cameraHit,
                            currentRange))
        {
            targetPoint = cameraHit.point;
            Debug.Log("Trúng: " + cameraHit.collider.name);
        }
        else
        {
            targetPoint = fpsCamera.transform.position +
                          fpsCamera.transform.forward * currentRange;
        }

        // Tính hướng bắn từ đầu nòng đến mục tiêu
        Vector3 shootDirection = (targetPoint - firePoint.position).normalized;

        // Raycast từ đầu nòng để tránh bắn xuyên tường gần người chơi
        RaycastHit gunHit;

        if (Physics.Raycast(firePoint.position,
                            shootDirection,
                            out gunHit,
                            currentRange))
        {
            targetPoint = gunHit.point;
        }

        // Hiệu ứng tia đạn
        if (tracerPrefab != null && firePoint != null)
        {
            GameObject tracer = Instantiate(
                tracerPrefab,
                firePoint.position,
                Quaternion.LookRotation(shootDirection));

            BulletTracer bulletTracer = tracer.GetComponent<BulletTracer>();

            if (bulletTracer != null)
            {
                bulletTracer.Fire(firePoint.position, targetPoint);
            }
        }
    }
    public void PickupWeapon(WeaponType weapon)
    {
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

        currentWeapon = weapon;

        gun.SetActive(currentWeapon == WeaponType.Rifle);
        pistol.SetActive(currentWeapon == WeaponType.Pistol);
        bat.SetActive(currentWeapon == WeaponType.Bat);
        shovel.SetActive(currentWeapon == WeaponType.Shovel);

        playerAnim.SetMelee(
            currentWeapon == WeaponType.Bat ||
            currentWeapon == WeaponType.Shovel);
    }
}