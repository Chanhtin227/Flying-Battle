using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class MatchStatsUI : MonoBehaviour
{
    // =========================================================
    // PANEL
    // =========================================================

    [Header("Panel")]
    public GameObject statsPanel;


    // =========================================================
    // WIN / LOSE PANEL
    // =========================================================

    [Header("Win / Lose Panel")]

    [Tooltip("Kéo WinPanel vào đây")]
    public GameObject winPanel;

    [Tooltip("Kéo LosePanel vào đây")]
    public GameObject losePanel;


    // Lưu trạng thái trước khi mở bảng tổng kết
    private bool winPanelWasActive;
    private bool losePanelWasActive;


    // =========================================================
    // TEXT VALUES
    // =========================================================

    [Header("Stats Text")]

    public TMP_Text damageValueText;

    public TMP_Text damageTakenValueText;

    public TMP_Text killValueText;

    public TMP_Text timeValueText;

    public TMP_Text goldValueText;


    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]

    public Button closeButton;


    // =========================================================
    // GOLD
    // =========================================================

    [Header("Gold Settings")]

    public int goldPerKill = 35;

    [Tooltip("Tu dong luu vang khi tran dau ket thuc.")]
    public bool autoSaveGoldOnMatchEnd = true;

    // Chi trao thuong 1 lan cho 1 lan choi tran dau.
    private bool goldAwardedThisMatch = false;


    // =========================================================
    // EFFECT
    // =========================================================

    [Header("Effect")]

    [Tooltip("Kéo ResultPanelEffect của StatsBox vào đây")]
    public ResultPanelEffect statsEffect;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // LUÔN ẨN PANEL KHI BẮT ĐẦU
        // =====================================================

        if (statsPanel != null)
        {
            statsPanel.SetActive(false);
        }
    }


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        // =====================================================
        // CLOSE BUTTON
        // =====================================================

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(
                ClosePanel
            );

            closeButton.onClick.AddListener(
                ClosePanel
            );
        }
    }


    // =========================================================
    // SAVE GOLD ONCE AFTER MATCH END
    // =========================================================

    private void Update()
    {
        // MatchStatsUI phai nam tren GameObject luon Active suot tran.
        if (autoSaveGoldOnMatchEnd && !goldAwardedThisMatch)
            TrySaveMatchGold();
    }

    private void OnDisable()
    {
        // Du phong khi UI bi tat luc ket thuc tran.
        if (autoSaveGoldOnMatchEnd && !goldAwardedThisMatch)
            TrySaveMatchGold();
    }

    private void TrySaveMatchGold()
    {
        if (goldAwardedThisMatch) return;

        MatchManager manager = MatchManager.Instance;
        if (manager == null || !manager.MatchEnded) return;

        PlayerMovement localMovement = PlayerMovement.LocalPlayer;
        if (localMovement == null || !localMovement.HasInputAuthority) return;

        PlayerHealth health = localMovement.GetComponent<PlayerHealth>();
        if (health == null) return;

        int goldEarned = Mathf.Max(0, health.Kills) * Mathf.Max(0, goldPerKill);
        GoldWallet.AddGold(goldEarned);
        goldAwardedThisMatch = true;

        Debug.Log("[MatchStatsUI] Da luu " + goldEarned +
                  " vang. Tong vang: " + GoldWallet.GetGold());
    }

    // =========================================================
    // OPEN PANEL
    // =========================================================

    public void OpenPanel()
    {
        // =====================================================
        // MATCH MANAGER
        // =====================================================

        MatchManager manager =
            MatchManager.Instance;


        if (manager == null)
        {
            Debug.LogWarning(
                "[MatchStatsUI] MatchManager = NULL"
            );

            return;
        }


        // =====================================================
        // CHỈ ĐƯỢC MỞ SAU KHI TRẬN KẾT THÚC
        // =====================================================

        if (!manager.MatchEnded)
        {
            Debug.LogWarning(
                "[MatchStatsUI] Trận đấu chưa kết thúc nên chưa thể xem thống kê."
            );

            return;
        }


        // =====================================================
        // CHECK PANEL
        // =====================================================

        if (statsPanel == null)
        {
            Debug.LogWarning(
                "[MatchStatsUI] Stats Panel chưa được gán!"
            );

            return;
        }


        // =====================================================
        // LƯU TRẠNG THÁI WIN / LOSE
        // =====================================================

        if (winPanel != null)
        {
            winPanelWasActive =
                winPanel.activeSelf;
        }


        if (losePanel != null)
        {
            losePanelWasActive =
                losePanel.activeSelf;
        }


        // =====================================================
        // ẨN WIN / LOSE PANEL
        // =====================================================

        if (winPanel != null)
        {
            winPanel.SetActive(
                false
            );
        }


        if (losePanel != null)
        {
            losePanel.SetActive(
                false
            );
        }


        // =====================================================
        // LUU VANG 1 LAN VA CAP NHAT BANG THONG KE
        // =====================================================

        TrySaveMatchGold();
        RefreshStats();


        // =====================================================
        // SHOW PANEL
        // =====================================================

        statsPanel.SetActive(
            true
        );


        // =====================================================
        // EFFECT
        // =====================================================

        if (statsEffect != null)
        {
            statsEffect.PlayEffect();
        }


        Debug.Log(
            "[MatchStatsUI] OPEN MATCH STATS"
        );
    }


    // =========================================================
    // CLOSE PANEL
    // =========================================================

    public void ClosePanel()
    {
        // =====================================================
        // HIDE STATS PANEL
        // =====================================================

        if (statsPanel != null)
        {
            statsPanel.SetActive(
                false
            );
        }


        // =====================================================
        // HIỆN LẠI WIN / LOSE PANEL ĐÚNG TRẠNG THÁI CŨ
        // =====================================================

        if (winPanel != null)
        {
            winPanel.SetActive(
                winPanelWasActive
            );
        }


        if (losePanel != null)
        {
            losePanel.SetActive(
                losePanelWasActive
            );
        }


        Debug.Log(
            "[MatchStatsUI] CLOSE MATCH STATS"
        );
    }


    // =========================================================
    // REFRESH STATS
    // =========================================================

    private void RefreshStats()
    {
        // =====================================================
        // LOCAL PLAYER
        // =====================================================

        if (PlayerMovement.LocalPlayer == null)
        {
            Debug.LogWarning(
                "[MatchStatsUI] LocalPlayer = NULL"
            );

            return;
        }


        GameObject localPlayer =
            PlayerMovement.LocalPlayer.gameObject;


        // =====================================================
        // PLAYER HEALTH
        // =====================================================

        PlayerHealth health =
            localPlayer.GetComponent<PlayerHealth>();


        if (health == null)
        {
            Debug.LogWarning(
                "[MatchStatsUI] Không tìm thấy PlayerHealth"
            );

            return;
        }


        // =====================================================
        // KILLS
        // =====================================================

        int kills =
            health.Kills;


        if (killValueText != null)
        {
            killValueText.text =
                kills.ToString("0");
        }


        // =====================================================
        // TOTAL DAMAGE + DAMAGE TAKEN
        // =====================================================

        int totalDamage =
            0;

        int totalDamageTaken =
            0;


        MatchStatsTracker tracker =
            localPlayer.GetComponent<MatchStatsTracker>();


        if (tracker != null)
        {
            totalDamage =
                tracker.GetTotalDamageInt();

            totalDamageTaken =
                tracker.GetTotalDamageTakenInt();
        }
        else
        {
            Debug.LogWarning(
                "[MatchStatsUI] Local Player chưa có MatchStatsTracker."
            );
        }


        // =====================================================
        // TOTAL DAMAGE TEXT
        // =====================================================

        if (damageValueText != null)
        {
            damageValueText.text =
                totalDamage.ToString("000");
        }


        // =====================================================
        // DAMAGE TAKEN TEXT
        // =====================================================

        if (damageTakenValueText != null)
        {
            damageTakenValueText.text =
                totalDamageTaken.ToString("000");
        }


        // =====================================================
        // MATCH TIME
        // =====================================================

        float playedTime =
            0f;


        MatchManager manager =
            MatchManager.Instance;


        if (manager != null)
        {
            playedTime =
                manager.GetPlayedTime();
        }


        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.FloorToInt(
                    playedTime
                )
            );


        int minutes =
            totalSeconds / 60;


        int seconds =
            totalSeconds % 60;


        if (timeValueText != null)
        {
            timeValueText.text =
                minutes.ToString("00") +
                ":" +
                seconds.ToString("00");
        }


        // =====================================================
        // GOLD
        // =====================================================

        int gold =
            kills *
            goldPerKill;


        if (goldValueText != null)
        {
            goldValueText.text =
                "+" +
                gold.ToString("000");
        }


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[MatchStatsUI]" +
            " | Damage = " +
            totalDamage +
            " | Damage Taken = " +
            totalDamageTaken +
            " | Kills = " +
            kills +
            " | Time = " +
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00") +
            " | Gold = " +
            gold
        );
    }
}