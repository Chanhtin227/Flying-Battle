using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class RespawnUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Respawn UI")]

    [Tooltip("Panel chứa toàn bộ giao diện hồi sinh")]
    public GameObject respawnPanel;

    [Tooltip("Text hiển thị 5 - 4 - 3 - 2 - 1")]
    public TMP_Text countdownText;

    [Tooltip("Thanh tiến trình hồi sinh")]
    public Slider progressSlider;


    // =========================================================
    // UI AN KHI HOI SINH
    // =========================================================

    [Header("Hide Gameplay UI While Dead")]
    [Tooltip("Keo WeaponHUD, MatchHUD, Minimap, HealthUI... vao day. KHONG keo Canvas tong hoac RespawnPanel.")]
    public GameObject[] hideWhileRespawning;

    // Chi khoi phuc nhung UI dang bat truoc khi Player chet.
    private readonly Dictionary<GameObject, bool> savedGameplayUIStates = new();
    private bool gameplayUIHidden;

    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Respawn Audio")]

    [Tooltip("AudioSource dùng để phát âm thanh hồi sinh")]
    public AudioSource audioSource;

    [Tooltip("Âm thanh phát mỗi lần 5 → 4 → 3 → 2 → 1")]
    public AudioClip respawnTickSound;

    [Tooltip("Âm thanh phát khi hồi sinh hoàn tất")]
    public AudioClip respawnCompleteSound;

    [Range(0f, 1f)]
    public float tickVolume = 1f;

    [Range(0f, 1f)]
    public float completeVolume = 1f;


    // =========================================================
    // PLAYER
    // =========================================================

    [Header("Local Player")]

    [Tooltip("Không cần kéo. Script tự tìm Local Player.")]
    public PlayerHealth playerHealth;


    // =========================================================
    // PRIVATE
    // =========================================================

    private PlayerMovement currentLocalPlayer;

    private bool lastDeadState = false;

    // Số giây đã phát âm thanh gần nhất.
    // Dùng để tránh âm thanh phát liên tục mỗi frame.
    private int lastPlayedSecond = -1;

    // Player đã thực sự bước vào trạng thái chết chưa?
    private bool wasRespawning = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        SetPanel(false);

        if (countdownText != null)
        {
            countdownText.text = "";
        }

        if (progressSlider != null)
        {
            progressSlider.minValue = 0f;
            progressSlider.maxValue = 1f;
            progressSlider.value = 0f;
            progressSlider.interactable = false;
        }

        // Nếu chưa kéo AudioSource thì thử lấy trên
        // RespawnUIManager.
        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        if (audioSource != null)
        {
            audioSource.Stop();
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        // =====================================================
        // TÌM LOCAL PLAYER
        // =====================================================

        FindLocalPlayer();


        // =====================================================
        // MATCH ĐÃ KẾT THÚC
        // =====================================================

        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            // Ẩn toàn bộ UI hồi sinh.
            SetPanel(false);

            if (countdownText != null)
            {
                countdownText.text = "";
            }

            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }

            // Reset trạng thái để không phát âm thanh hoàn tất
            // sau khi trận đã kết thúc.
            wasRespawning = false;
            lastPlayedSecond = -1;
            lastDeadState = false;

            // Dừng ngay mọi âm thanh hồi sinh đang phát.
            if (audioSource != null)
            {
                audioSource.Stop();
            }

            return;
        }


        if (playerHealth == null)
        {
            SetPanel(false);

            return;
        }


        // =====================================================
        // CHỈ XỬ LÝ LOCAL PLAYER
        // =====================================================

        if (!playerHealth.HasInputAuthority)
        {
            playerHealth = null;
            currentLocalPlayer = null;

            SetPanel(false);

            return;
        }


        // =====================================================
        // LẤY TRẠNG THÁI CHẾT
        // =====================================================

        bool isDead = playerHealth.IsDead;


        if (isDead != lastDeadState)
        {
            Debug.Log(
                "[RespawnUI] Local Player IsDead = " +
                isDead
            );

            lastDeadState = isDead;
        }


        // =====================================================
        // PLAYER CÒN SỐNG
        // =====================================================

        if (!isDead)
        {
            // Nếu trước đó đang chết mà bây giờ sống lại
            // => phát âm thanh hồi sinh hoàn tất.
            if (wasRespawning)
            {
                PlayCompleteSound();

                wasRespawning = false;

                lastPlayedSecond = -1;
            }


            SetPanel(false);


            if (countdownText != null)
            {
                countdownText.text = "";
            }


            if (progressSlider != null)
            {
                progressSlider.value = 0f;
            }


            return;
        }


        // =====================================================
        // PLAYER ĐÃ CHẾT
        // =====================================================

        if (!wasRespawning)
        {
            wasRespawning = true;

            lastPlayedSecond = -1;
        }


        SetPanel(true);


        // =====================================================
        // KIỂM TRA RUNNER
        // =====================================================

        if (playerHealth.Runner == null)
        {
            Debug.LogWarning(
                "[RespawnUI] PlayerHealth.Runner = NULL"
            );

            return;
        }


        // =====================================================
        // LẤY THỜI GIAN CÒN LẠI
        // =====================================================

        float? remaining =
            playerHealth.RespawnTimer.RemainingTime(
                playerHealth.Runner
            );


        if (!remaining.HasValue)
        {
            return;
        }


        float remainingTime =
            Mathf.Max(
                remaining.Value,
                0f
            );


        // =====================================================
        // COUNTDOWN
        // =====================================================

        int seconds =
            Mathf.CeilToInt(
                remainingTime
            );


        // Chỉ hiện 5 → 4 → 3 → 2 → 1.
        seconds =
            Mathf.Max(
                seconds,
                1
            );


        // =====================================================
        // UPDATE TEXT
        // =====================================================

        if (countdownText != null)
        {
            countdownText.text =
                seconds.ToString();
        }


        // =====================================================
        // PHÁT TICK SOUND
        // =====================================================

        // Chỉ phát khi số thay đổi.
        //
        // Ví dụ:
        // 5 -> phát 1 lần
        // 4 -> phát 1 lần
        // 3 -> phát 1 lần
        // 2 -> phát 1 lần
        // 1 -> phát 1 lần
        //
        // Không phát liên tục mỗi frame.
        if (seconds != lastPlayedSecond)
        {
            lastPlayedSecond = seconds;

            PlayTickSound();
        }


        // =====================================================
        // PROGRESS
        // =====================================================

        float totalTime =
            Mathf.Max(
                playerHealth.respawnDelay,
                0.01f
            );


        float progress =
            1f -
            (
                remainingTime /
                totalTime
            );


        progress =
            Mathf.Clamp01(
                progress
            );


        if (progressSlider != null)
        {
            progressSlider.value =
                progress;
        }
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        PlayerMovement localPlayer =
            PlayerMovement.LocalPlayer;


        if (localPlayer == null)
        {
            currentLocalPlayer = null;
            playerHealth = null;

            return;
        }


        if (currentLocalPlayer == localPlayer &&
            playerHealth != null)
        {
            return;
        }


        currentLocalPlayer =
            localPlayer;


        playerHealth =
            localPlayer.GetComponent<PlayerHealth>();


        if (playerHealth == null)
        {
            Debug.LogError(
                "[RespawnUI] Local Player không có PlayerHealth!"
            );

            return;
        }


        Debug.Log(
            "[RespawnUI] Đã kết nối Local Player: " +
            localPlayer.name
        );
    }


    // =========================================================
    // PLAY TICK SOUND
    // =========================================================

    private void PlayTickSound()
    {
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }

        if (audioSource == null)
            return;


        if (respawnTickSound == null)
            return;


        audioSource.PlayOneShot(
            respawnTickSound,
            tickVolume
        );
    }


    // =========================================================
    // PLAY COMPLETE SOUND
    // =========================================================

    private void PlayCompleteSound()
    {
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }

        if (audioSource == null)
            return;


        if (respawnCompleteSound == null)
            return;


        audioSource.PlayOneShot(
            respawnCompleteSound,
            completeVolume
        );
    }


    // =========================================================
    // SET PANEL
    // =========================================================

    private void SetPanel(bool show)
    {
        // Luon de RespawnUIManager/this GameObject dang hoat dong.
        if (show)
            SetGameplayUIHidden(true);
        else if (gameplayUIHidden &&
                 (MatchManager.Instance == null ||
                  !MatchManager.Instance.MatchEnded))
            SetGameplayUIHidden(false);

        if (respawnPanel == null)
            return;

        if (respawnPanel.activeSelf != show)
        {
            respawnPanel.SetActive(show);
            Debug.Log(show
                ? "[RespawnUI] HIEN RESPAWN PANEL"
                : "[RespawnUI] AN RESPAWN PANEL");
        }
    }

    // =========================================================
    // HIDE / RESTORE GAMEPLAY UI
    // =========================================================

    private void SetGameplayUIHidden(bool hidden)
    {
        if (hidden && !gameplayUIHidden)
        {
            savedGameplayUIStates.Clear();
            gameplayUIHidden = true;

            if (hideWhileRespawning != null)
            {
                foreach (GameObject ui in hideWhileRespawning)
                {
                    if (!IsSafeGameplayUI(ui) ||
                        savedGameplayUIStates.ContainsKey(ui))
                        continue;

                    savedGameplayUIStates.Add(ui, ui.activeSelf);
                }
            }
        }

        if (hidden)
        {
            // Goi lai moi frame de UI khac khong tu bat trong khi chet.
            foreach (var item in savedGameplayUIStates)
            {
                if (item.Key != null && item.Key.activeSelf)
                    item.Key.SetActive(false);
            }
            return;
        }

        if (!gameplayUIHidden)
            return;

        gameplayUIHidden = false;
        foreach (var item in savedGameplayUIStates)
        {
            if (item.Key != null)
                item.Key.SetActive(item.Value);
        }
        savedGameplayUIStates.Clear();
    }

    private bool IsSafeGameplayUI(GameObject ui)
    {
        if (ui == null || ui == gameObject || ui == respawnPanel)
            return false;

        // Khong tat cha cua RespawnUIManager hoac RespawnPanel.
        if (transform.IsChildOf(ui.transform))
            return false;

        if (respawnPanel != null &&
            respawnPanel.transform.IsChildOf(ui.transform))
            return false;

        return true;
    }
}
