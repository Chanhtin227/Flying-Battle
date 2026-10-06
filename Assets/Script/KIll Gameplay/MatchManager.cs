using UnityEngine;
using Fusion;

public class MatchManager : NetworkBehaviour
{
    public static MatchManager Instance;


    // =========================================================
    // MATCH SETTINGS
    // =========================================================

    [Header("Match Settings")]

    [Tooltip("Số kill một Player cần đạt để thắng")]
    public int killLimit = 10;

    [Tooltip("Thời gian trận đấu tính bằng giây")]
    public float matchDuration = 300f;


    // =========================================================
    // MATCH STATE
    // =========================================================

    [Networked]
    public TickTimer MatchTimer { get; set; }

    [Networked]
    public NetworkBool MatchStarted { get; set; }

    [Networked]
    public NetworkBool MatchEnded { get; set; }

    [Networked]
    public PlayerRef Winner { get; set; }

    [Networked]
    public int WinnerKills { get; set; }


    // =========================================================
    // PLAYED TIME
    // =========================================================

    [Networked]
    public float PlayedTime { get; set; }


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        Instance = this;
    }


    // =========================================================
    // SPAWNED
    // =========================================================

    public override void Spawned()
    {
        Instance = this;


        if (!HasStateAuthority)
            return;


        MatchStarted = true;
        MatchEnded = false;

        Winner = PlayerRef.None;
        WinnerKills = 0;

        PlayedTime = 0f;


        MatchTimer =
            TickTimer.CreateFromSeconds(
                Runner,
                matchDuration
            );


        Debug.Log(
            "[MatchManager] SOLO MATCH STARTED"
        );
    }


    // =========================================================
    // NETWORK UPDATE
    // =========================================================

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority)
            return;

        if (!MatchStarted)
            return;

        if (MatchEnded)
            return;


        // =====================================================
        // COUNT PLAYED TIME
        // =====================================================

        PlayedTime +=
            Runner.DeltaTime;


        // =====================================================
        // TIME UP
        // =====================================================

        if (MatchTimer.Expired(Runner))
        {
            EndMatchByTime();
        }
    }


    // =========================================================
    // REPORT KILL
    // =========================================================

    public void ReportKill(
        PlayerRef killer,
        int currentKills)
    {
        if (!HasStateAuthority)
            return;

        if (!MatchStarted)
            return;

        if (MatchEnded)
            return;

        if (killer == PlayerRef.None)
            return;


        Debug.Log(
            "[MatchManager] Player " +
            killer +
            " | Kills = " +
            currentKills +
            " / " +
            killLimit
        );


        if (currentKills >= killLimit)
        {
            EndMatch(
                killer,
                currentKills
            );
        }
    }


    // =========================================================
    // END MATCH - KILL LIMIT
    // =========================================================

    private void EndMatch(
        PlayerRef winner,
        int kills)
    {
        if (!HasStateAuthority)
            return;

        if (MatchEnded)
            return;


        Winner = winner;

        WinnerKills = kills;

        MatchEnded = true;

        MatchStarted = false;


        MatchTimer =
            TickTimer.None;


        Debug.Log(
            "=============================\n" +
            "SOLO MATCH END\n" +
            "WINNER = " +
            winner +
            "\nKILLS = " +
            kills +
            "\nTIME = " +
            GetPlayedTime().ToString("F1") +
            "s\n" +
            "============================="
        );
    }


    // =========================================================
    // END MATCH - TIME
    // =========================================================

    private void EndMatchByTime()
    {
        if (!HasStateAuthority)
            return;

        if (MatchEnded)
            return;


        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        PlayerHealth bestPlayer =
            null;

        int bestKills =
            -1;

        bool draw =
            false;


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null ||
                !player.Object.IsValid)
            {
                continue;
            }


            if (player.Kills > bestKills)
            {
                bestKills =
                    player.Kills;

                bestPlayer =
                    player;

                draw =
                    false;
            }
            else if (player.Kills ==
                     bestKills)
            {
                draw =
                    true;
            }
        }


        MatchEnded =
            true;

        MatchStarted =
            false;


        MatchTimer =
            TickTimer.None;


        // =====================================================
        // DRAW
        // =====================================================

        if (bestPlayer == null ||
            draw)
        {
            Winner =
                PlayerRef.None;

            WinnerKills =
                Mathf.Max(
                    0,
                    bestKills
                );


            Debug.Log(
                "[MatchManager] TIME UP - DRAW" +
                " | Played Time = " +
                GetPlayedTime().ToString("F1")
            );


            return;
        }


        // =====================================================
        // WINNER
        // =====================================================

        Winner =
            bestPlayer.Object.InputAuthority;


        WinnerKills =
            bestPlayer.Kills;


        Debug.Log(
            "[MatchManager] TIME UP" +
            " | WINNER = " +
            Winner +
            " | KILLS = " +
            WinnerKills +
            " | TIME = " +
            GetPlayedTime().ToString("F1")
        );
    }


    // =========================================================
    // GET REMAINING TIME
    // =========================================================

    public float GetRemainingTime()
    {
        if (Runner == null)
            return 0f;


        if (MatchEnded)
            return 0f;


        float? remaining =
            MatchTimer.RemainingTime(
                Runner
            );


        if (!remaining.HasValue)
            return 0f;


        return Mathf.Max(
            0f,
            remaining.Value
        );
    }


    // =========================================================
    // GET PLAYED TIME
    // =========================================================

    public float GetPlayedTime()
    {
        return Mathf.Max(
            0f,
            PlayedTime
        );
    }
}