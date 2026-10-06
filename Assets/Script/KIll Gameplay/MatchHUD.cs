using UnityEngine;
using TMPro;
using Fusion;
using System.Collections.Generic;

public class MatchHUD : MonoBehaviour
{
    [Header("Player 1")]
    public TMP_Text player1NameText;
    public TMP_Text player1KillText;

    [Header("Player 2")]
    public TMP_Text player2NameText;
    public TMP_Text player2KillText;

    [Header("Kill Limit")]
    public TMP_Text killLimitTitleText;
    public TMP_Text killLimitText;

    [Header("Time")]
    public TMP_Text timeText;


    // =========================================================
    // WIN UI
    // =========================================================

    [Header("Win UI")]
    public GameObject winPanel;
    public TMP_Text winTitleText;
    public TMP_Text winSubtitleText;
    public TMP_Text winScoreLeftText;
    public TMP_Text winScoreRightText;


    // =========================================================
    // LOSE UI
    // =========================================================

    [Header("Lose UI")]
    public GameObject losePanel;
    public TMP_Text loseTitleText;
    public TMP_Text loseSubtitleText;
    public TMP_Text loseScoreLeftText;
    public TMP_Text loseScoreRightText;


    // =========================================================
    // RESULT AUDIO
    // =========================================================

    [Header("Result Audio")]

    [Tooltip("AudioSource dùng để phát âm thanh thắng / thua")]
    public AudioSource resultAudioSource;

    [Tooltip("Âm thanh phát khi Local Player thắng")]
    public AudioClip winSound;

    [Tooltip("Âm thanh phát khi Local Player thua")]
    public AudioClip loseSound;

    [Range(0f, 1f)]
    public float resultVolume = 1f;


    // =========================================================
    // BACKGROUND MUSIC
    // =========================================================

    [Header("Background Music")]

    [Tooltip("Kéo AudioSource đang phát nhạc nền của game vào đây")]
    public AudioSource backgroundMusicSource;


    // =========================================================
    // PLAYER
    // =========================================================

    private PlayerHealth player1Health;
    private PlayerHealth player2Health;

    private bool resultShown = false;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (player1NameText != null)
            player1NameText.text = "PLAYER 1";

        if (player2NameText != null)
            player2NameText.text = "PLAYER 2";

        if (player1KillText != null)
            player1KillText.text = "0";

        if (player2KillText != null)
            player2KillText.text = "0";

        if (killLimitTitleText != null)
            killLimitTitleText.text = "KILL LIMIT";

        if (killLimitText != null)
            killLimitText.text = "0";

        if (timeText != null)
            timeText.text = "00:00";


        // =====================================================
        // WIN TEXT
        // =====================================================

        if (winTitleText != null)
            winTitleText.text = "VICTORY";

        if (winSubtitleText != null)
            winSubtitleText.text =
                "You Win!!";


        // =====================================================
        // LOSE TEXT
        // =====================================================

        if (loseTitleText != null)
            loseTitleText.text = " DEFEAT";

        if (loseSubtitleText != null)
            loseSubtitleText.text =
                "Try again in the next match!";


        // =====================================================
        // HIDE RESULT UI
        // =====================================================

        if (winPanel != null)
            winPanel.SetActive(false);

        if (losePanel != null)
            losePanel.SetActive(false);


        // =====================================================
        // RESULT AUDIO SOURCE
        // =====================================================

        if (resultAudioSource == null)
        {
            resultAudioSource =
                GetComponent<AudioSource>();
        }

