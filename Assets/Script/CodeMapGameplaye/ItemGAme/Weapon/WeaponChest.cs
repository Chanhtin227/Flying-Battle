using UnityEngine;
using Fusion;
using System.Collections;

public class WeaponChest : NetworkBehaviour
{
    // =========================================================
    // WEAPONS
    // =========================================================

    [Header("Weapons")]

    public NetworkPrefabRef riflePrefab;
    public NetworkPrefabRef pistolPrefab;
    public NetworkPrefabRef batPrefab;
    public NetworkPrefabRef shovelPrefab;


    // =========================================================
    // AMMO
    // =========================================================

    [Header("Ammo")]

    public NetworkPrefabRef rifleAmmoPrefab;
    public NetworkPrefabRef pistolAmmoPrefab;


    // =========================================================
    // MEDKIT
    // =========================================================

    [Header("Medkits")]

    public NetworkPrefabRef smallMedkitPrefab;
    public NetworkPrefabRef mediumMedkitPrefab;
    public NetworkPrefabRef largeMedkitPrefab;


    // =========================================================
    // WEAPON SPAWN CHANCE
    // =========================================================

    [Header("Weapon Spawn Chance")]

    [Range(0f, 100f)]
    public float rifleChance = 25f;

    [Range(0f, 100f)]
    public float pistolChance = 20f;

    [Range(0f, 100f)]
    public float batChance = 15f;

    [Range(0f, 100f)]
    public float shovelChance = 10f;


    // =========================================================
    // AMMO SPAWN CHANCE
    // =========================================================

    [Header("Ammo Spawn Chance")]

    [Range(0f, 100f)]
    public float rifleAmmoChance = 10f;

    [Range(0f, 100f)]
    public float pistolAmmoChance = 5f;


    // =========================================================
    // MEDKIT SPAWN CHANCE
    // =========================================================

    [Header("Medkit Spawn Chance")]

    [Range(0f, 100f)]
    public float smallMedkitChance = 5f;

    [Range(0f, 100f)]
    public float mediumMedkitChance = 5f;

    [Range(0f, 100f)]
    public float largeMedkitChance = 5f;


    // =========================================================
    // FALL SETTINGS
    // =========================================================

    [Header("Fall Settings")]

    public float fallSpeed = 8f;
    public float rotationSpeed = 100f;


    // =========================================================
    // GROUND DETECTION
    // =========================================================

    [Header("Ground Detection")]

    public LayerMask groundLayer;


    // =========================================================
    // INTERACTION
    // =========================================================

    [Header("Interaction")]

    public KeyCode interactKey = KeyCode.F;
    public float interactDistance = 3f;


    // =========================================================
    // CHEST OPEN SOUND
    // =========================================================

    [Header("Chest Open Sound")]

    [Tooltip("Âm thanh phát khi player mở rương thành công.")]
    public AudioClip chestOpenSound;

    [Range(0f, 1f)]
    public float chestOpenVolume = 1f;


    // =========================================================
    // LIGHT BEAM
    // =========================================================

    [Header("Light Beam")]

    public GameObject lightBeam;


    // =========================================================
    // CHEST OPEN ANIMATION
    // =========================================================

    [Header("Chest Open Animation")]
    [Tooltip("Animator tren Chest_Animated (co the de trong de tu tim)")]
    public Animator chestAnimator;

    [Tooltip("Ten Trigger chuyen tu anim cho sang anim mo ruong")]
    public string openTriggerName = "Open";

    [Tooltip("Thoi gian cho animation mo ruong truoc khi spawn vat pham")]
    [Min(0f)]
    public float openAnimationDuration = 1.2f;

    // =========================================================
    // STATE
    // =========================================================

    private bool hasLanded = false;
    private bool opened = false;


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        base.Spawned();

        hasLanded = true;
        opened = false;

        if (chestAnimator == null)
            chestAnimator = GetComponentInChildren<Animator>(true);

