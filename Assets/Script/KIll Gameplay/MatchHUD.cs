using UnityEngine;
using TMPro;
using Fusion;
using System.Collections;
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

    [Header("Cinematic Audio")]
    [Tooltip("AudioSource chỉ phát âm thanh cinematic kết thúc trận.")]
    public AudioSource resultAudioSource;
    public AudioClip matchEndCinematicSound;
    [Range(0f, 1f)] public float resultVolume = 1f;

    private void PlayMatchEndCinematicSound()
    {
        if (resultAudioSource == null || matchEndCinematicSound == null) return;
        resultAudioSource.Stop();
        resultAudioSource.PlayOneShot(matchEndCinematicSound, resultVolume);
    }


    // =========================================================
    // BACKGROUND MUSIC
    // =========================================================

    [Header("Background Music")]

    [Tooltip("Kéo AudioSource đang phát nhạc nền của game vào đây")]
    public AudioSource backgroundMusicSource;

    // =========================================================
    // MATCH END CINEMATIC
    // =========================================================

    [Header("Match End Cinematic")]

    [Tooltip("CanvasGroup của Image đen full màn hình")]
    public CanvasGroup matchEndFade;

    [Tooltip("Có thể để trống, script sẽ tự lấy Camera.main")]
    public Camera cinematicCamera;

    [Tooltip("Vị trí camera so với Player thắng")]
    public Vector3 cinematicCameraOffset =
        new Vector3(
            0f,
            2.2f,
            -4f
        );

    [Tooltip("Điểm camera nhìn vào trên người Player")]
    public float cinematicLookHeight = 1.3f;

    [Tooltip("Thời gian màn hình tối dần")]
    public float fadeToBlackDuration = 0.7f;

    [Tooltip("Thời gian camera bay tới Player")]
    public float cameraFlyDuration = 2.5f;

    [Tooltip("Thời gian màn hình sáng trở lại")]
    public float fadeFromBlackDuration = 0.8f;

    [Tooltip("Thời gian giữ camera trước khi hiện Win/Lose")]
    public float cinematicHoldTime = 1f;


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
            player1NameText.text = "Team 1";

        if (player2NameText != null)
            player2NameText.text = "Team 2";

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

        // =====================================================
        // RESET MATCH END FADE
        // =====================================================

        if (matchEndFade != null)
        {
            matchEndFade.alpha = 0f;

            matchEndFade.blocksRaycasts = false;

            matchEndFade.gameObject.SetActive(
                false
            );
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
        // =====================================================
        // LOCAL PLAYER
        // =====================================================

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


        // Không cho gọi lại nhiều lần
        resultShown = true;


        // =====================================================
        // PAUSE MUSIC
        // =====================================================

        PauseBackgroundMusic();


        // =====================================================
        // LOCAL PLAYER REF
        // =====================================================

        PlayerRef localPlayerRef =
            localObject.InputAuthority;


        // =====================================================
        // OTHER PLAYER
        // =====================================================

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


        // =====================================================
        // SCORE
        // =====================================================

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
                winPanel.SetActive(false);
            }


            if (losePanel != null)
            {
                losePanel.SetActive(false);
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
        // LOCAL WIN?
        // =====================================================

        bool localWon =
            localPlayerRef ==
            manager.Winner;



        // =====================================================
        // SET SCORE TRƯỚC
        // =====================================================

        if (localWon)
        {
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
        }
        else
        {
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
        }


        // =====================================================
        // ĐẢM BẢO PANEL CHƯA HIỆN
        // =====================================================

        if (winPanel != null)
        {
            winPanel.SetActive(false);
        }


        if (losePanel != null)
        {
            losePanel.SetActive(false);
        }


        // =====================================================
        // PLAY CINEMATIC SOUND
        // =====================================================

        PlayMatchEndCinematicSound();


        // =====================================================
        // PLAY CINEMATIC
        // =====================================================

        StartCoroutine(
            MatchEndCinematicRoutine(
                manager.Winner,
                localWon,
                localKills,
                otherKills
            )
        );
    }

    // =========================================================
    // MATCH END CINEMATIC
    // =========================================================

    private IEnumerator MatchEndCinematicRoutine(
        PlayerRef targetPlayerRef,
        bool localWon,
        int localKills,
        int otherKills)
    {

        // =====================================================
        // TÌM CAMERA
        // =====================================================

        if (cinematicCamera == null)
        {
            cinematicCamera =
                Camera.main;
        }


        // =====================================================
        // TÌM PLAYER MỤC TIÊU
        // =====================================================

        Transform targetPlayer =
            FindPlayerTransform(
                targetPlayerRef
            );


        // =====================================================
        // FADE TO BLACK
        // =====================================================

        yield return StartCoroutine(
            FadeMatchScreen(
                0f,
                1f,
                fadeToBlackDuration
            )
        );


        // =====================================================
        // CAMERA
        // =====================================================

        if (cinematicCamera != null &&
            targetPlayer != null)
        {
            // Camera bắt đầu ở vị trí hiện tại
            Vector3 startPosition =
                cinematicCamera
                    .transform
                    .position;


            Quaternion startRotation =
                cinematicCamera
                    .transform
                    .rotation;


            // =================================================
            // VỊ TRÍ CAMERA CUỐI
            // =================================================

            Vector3 targetPosition =
                targetPlayer.TransformPoint(
                    cinematicCameraOffset
                );


            Vector3 lookPoint =
                targetPlayer.position +
                Vector3.up *
                cinematicLookHeight;


            Quaternion targetRotation =
                Quaternion.LookRotation(
                    lookPoint -
                    targetPosition
                );


            // =================================================
            // BẮT ĐẦU SÁNG LẠI
            // =================================================

            StartCoroutine(
                FadeMatchScreen(
                    1f,
                    0f,
                    fadeFromBlackDuration
                )
            );


            // =================================================
            // CAMERA BAY
            // =================================================

            float timer =
                0f;


            while (timer <
                   cameraFlyDuration)
            {
                timer +=
                    Time.unscaledDeltaTime;


                float t =
                    Mathf.Clamp01(
                        timer /
                        cameraFlyDuration
                    );


                // Smooth Step
                float smoothT =
                    t *
                    t *
                    (3f - 2f * t);


                cinematicCamera
                    .transform
                    .position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        smoothT
                    );


                cinematicCamera
                    .transform
                    .rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        smoothT
                    );


                yield return null;
            }


            // =================================================
            // ĐẢM BẢO CAMERA ĐÚNG VỊ TRÍ CUỐI
            // =================================================

            cinematicCamera
                .transform
                .position =
                targetPosition;


            cinematicCamera
                .transform
                .LookAt(
                    lookPoint
                );
        }
        else
        {
            // Không tìm được Player thì vẫn sáng lại
            yield return StartCoroutine(
                FadeMatchScreen(
                    1f,
                    0f,
                    fadeFromBlackDuration
                )
            );
        }


        // =====================================================
        // HOLD CAMERA
        // =====================================================

        yield return
            new WaitForSecondsRealtime(
                cinematicHoldTime
            );



        // =====================================================
        // SHOW WIN
        // =====================================================

        if (localWon)
        {
            if (losePanel != null)
            {
                losePanel.SetActive(
                    false
                );
            }


            if (winPanel != null)
            {
                winPanel.SetActive(
                    true
                );
            }





            Debug.Log(
                "[MatchHUD] YOU WIN | " +
                localKills +
                " - " +
                otherKills
            );
        }

        // =====================================================
        // SHOW LOSE
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





            Debug.Log(
                "[MatchHUD] YOU LOSE | " +
                localKills +
                " - " +
                otherKills
            );
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
    // FIND CINEMATIC PLAYER
    // =========================================================

    private Transform FindPlayerTransform(
        PlayerRef playerRef)
    {
        if (playerRef ==
            PlayerRef.None)
        {
            return null;
        }


        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        foreach (PlayerHealth player in players)
        {
            if (player == null)
                continue;


            if (player.Object == null)
                continue;


            if (!player.Object.IsValid)
                continue;


            if (player.Object.InputAuthority !=
                playerRef)
            {
                continue;
            }


            return player.transform;
        }


        return null;
    }

    // =========================================================
    // FADE MATCH SCREEN
    // =========================================================

    private IEnumerator FadeMatchScreen(
        float from,
        float to,
        float duration)
    {
        // =====================================================
        // CHECK FADE
        // =====================================================

        if (matchEndFade == null)
        {
            yield break;
        }


        // =====================================================
        // BẬT MÀN HÌNH FADE
        // =====================================================

        matchEndFade
            .gameObject
            .SetActive(
                true
            );


        // =====================================================
        // ĐƯA FADE LÊN TRÊN CÙNG UI
        //
        // Rất quan trọng:
        // WinPanel / LosePanel / HUD sẽ không che màn đen.
        // =====================================================

        matchEndFade
            .transform
            .SetAsLastSibling();


        // =====================================================
        // ALPHA BAN ĐẦU
        // =====================================================

        matchEndFade.alpha =
            from;


        // Không chặn click.
        matchEndFade.blocksRaycasts =
            false;


        float timer =
            0f;


        duration =
            Mathf.Max(
                0.01f,
                duration
            );


        // =====================================================
        // FADE
        // =====================================================

        while (timer <
               duration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            // Smooth hơn Linear một chút
            float smoothT =
                t *
                t *
                (3f - 2f * t);


            matchEndFade.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    smoothT
                );


            yield return null;
        }


        // =====================================================
        // ĐẢM BẢO GIÁ TRỊ CUỐI
        // =====================================================

        matchEndFade.alpha =
            to;


        // =====================================================
        // NẾU ĐÃ SÁNG LẠI HOÀN TOÀN
        // THÌ TẮT OBJECT FADE
        // =====================================================

        if (to <= 0f)
        {
            matchEndFade
                .gameObject
                .SetActive(
                    false
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
                FindObjectsInactive.Include
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