        if (resultAudioSource != null)
        {
            resultAudioSource.playOnAwake = false;
            resultAudioSource.loop = false;
            resultAudioSource.spatialBlend = 0f;

            resultAudioSource.Stop();
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        FindPlayers();


        MatchManager manager =
            MatchManager.Instance;


        if (manager == null)
            return;


        // =====================================================
        // PLAYER 1 SCORE
        // =====================================================

        if (player1KillText != null)
        {
            player1KillText.text =
                (
                    player1Health != null
                        ? player1Health.Kills
                        : 0
                ).ToString();
        }


        // =====================================================
        // PLAYER 2 SCORE
        // =====================================================

        if (player2KillText != null)
        {
            player2KillText.text =
                (
                    player2Health != null
                        ? player2Health.Kills
                        : 0
                ).ToString();
        }


        // =====================================================
        // KILL LIMIT
        // =====================================================

        if (killLimitText != null)
        {
            killLimitText.text =
                manager.killLimit.ToString();
        }


        // =====================================================
        // TIME
        // =====================================================

        UpdateTime(
            manager.GetRemainingTime()
        );


        // =====================================================
        // MATCH END
        // =====================================================

        if (manager.MatchEnded)
        {
            if (!resultShown)
            {
                ShowMatchResult(
                    manager
                );
            }


            LockAllLocalControls();
        }
    }


    // =========================================================
    // FIND PLAYERS
    // =========================================================

    private void FindPlayers()
    {
        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        List<PlayerHealth> validPlayers =
            new List<PlayerHealth>();


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;

            if (player.Object == null)
                continue;

            if (!player.Object.IsValid)
                continue;


            validPlayers.Add(
                player
            );
        }


        validPlayers.Sort(
            (a, b) =>
                a.Object.InputAuthority.RawEncoded.CompareTo(
                    b.Object.InputAuthority.RawEncoded
                )
        );


        player1Health =
            validPlayers.Count >= 1
                ? validPlayers[0]
                : null;