        // Animator Controller phai co default state la Chest_Rotation
        // (hoac mot Idle), KHONG phai Chest_Shake / Chest_Open_Close.
        if (lightBeam != null)
        {
            lightBeam.SetActive(true);
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (
            Object == null ||
            !Object.IsValid ||
            opened
        )
        {
            return;
        }


        // =====================================================
        // XOAY RƯƠNG
        // Chỉ State Authority xoay
        // =====================================================

        if (HasStateAuthority)
        {
            if (!hasLanded)
            {
                Fall();
            }

            transform.Rotate(
                Vector3.up *
                rotationSpeed *
                Time.deltaTime,
                Space.World
            );
        }


        // =====================================================
        // NHẤN F ĐỂ MỞ RƯƠNG
        // =====================================================

        if (!HasInputAuthority)
        {
            // Không dùng HasInputAuthority của chest.
            // Chest không thuộc quyền input của player.
        }

        // Chỉ player local mới đọc phím
        if (PlayerMovement.LocalPlayer == null)
            return;

        if (!Input.GetKeyDown(interactKey))
            return;


        // =====================================================
        // LẤY PLAYER LOCAL
        // =====================================================

        PlayerWeapon playerWeapon =
            PlayerMovement.LocalPlayer.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
        {
            Debug.LogWarning(
                "[WeaponChest] Không tìm thấy PlayerWeapon!"
            );

            return;
        }


        // =====================================================
        // KIỂM TRA KHOẢNG CÁCH LOCAL
        // =====================================================

        float distance = Vector3.Distance(
            transform.position,
            playerWeapon.transform.position
        );

        if (distance > interactDistance)
        {
            return;
        }


        // =====================================================
        // GỬI RPC LÊN STATE AUTHORITY
        // =====================================================

        RequestOpenChestRpc(
            playerWeapon.Object
        );
    }


    // =========================================================
    // FALL
    // =========================================================

    private void Fall()
    {
        transform.position +=
            Vector3.down *
            fallSpeed *
            Time.deltaTime;


        Ray ray = new Ray(
            transform.position,
            Vector3.down
        );


        if (
            Physics.Raycast(
                ray,
                out RaycastHit hit,
                2f,
                groundLayer
            )
        )
        {
            transform.position =
                hit.point +
                Vector3.up *
                0.5f;

            hasLanded = true;
        }
    }


    // =========================================================
    // REQUEST OPEN CHEST RPC
    // =========================================================

    [Rpc(
        RpcSources.All,
        RpcTargets.StateAuthority
    )]
    private void RequestOpenChestRpc(
        NetworkObject playerObject,
        RpcInfo info = default
    )
    {
        if (!HasStateAuthority)
            return;


        if (
            Object == null ||
            !Object.IsValid ||
            opened
        )
        {
            return;
        }


        if (
            playerObject == null ||
            !playerObject.IsValid
        )
        {
            return;
        }


        // =====================================================
        // KIỂM TRA PLAYER GỬI RPC
        // =====================================================

        if (
            info.Source !=
            playerObject.InputAuthority
        )
        {
            Debug.LogWarning(
                "[WeaponChest] RPC không hợp lệ!"
            );

            return;
        }


        // =====================================================
        // LẤY PLAYER WEAPON
        // =====================================================

        PlayerWeapon playerWeapon =
            playerObject.GetComponent<PlayerWeapon>();

        if (playerWeapon == null)
        {
            Debug.LogWarning(
                "[WeaponChest] Player không có PlayerWeapon!"
            );

            return;
        }


        // =====================================================
        // KIỂM TRA KHOẢNG CÁCH TRÊN SERVER
        // =====================================================

        Vector2 objectPos =
            new Vector2(
                transform.position.x,
                transform.position.z
            );


        Vector2 playerPos =
            new Vector2(
                playerWeapon.transform.position.x,
                playerWeapon.transform.position.z
            );


        float distance =
            Vector2.Distance(
                objectPos,
                playerPos
            );


        if (distance > interactDistance)
        {
            Debug.LogWarning(
                "[WeaponChest] Player quá xa rương: " +
                distance
            );

            return;
        }


        // =====================================================
        // MỞ RƯƠNG
        // =====================================================

        OpenChest(
            playerObject.InputAuthority
        );
    }


    // =========================================================
    // TRY OPEN
    // =========================================================

