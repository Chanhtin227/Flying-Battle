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
    // TEXT VALUES
    // =========================================================

    [Header("Stats Text")]

    public TMP_Text damageValueText;

    public TMP_Text killValueText;

    public TMP_Text timeValueText;

    public TMP_Text goldValueText;


    // =========================================================
    // BUTTONS
    // =========================================================

    [Header("Buttons")]

    public Button closeButton;

    public Button closeXButton;


    // =========================================================
    // GOLD
    // =========================================================

    [Header("Gold Settings")]

    public int goldPerKill = 35;


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


        // =====================================================
        // X BUTTON
        // =====================================================

        if (closeXButton != null)
        {
            closeXButton.onClick.RemoveListener(
                ClosePanel
            );

            closeXButton.onClick.AddListener(
                ClosePanel
            );
        }
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
        // REFRESH DATA TRƯỚC
        // =====================================================

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
        if (statsPanel == null)
            return;


        statsPanel.SetActive(
            false
        );


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
                kills.ToString("00");
        }


        // =====================================================
        // TOTAL DAMAGE
        // =====================================================

        int totalDamage =
            0;


        MatchStatsTracker tracker =
            localPlayer.GetComponent<MatchStatsTracker>();


        if (tracker != null)
        {
            totalDamage =
                Mathf.RoundToInt(
                    tracker.TotalDamage
                );
        }
        else
        {
            Debug.LogWarning(
                "[MatchStatsUI] Local Player chưa có MatchStatsTracker."
            );
        }


        if (damageValueText != null)
        {
            damageValueText.text =
                totalDamage.ToString("0000");
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