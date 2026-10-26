
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class NetworkHealthUI : MonoBehaviour
{
    // =====================================================
    // HEALTH UI
    // =====================================================

    [Header("Health UI")]
    public Slider healthSlider;
    public TMP_Text hpText;

    // =====================================================
    // MEDKIT PROGRESS UI
    // =====================================================

    [Header("Medkit Progress UI")]
    public GameObject medkitPanel;
    public Slider medkitProgressSlider;
    public TMP_Text medkitStatusText;
    public TMP_Text medkitTimeText;

    [Header("Medkit Settings")]
    public bool showMedkitType = true;
    public bool showProgressPercent = false;

    // =====================================================
    // LOCAL PLAYER
    // =====================================================

    [Header("Local Player Search")]
    [Min(0.1f)]
    public float searchInterval = 0.5f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerHealth localPlayerHealth;
    private float nextSearchTime;

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (medkitProgressSlider != null)
        {
            medkitProgressSlider.minValue = 0f;
            medkitProgressSlider.maxValue = 1f;
            medkitProgressSlider.wholeNumbers = false;
            medkitProgressSlider.interactable = false;
            medkitProgressSlider.value = 0f;
        }

        HideMedkitUI();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        if (!IsLocalPlayerValid())
        {
            localPlayerHealth = null;

            if (Time.unscaledTime >= nextSearchTime)
            {
                nextSearchTime =
                    Time.unscaledTime +
                    Mathf.Max(0.1f, searchInterval);

                FindLocalPlayer();
            }
        }

        if (!IsLocalPlayerValid())
        {
            HideMedkitUI();
            return;
        }

        UpdateHealthUI();
        UpdateMedkitUI();
    }

    // =====================================================
    // VALIDATE LOCAL PLAYER
    // =====================================================

    private bool IsLocalPlayerValid()
    {
        return localPlayerHealth != null &&
               localPlayerHealth.Object != null &&
               localPlayerHealth.Object.IsValid &&
               localPlayerHealth.HasInputAuthority;
    }

    // =====================================================
    // FIND LOCAL PLAYER
    // =====================================================

    private void FindLocalPlayer()
    {
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsSortMode.None
            );

        foreach (PlayerHealth player in players)
        {
            if (player == null ||
                player.Object == null ||
                !player.Object.IsValid ||
                !player.HasInputAuthority)
            {
                continue;
            }

            localPlayerHealth = player;

            if (showDebugLogs)
            {
                Debug.Log(
                    "[NetworkHealthUI] Local Player: " +
                    player.name,
                    this
                );
            }

            return;
        }
    }

    // =====================================================
    // UPDATE HEALTH UI
    // =====================================================

    private void UpdateHealthUI()
    {
        float maxHP =
            Mathf.Max(1f, localPlayerHealth.maxHealth);

        float currentHP =
            Mathf.Clamp(
                localPlayerHealth.CurrentHealth,
                0f,
                maxHP
            );

        // =============================================
        // HEALTH SLIDER
        // =============================================

        if (healthSlider != null)
        {
            healthSlider.minValue = 0f;
            healthSlider.maxValue = maxHP;
            healthSlider.value = currentHP;
        }

        // =============================================
        // HEALTH TEXT
        // =============================================

        if (hpText != null)
        {
            hpText.text =
                "HP: " +
                Mathf.CeilToInt(currentHP);
        }
    }

    // =====================================================
    // UPDATE MEDKIT UI
    // =====================================================

    private void UpdateMedkitUI()
    {
        // Khong hien Medkit UI khi Player chet.
        if (localPlayerHealth.IsDead)
        {
            HideMedkitUI();
            return;
        }

        // Khong hien Medkit UI khi tran ket thuc.
        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            HideMedkitUI();
            return;
        }

        // 0 = khong su dung Medkit.
        int medkitType =
            localPlayerHealth.UsingMedkitType;

        if (medkitType < 1 || medkitType > 3)
        {
            HideMedkitUI();
            return;
        }

        NetworkRunner runner =
            localPlayerHealth.Runner;

        if (runner == null)
        {
            HideMedkitUI();
            return;
        }

        // =============================================
        // GET MEDKIT DURATION
        // =============================================

        float totalTime =
            Mathf.Max(
                0.01f,
                localPlayerHealth.GetMedkitUseTime(
                    medkitType
                )
            );

        float? remainingNullable =
            localPlayerHealth.MedkitTimer
                .RemainingTime(runner);

        if (!remainingNullable.HasValue)
        {
            HideMedkitUI();
            return;
        }

        float remainingTime =
            Mathf.Clamp(
                remainingNullable.Value,
                0f,
                totalTime
            );

        // =============================================
        // CALCULATE PROGRESS
        // =============================================

        float progress =
            Mathf.Clamp01(
                1f - remainingTime / totalTime
            );

        // =============================================
        // SHOW PANEL
        // =============================================

        if (medkitPanel != null &&
            !medkitPanel.activeSelf)
        {
            medkitPanel.SetActive(true);
        }

        // =============================================
        // PROGRESS SLIDER
        // =============================================

        if (medkitProgressSlider != null)
        {
            medkitProgressSlider.value = progress;
        }

        // =============================================
        // STATUS TEXT
        // =============================================

        if (medkitStatusText != null)
        {
            if (showMedkitType)
            {
                medkitStatusText.text =
                    GetMedkitName(medkitType);
            }
            else
            {
                medkitStatusText.text =
                    "USING MEDKIT...";
            }
        }

        // =============================================
        // REMAINING TIME
        // =============================================

        if (medkitTimeText != null)
        {
            medkitTimeText.text =
                showProgressPercent
                    ? Mathf.RoundToInt(progress * 100f) + "%"
                    : remainingTime.ToString("F1") + "s";
        }
    }

    // =====================================================
    // GET MEDKIT NAME
    // =====================================================

    private string GetMedkitName(int type)
    {
        switch (type)
        {
            case 1:
                return "USING SMALL MEDKIT...";

            case 2:
                return "USING MEDIUM MEDKIT...";

            case 3:
                return "USING LARGE MEDKIT...";

            default:
                return "USING MEDKIT...";
        }
    }

    // =====================================================
    // HIDE MEDKIT UI
    // =====================================================

    private void HideMedkitUI()
    {
        if (medkitPanel != null &&
            medkitPanel.activeSelf)
        {
            medkitPanel.SetActive(false);
        }

        if (medkitProgressSlider != null)
        {
            medkitProgressSlider.value = 0f;
        }

        if (medkitTimeText != null)
        {
            medkitTimeText.text = "";
        }
    }
}
