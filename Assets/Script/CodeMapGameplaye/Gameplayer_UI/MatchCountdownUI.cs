
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Fusion;

public class MatchCountdownUI : MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("Countdown UI")]
    public GameObject countdownPanel;
    public TMP_Text countdownText;
    public TMP_Text statusText;

    [Header("Spawn System")]
    public SpawnPlayer spawnPlayer;

    // =====================================================
    // GAMEPLAY UI
    // =====================================================

    [Header("Hide During Waiting Map")]
    [Tooltip("Keo cac UI gameplay can an vao day.")]
    public GameObject[] gameplayUIObjects;

    [Header("Settings")]
    public bool hideWhenBattleStarts = true;

    // =====================================================
    // PRIVATE
    // =====================================================

    private int lastSeconds = -1;
    private bool? lastBattleState;

    private readonly Dictionary<GameObject, bool>
        initialUIStates = new();

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        // Save original UI active states.
        if (gameplayUIObjects == null)
            return;

        foreach (GameObject ui in gameplayUIObjects)
        {
            if (ui == null || initialUIStates.ContainsKey(ui))
                continue;

            initialUIStates.Add(ui, ui.activeSelf);

            // Hide immediately to prevent flashing.
            ui.SetActive(false);
        }
    }

    // =====================================================
    // START
    // =====================================================

    private void Start()
    {
        if (countdownPanel != null)
            countdownPanel.SetActive(true);

        if (countdownText != null)
            countdownText.text = "--:--";

        if (statusText != null)
            statusText.text = "WAITING FOR PLAYERS...";

        FindSpawnPlayer();
    }

    // =====================================================
    // FIND SPAWN
    // =====================================================

    private void FindSpawnPlayer()
    {
        if (spawnPlayer != null)
            return;

        spawnPlayer =
            FindFirstObjectByType<SpawnPlayer>();
    }

    // =====================================================
    // UPDATE
    // =====================================================

    private void Update()
    {
        FindSpawnPlayer();

        if (spawnPlayer == null ||
            spawnPlayer.Object == null ||
            !spawnPlayer.Object.IsValid ||
            spawnPlayer.Runner == null)
        {
            return;
        }

        bool battleStarted = spawnPlayer.BattleStarted;

        // =================================================
        // UPDATE GAMEPLAY UI
        // =================================================

        if (lastBattleState != battleStarted)
        {
            lastBattleState = battleStarted;
            SetGameplayUIVisible(battleStarted);
        }

        // =================================================
        // BATTLE STARTED
        // =================================================

        if (battleStarted)
        {
            if (hideWhenBattleStarts && countdownPanel != null)
                countdownPanel.SetActive(false);

            return;
        }

        // =================================================
        // WAITING MAP
        // =================================================

        if (countdownPanel != null &&
            !countdownPanel.activeSelf)
        {
            countdownPanel.SetActive(true);
        }

        // =================================================
        // WAITING FOR PLAYERS
        // =================================================

        if (!spawnPlayer.CountdownStarted)
        {
            if (countdownText != null)
                countdownText.text = "--:--";

            if (statusText != null)
                statusText.text = "WAITING FOR PLAYERS...";

            lastSeconds = -1;
            return;
        }

        // =================================================
        // COUNTDOWN
        // =================================================

        float? remaining =
            spawnPlayer.WaitingTimer.RemainingTime(
                spawnPlayer.Runner
            );

        if (!remaining.HasValue)
            return;

        int seconds = Mathf.CeilToInt(
            Mathf.Max(0f, remaining.Value)
        );

        if (seconds == lastSeconds)
            return;

        lastSeconds = seconds;

        int minutes = seconds / 60;
        int secs = seconds % 60;

        if (countdownText != null)
        {
            countdownText.text =
                $"{minutes:00}:{secs:00}";
        }

        if (statusText != null)
        {
            statusText.text =
                seconds <= 3
                ? "GET READY!"
                : "MATCH STARTING IN";
        }
    }

    // =====================================================
    // SHOW / HIDE GAMEPLAY UI
    // =====================================================

    private void SetGameplayUIVisible(bool visible)
    {
        if (gameplayUIObjects == null)
            return;

        foreach (GameObject ui in gameplayUIObjects)
        {
            if (ui == null)
                continue;

            // Never hide our countdown UI through this list.
            if (ui == countdownPanel || ui == gameObject)
                continue;

            if (!visible)
            {
                ui.SetActive(false);
            }
            else
            {
                // Restore original active state.
                if (initialUIStates.TryGetValue(
                    ui, out bool wasActive))
                {
                    ui.SetActive(wasActive);
                }
            }
        }

        Debug.Log(
            "[MatchCountdownUI] Gameplay UI " +
            (visible ? "ENABLED" : "DISABLED")
        );
    }
}