    public void TryOpenFrom(
        PlayerWeapon player
    )
    {
        if (
            !HasStateAuthority ||
            opened ||
            Object == null ||
            !Object.IsValid ||
            player == null ||
            player.Object == null ||
            !player.Object.IsValid
        )
        {
            return;
        }


        Vector2 objectPos =
            new Vector2(
                transform.position.x,
                transform.position.z
            );


        Vector2 playerPos =
            new Vector2(
                player.transform.position.x,
                player.transform.position.z
            );


        float distance =
            Vector2.Distance(
                objectPos,
                playerPos
            );


        if (
    distance >
    interactDistance)
        {
            return;
        }


        OpenChest(
            player.Object.InputAuthority
        );
    }


    // =========================================================
    // OPEN CHEST
    // =========================================================

    private void OpenChest(PlayerRef opener)
    {
        if (opened || !HasStateAuthority ||
            Object == null || !Object.IsValid)
            return;

        // Khoa mo ruong ngay, tranh nhieu player mo cung luc.
        opened = true;

        // Tat beam tren tat ca may va phat animation dong bo.
        Rpc_PlayOpenAnimation();

        // Am thanh chi cho player da mo ruong (giu logic cu).
        Rpc_PlayChestOpenSound(opener);

        // Chi State Authority duoc spawn vat pham va despawn ruong.
        StartCoroutine(OpenChestRoutine());
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayOpenAnimation()
    {
        opened = true;

        if (lightBeam != null)
            lightBeam.SetActive(false);

        if (chestAnimator == null)
            chestAnimator = GetComponentInChildren<Animator>(true);

        if (chestAnimator != null &&
            !string.IsNullOrEmpty(openTriggerName))
        {
            chestAnimator.ResetTrigger(openTriggerName);
            chestAnimator.SetTrigger(openTriggerName);
        }
        else
        {
            Debug.LogWarning(
                "[WeaponChest] Chua gan Animator / Open Trigger.");
        }
    }

    private IEnumerator OpenChestRoutine()
    {
        // Cho animation mo ruong chay truoc khi lay do.
        if (openAnimationDuration > 0f)
            yield return new WaitForSeconds(openAnimationDuration);

        if (!HasStateAuthority || Runner == null ||
            Object == null || !Object.IsValid)
            yield break;

        SpawnRandomItem();
        Runner.Despawn(Object);
    }


    // =========================================================
    // CHEST OPEN SOUND RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayChestOpenSound(
        PlayerRef opener)
    {
        // =====================================================
        // CHỈ PLAYER MỞ RƯƠNG MỚI NGHE
        // =====================================================

        if (Runner == null)
            return;


        if (Runner.LocalPlayer != opener)
            return;


        // =====================================================
        // CHECK SOUND
        // =====================================================

        if (chestOpenSound == null)
            return;


        // =====================================================
        // CHECK SFX PLAYER
        // =====================================================

        if (GameAudio.SfxPlayer.Instance == null)
        {
            Debug.LogWarning(
                "[WeaponChest] Không tìm thấy GameAudio.SfxPlayer!"
            );

            return;
        }


        // =====================================================
        // PLAY
        // =====================================================

        GameAudio.SfxPlayer.Instance.PlaySfx(
            chestOpenSound,
            chestOpenVolume
        );
    }


    // =========================================================
    // SPAWN RANDOM ITEM
    // =========================================================

