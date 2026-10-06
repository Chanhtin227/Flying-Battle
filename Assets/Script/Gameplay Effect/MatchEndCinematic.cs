using UnityEngine;
using Fusion;
using System.Collections;

public class MatchEndCinematic : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("References")]

    public CanvasGroup fadeCanvasGroup;

    public GameObject winPanel;

    public GameObject losePanel;


    // =========================================================
    // CAMERA
    // =========================================================

    [Header("Camera")]

    [Tooltip("Có thể để trống, script sẽ dùng Camera.main")]
    public Camera cinematicCamera;


    [Tooltip("Vị trí camera so với Player mục tiêu")]
    public Vector3 cameraOffset =
        new Vector3(
            0f,
            2.2f,
            -4f
        );


    [Tooltip("Camera nhìn cao hơn tâm Player một chút")]
    public float lookHeight =
        1.3f;


    // =========================================================
    // EFFECT SETTINGS
    // =========================================================

    [Header("Effect Settings")]

    public float fadeToBlackDuration =
        0.7f;

    public float cameraFlyDuration =
        2.5f;

    public float fadeFromBlackDuration =
        1f;

    public float cinematicHoldTime =
        1f;


    // =========================================================
    // PRIVATE
    // =========================================================

    private bool isPlaying =
        false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        if (fadeCanvasGroup != null)
        {
            fadeCanvasGroup.alpha =
                0f;

            fadeCanvasGroup.blocksRaycasts =
                false;
        }
    }


    // =========================================================
    // PLAY
    // =========================================================

    public void PlayResultCinematic(
        PlayerRef winner,
        bool localPlayerWon)
    {
        if (isPlaying)
            return;


        StartCoroutine(
            ResultRoutine(
                winner,
                localPlayerWon
            )
        );
    }


    // =========================================================
    // ROUTINE
    // =========================================================

    private IEnumerator ResultRoutine(
        PlayerRef winner,
        bool localPlayerWon)
    {
        isPlaying =
            true;


        // =====================================================
        // ẨN WIN / LOSE TRƯỚC
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
        // CAMERA
        // =====================================================

        if (cinematicCamera == null)
        {
            cinematicCamera =
                Camera.main;
        }


        // =====================================================
        // FADE TO BLACK
        // =====================================================

        yield return
            StartCoroutine(
                Fade(
                    0f,
                    1f,
                    fadeToBlackDuration
                )
            );


        // =====================================================
        // FIND WINNER / KILLER
        // =====================================================

        Transform target =
            FindPlayerTransform(
                winner
            );


        // =====================================================
        // CAMERA CINEMATIC
        // =====================================================

        if (cinematicCamera != null &&
            target != null)
        {
            Vector3 startPosition =
                cinematicCamera.transform.position;


            Quaternion startRotation =
                cinematicCamera.transform.rotation;


            Vector3 targetPosition =
                target.TransformPoint(
                    cameraOffset
                );


            Vector3 targetLookPoint =
                target.position +
                Vector3.up *
                lookHeight;


            Quaternion targetRotation =
                Quaternion.LookRotation(
                    targetLookPoint -
                    targetPosition
                );


            float timer =
                0f;


            // =============================================
            // FADE RA KHỎI MÀU ĐEN
            // =============================================

            StartCoroutine(
                Fade(
                    1f,
                    0f,
                    fadeFromBlackDuration
                )
            );


            // =============================================
            // CAMERA BAY VỀ PLAYER
            // =============================================

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


                // Smooth cinematic
                float smoothT =
                    t *
                    t *
                    (3f - 2f * t);


                cinematicCamera.transform.position =
                    Vector3.Lerp(
                        startPosition,
                        targetPosition,
                        smoothT
                    );


                cinematicCamera.transform.rotation =
                    Quaternion.Slerp(
                        startRotation,
                        targetRotation,
                        smoothT
                    );


                yield return null;
            }


            cinematicCamera.transform.position =
                targetPosition;


            cinematicCamera.transform.LookAt(
                targetLookPoint
            );
        }
        else
        {
            // Không tìm được Player
            // vẫn fade trở lại.
            yield return
                StartCoroutine(
                    Fade(
                        1f,
                        0f,
                        fadeFromBlackDuration
                    )
                );
        }


        // =====================================================
        // GIỮ CINEMATIC
        // =====================================================

        yield return
            new WaitForSecondsRealtime(
                cinematicHoldTime
            );


        // =====================================================
        // SHOW RESULT
        // =====================================================

        if (localPlayerWon)
        {
            if (winPanel != null)
            {
                winPanel.SetActive(
                    true
                );
            }
        }
        else
        {
            if (losePanel != null)
            {
                losePanel.SetActive(
                    true
                );
            }
        }


        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        isPlaying =
            false;
    }


    // =========================================================
    // FIND PLAYER
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


        foreach (PlayerHealth player
                 in players)
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
    // FADE
    // =========================================================

    private IEnumerator Fade(
        float from,
        float to,
        float duration)
    {
        if (fadeCanvasGroup == null)
        {
            yield break;
        }


        fadeCanvasGroup.gameObject.SetActive(
            true
        );


        float timer =
            0f;


        fadeCanvasGroup.alpha =
            from;


        while (timer < duration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    duration
                );


            fadeCanvasGroup.alpha =
                Mathf.Lerp(
                    from,
                    to,
                    t
                );


            yield return null;
        }


        fadeCanvasGroup.alpha =
            to;


        if (to <= 0f)
        {
            fadeCanvasGroup.gameObject.SetActive(
                false
            );
        }
    }
}