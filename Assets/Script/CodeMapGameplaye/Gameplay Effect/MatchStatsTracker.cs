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

    [Networked]
    public float TotalDamageTaken { get; set; }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        // Chỉ State Authority được phép thay đổi
        // Networked variable.
        if (!HasStateAuthority)
            return;


        // Reset stats khi Player được spawn vào trận.
        TotalDamage = 0f;

        TotalDamageTaken = 0f;


        Debug.Log(
            "[MatchStatsTracker] SPAWNED" +
            " | Player = " +
            Object.InputAuthority +
            " | Total Damage = 0" +
            " | Damage Taken = 0"
        );
    }


    // =========================================================
    // ADD DAMAGE
    //
    // Sát thương Player này gây ra cho Player khác.
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
    // ADD DAMAGE TAKEN
    //
    // Sát thương Player này nhận vào.
    // =========================================================

    public void AddDamageTaken(float damage)
    {
        // =====================================================
        // CHỈ STATE AUTHORITY ĐƯỢC CỘNG
        // =====================================================

        if (!HasStateAuthority)
        {
            Debug.LogWarning(
                "[MatchStatsTracker] AddDamageTaken bị bỏ qua" +
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
        // KHÔNG CỘNG SAU KHI MATCH ĐÃ KẾT THÚC
        // =====================================================

        if (MatchManager.Instance != null &&
            MatchManager.Instance.MatchEnded)
        {
            return;
        }


        // =====================================================
        // CỘNG DAMAGE TAKEN
        // =====================================================

        TotalDamageTaken += damage;


        TotalDamageTaken =
            Mathf.Max(
                0f,
                TotalDamageTaken
            );


        // =====================================================
        // DEBUG
        // =====================================================

        Debug.Log(
            "[MatchStatsTracker] DAMAGE TAKEN" +
            " | Player = " +
            Object.InputAuthority +
            " | +" +
            damage.ToString("F0") +
            " | Total Damage Taken = " +
            TotalDamageTaken.ToString("F0")
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
    // GET DAMAGE TAKEN
    // =========================================================

    public float GetTotalDamageTaken()
    {
        return Mathf.Max(
            0f,
            TotalDamageTaken
        );
    }


    // =========================================================
    // GET DAMAGE TAKEN AS INT
    // =========================================================

    public int GetTotalDamageTakenInt()
    {
        return Mathf.RoundToInt(
            Mathf.Max(
                0f,
                TotalDamageTaken
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

        TotalDamageTaken = 0f;


        Debug.Log(
            "[MatchStatsTracker] RESET STATS" +
            " | Player = " +
            Object.InputAuthority +
            " | Total Damage = 0" +
            " | Damage Taken = 0"
        );
    }
}