    private void SpawnRandomItem()
    {
        // Chi spawn vat pham SAU KHI ruong duoc mo boi Player.
        // Ruong chua mo: khong tao vat pham, nen khong co timer 5 giay.
        if (!HasStateAuthority || !opened ||
            Object == null || !Object.IsValid)
        {
            return;
        }

        // =====================================================
        // TỔNG TỈ LỆ
        // =====================================================

        float totalChance =
            rifleChance +
            pistolChance +
            batChance +
            shovelChance +

            rifleAmmoChance +
            pistolAmmoChance +

            smallMedkitChance +
            mediumMedkitChance +
            largeMedkitChance;


        if (totalChance <= 0f)
        {
            Debug.LogError(
                "[WeaponChest] " +
                "Tổng tỉ lệ item phải lớn hơn 0!"
            );

            return;
        }


        // =====================================================
        // RANDOM
        // =====================================================

        float randomValue =
            Random.Range(
                0f,
                totalChance
            );


        NetworkPrefabRef selectedPrefab =
            default;


        string selectedItem =
            "None";


        float currentChance =
            0f;


        // =====================================================
        // RIFLE
        // =====================================================

        currentChance += rifleChance;

        if (randomValue < currentChance)
        {
            selectedPrefab =
                riflePrefab;

            selectedItem =
                "Rifle";
        }


        // =====================================================
        // PISTOL
        // =====================================================

        else
        {
            currentChance +=
                pistolChance;

            if (randomValue < currentChance)
            {
                selectedPrefab =
                    pistolPrefab;

                selectedItem =
                    "Pistol";
            }


            // =================================================
            // BAT
            // =================================================

            else
            {
                currentChance +=
                    batChance;

                if (randomValue < currentChance)
                {
                    selectedPrefab =
                        batPrefab;

                    selectedItem =
                        "Bat";
                }


                // =============================================
                // SHOVEL
                // =============================================

                else
                {
                    currentChance +=
                        shovelChance;

                    if (randomValue < currentChance)
                    {
                        selectedPrefab =
                            shovelPrefab;

                        selectedItem =
                            "Shovel";
                    }


                    // =========================================
                    // RIFLE AMMO
                    // =========================================

                    else
                    {
                        currentChance +=
                            rifleAmmoChance;

                        if (randomValue < currentChance)
                        {
                            selectedPrefab =
                                rifleAmmoPrefab;

                            selectedItem =
                                "Rifle Ammo";
                        }


                        // =====================================
                        // PISTOL AMMO
                        // =====================================

                        else
                        {
                            currentChance +=
                                pistolAmmoChance;

                            if (randomValue < currentChance)
                            {
                                selectedPrefab =
                                    pistolAmmoPrefab;

                                selectedItem =
                                    "Pistol Ammo";
                            }


                            // =================================
                            // SMALL MEDKIT
                            // =================================

                            else
                            {
                                currentChance +=
                                    smallMedkitChance;

                                if (randomValue < currentChance)
                                {
                                    selectedPrefab =
                                        smallMedkitPrefab;

                                    selectedItem =
                                        "Small Medkit";
                                }


                                // =============================
                                // MEDIUM MEDKIT
                                // =============================

                                else
                                {
                                    currentChance +=
                                        mediumMedkitChance;

                                    if (randomValue < currentChance)
                                    {
                                        selectedPrefab =
                                            mediumMedkitPrefab;

                                        selectedItem =
                                            "Medium Medkit";
                                    }


                                    // =============================
                                    // LARGE MEDKIT
                                    // =============================

                                    else
                                    {
                                        selectedPrefab =
                                            largeMedkitPrefab;

                                        selectedItem =
                                            "Large Medkit";
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }


        // =====================================================
        // CHECK PREFAB
        // =====================================================

        if (!selectedPrefab.IsValid)
        {
            Debug.LogError(
                "[WeaponChest] Prefab của " +
                selectedItem +
                " không hợp lệ!"
            );

            return;
        }


        // =====================================================
        // SPAWN POSITION
        // =====================================================

        Vector3 spawnPosition =
            transform.position +
            Vector3.up *
            0.5f;


        // =====================================================
        // FUSION SPAWN
        // =====================================================

        NetworkObject item =
            Runner.Spawn(
                selectedPrefab,
                spawnPosition,
                Quaternion.identity
            );


        if (item == null)
        {
            Debug.LogError(
                "[WeaponChest] " +
                "Runner.Spawn item FAILED!"
            );

            return;
        }


        // =====================================================
        // ITEM ROTATE
        // =====================================================

        ItemRotate itemRotate =
            item.GetComponent<ItemRotate>();

        if (itemRotate != null)
        {
            itemRotate.enabled = true;
        }


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "====================================\n" +
            "[WeaponChest] ITEM SPAWN\n" +
            "Item: " + selectedItem +
            "\nRandom Value: " + randomValue +
            "\nTotal Chance: " + totalChance +
            "\nSpawn Position: " + spawnPosition +
            "\n===================================="
        );
    }
}