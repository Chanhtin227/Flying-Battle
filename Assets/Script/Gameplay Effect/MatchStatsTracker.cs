using UnityEngine;
using Fusion;

public class MatchStatsTracker : NetworkBehaviour
{
    // =========================================================
    // NETWORKED MATCH STATS
    // =========================================================

    [Header("Match Stats")]

    [Networked]
    public float TotalDamage { get; set; }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        // Chỉ State Authority được phép thay đổi
        // Networked variable.
        if (!HasStateAuthority)
            return;


        // Reset damage khi Player được spawn vào trận.
        TotalDamage = 0f;


        Debug.Log(
            "[MatchStatsTracker] SPAWNED" +
            " | Player = " +
            Object.InputAuthority +
            " | Total Damage = 0"
        );
    }


    // =========================================================
    // ADD DAMAGE
    // =========================================================

    public void AddDamage(float damage)
    {
        // =====================================================
        // CHỈ STATE AUTHORITY ĐƯỢC CỘNG
        // =====================================================

        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[MatchStatsTracker] AddDamage bị bỏ qua" +
                " | Không có State Authority" +
                " | Player = " +
                Object.InputAuthority
            );

            return;
        }


        // =====================================================
        // DAMAGE PHẢI > 0
        // =====================================================

        if (damage <= 0f)
        {
            return;
        }


        // =====================================================
        // KHÔNG CỘNG DAMAGE SAU KHI MATCH ĐÃ KẾT THÚC
        // =====================================================

        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }


        // =====================================================
        // CỘNG TOTAL DAMAGE
        // =====================================================

        TotalDamage += damage;


        // Đảm bảo không bao giờ âm.
        TotalDamage =
            Mathf.Max(
                0f,
                TotalDamage
            );


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[MatchStatsTracker] ADD DAMAGE" +
            " | Player = " +
            Object.InputAuthority +
            " | +" +
            damage.ToString("F0") +
            " | Total Damage = " +
            TotalDamage.ToString("F0")
        );
    }


    // =========================================================
    // GET TOTAL DAMAGE
    // =========================================================

    public float GetTotalDamage()
    {
        return Mathf.Max(
            0f,
            TotalDamage
        );
    }


    // =========================================================
    // GET TOTAL DAMAGE AS INT
    // =========================================================

    public int GetTotalDamageInt()
    {
        return Mathf.RoundToInt(
            Mathf.Max(
                0f,
                TotalDamage
            )
        );
    }


    // =========================================================
    // RESET MATCH STATS
    // =========================================================

    public void ResetStats()
    {
        // Chỉ State Authority được reset.
        if (!HasStateAuthority)
            return;


        TotalDamage = 0f;


        Debug.Log(
            "[MatchStatsTracker] RESET STATS" +
            " | Player = " +
            Object.InputAuthority +
            " | Total Damage = 0"
        );
    }
}