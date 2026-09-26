using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class MedkitProgressUI : MonoBehaviour
{
    [Header("UI")]
    public GameObject progressRoot;
    public Image progressFill;
    public TMP_Text progressText;

    private PlayerHealth playerHealth;

    private void Start()
    {
        HideUI();
    }

    private void Update()
    {
        // =========================================
        // TÌM PLAYER LOCAL
        // =========================================

        if (PlayerMovement.LocalPlayer == null)
        {
            HideUI();
            return;
        }

        if (playerHealth == null)
        {
            playerHealth =
                PlayerMovement.LocalPlayer.GetComponent<PlayerHealth>();
        }

        if (playerHealth == null)
        {
            HideUI();
            return;
        }

        // =========================================
        // KIỂM TRA ĐANG HỒI MÁU
        // =========================================

        int usingType = playerHealth.UsingMedkitType;

        if (usingType == 0)
        {
            HideUI();
            return;
        }

        // =========================================
        // HIỆN UI
        // =========================================

        ShowUI();

        // =========================================
        // LẤY THỜI GIAN HỒI
        // =========================================

        float totalTime = GetMedkitUseTime(usingType);

        if (totalTime <= 0)
        {
            HideUI();
            return;
        }

        // =========================================
        // LẤY THỜI GIAN CÒN LẠI
        // =========================================

        float remainingTime =
            playerHealth.MedkitTimer.RemainingTime(
                playerHealth.Runner
            ) ?? 0f;

        // =========================================
        // TÍNH %
        // =========================================

        float progress =
            1f - Mathf.Clamp01(remainingTime / totalTime);

        // =========================================
        // UPDATE BAR
        // =========================================

        if (progressFill != null)
        {
            progressFill.fillAmount = progress;
        }

        // =========================================
        // UPDATE TEXT
        // =========================================

        if (progressText != null)
        {
            progressText.text =
                "Healing " +
                Mathf.RoundToInt(progress * 100f) +
                "%";
        }
    }

    // =========================================================
    // LẤY THỜI GIAN THEO LOẠI MEDKIT
    // =========================================================

    private float GetMedkitUseTime(int type)
    {
        switch (type)
        {
            case 1:
                return playerHealth.smallUseTime;

            case 2:
                return playerHealth.mediumUseTime;

            case 3:
                return playerHealth.largeUseTime;
        }

        return 0f;
    }

    // =========================================================
    // SHOW
    // =========================================================

    private void ShowUI()
    {
        if (progressRoot != null &&
            !progressRoot.activeSelf)
        {
            progressRoot.SetActive(true);
        }
    }

    // =========================================================
    // HIDE
    // =========================================================

    private void HideUI()
    {
        if (progressRoot != null)
            progressRoot.SetActive(false);

        if (progressFill != null)
            progressFill.fillAmount = 0f;

        if (progressText != null)
            progressText.text = "";
    }
}