        player2Health =
            validPlayers.Count >= 2
                ? validPlayers[1]
                : null;
    }


    // =========================================================
    // SHOW MATCH RESULT
    // =========================================================

    private void ShowMatchResult(
        MatchManager manager)
    {
        // Chờ LocalPlayer có đầy đủ trước.
        if (PlayerMovement.LocalPlayer == null)
        {
            Debug.LogWarning(
                "[MatchHUD] Không tìm thấy LocalPlayer."
            );

            return;
        }


        PlayerHealth localHealth =
            PlayerMovement.LocalPlayer
                .GetComponent<PlayerHealth>();


        NetworkObject localObject =
            PlayerMovement.LocalPlayer
                .GetComponent<NetworkObject>();


        if (localObject == null)
        {
            Debug.LogWarning(
                "[MatchHUD] LocalPlayer không có NetworkObject."
            );

            return;
        }


        resultShown = true;


        // =====================================================
        // PAUSE BACKGROUND MUSIC
        // =====================================================

        PauseBackgroundMusic();


        PlayerRef localPlayerRef =
            localObject.InputAuthority;


        PlayerHealth otherHealth =
            null;


        if (player1Health != null &&
            player1Health != localHealth)
        {
            otherHealth =
                player1Health;
        }


        if (player2Health != null &&
            player2Health != localHealth)
        {
            otherHealth =
                player2Health;
        }


        int localKills =
            localHealth != null
                ? localHealth.Kills
                : 0;


        int otherKills =
            otherHealth != null
                ? otherHealth.Kills
                : 0;


        // =====================================================
        // DRAW
        // =====================================================

        if (manager.Winner ==
            PlayerRef.None)
        {
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


            if (resultAudioSource != null)
            {
                resultAudioSource.Stop();
            }


            Debug.Log(
                "[MatchHUD] DRAW | " +
                localKills +
                " - " +
                otherKills
            );


            return;
        }


        // =====================================================
        // WIN
        // =====================================================

        if (localPlayerRef ==
            manager.Winner)
        {
            if (winPanel != null)
            {
                winPanel.SetActive(
                    true
                );
            }


            if (losePanel != null)
            {
                losePanel.SetActive(
                    false
                );
            }


            if (winScoreLeftText != null)
            {
                winScoreLeftText.text =
                    localKills.ToString();
            }


            if (winScoreRightText != null)
            {
                winScoreRightText.text =
                    otherKills.ToString();
            }


            PlayWinSound();


            Debug.Log(
                "[MatchHUD] YOU WIN | " +
                localKills +
                " - " +
                otherKills
            );
        }

        // =====================================================
        // LOSE
        // =====================================================

        else
        {
            if (winPanel != null)
            {
                winPanel.SetActive(
                    false
                );
            }


            if (losePanel != null)
            {
                losePanel.SetActive(
                    true
                );
            }


            if (loseScoreLeftText != null)
            {
                loseScoreLeftText.text =
                    localKills.ToString();
            }


            if (loseScoreRightText != null)
            {
                loseScoreRightText.text =
                    otherKills.ToString();
            }


            PlayLoseSound();


            Debug.Log(
                "[MatchHUD] YOU LOSE | " +
                localKills +
                " - " +
                otherKills
            );
        }
    }


    // =========================================================
    // BACKGROUND MUSIC
    // =========================================================

    private void PauseBackgroundMusic()
    {
        if (backgroundMusicSource == null)
            return;


        if (backgroundMusicSource.isPlaying)
        {
            backgroundMusicSource.Pause();
        }
    }


    public void ResumeBackgroundMusic()
    {
        if (backgroundMusicSource == null)
            return;


        backgroundMusicSource.UnPause();
    }


    // =========================================================
    // WIN SOUND
    // =========================================================

    private void PlayWinSound()
    {
        if (resultAudioSource == null)
            return;

        if (winSound == null)
            return;


        resultAudioSource.Stop();


        resultAudioSource.PlayOneShot(
            winSound,
            resultVolume
        );
    }


    // =========================================================
    // LOSE SOUND
    // =========================================================

    private void PlayLoseSound()
    {
        if (resultAudioSource == null)
            return;

        if (loseSound == null)
            return;


        resultAudioSource.Stop();


        resultAudioSource.PlayOneShot(
            loseSound,
            resultVolume
        );
    }


    // =========================================================
    // LOCK ALL LOCAL CONTROLS
    // =========================================================

    private void LockAllLocalControls()
    {
        if (PlayerMovement.LocalPlayer == null)
            return;


        GameObject localPlayer =
            PlayerMovement.LocalPlayer.gameObject;


        // =====================================================
        // MOVEMENT
        // =====================================================

        PlayerMovement movement =
            localPlayer.GetComponent<PlayerMovement>();


        if (movement != null)
        {
            movement.ResetMovementState();

            movement.enabled =
                false;
        }


        // =====================================================
        // WEAPON
        // =====================================================

        PlayerWeapon weapon =
            localPlayer.GetComponent<PlayerWeapon>();


        if (weapon != null)
        {
            weapon.enabled =
                false;
        }


        // =====================================================
        // CHARACTER CONTROLLER
        // =====================================================

        CharacterController controller =
            localPlayer.GetComponent<CharacterController>();


        if (controller != null &&
            controller.enabled)
        {
            controller.enabled =
                false;
        }


        // =====================================================
        // CAMERA
        // =====================================================

        ThirdPersonCamera[] cameraControllers =
            FindObjectsByType<ThirdPersonCamera>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
            );


        foreach (
            ThirdPersonCamera cameraController
            in cameraControllers)
        {
            if (cameraController == null ||
                cameraController.target == null)
            {
                continue;
            }


            Transform target =
                cameraController.target;


            bool belongsToLocalPlayer =
                target == localPlayer.transform
                ||
                target.IsChildOf(
                    localPlayer.transform
                )
                ||
                localPlayer.transform.IsChildOf(
                    target
                );


            if (belongsToLocalPlayer)
            {
                cameraController.enabled =
                    false;
            }
        }


        // =====================================================
        // CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;
    }


    // =========================================================
    // UPDATE TIME
    // =========================================================

    private void UpdateTime(
        float remainingTime)
    {
        if (timeText == null)
            return;


        int totalSeconds =
            Mathf.Max(
                0,
                Mathf.CeilToInt(
                    remainingTime
                )
            );


        int minutes =
            totalSeconds / 60;


        int seconds =
            totalSeconds % 60;


        timeText.text =
            minutes.ToString("00") +
            ":" +
            seconds.ToString("00");
    }
}