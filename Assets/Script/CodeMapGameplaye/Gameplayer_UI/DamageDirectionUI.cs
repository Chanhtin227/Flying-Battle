using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class DamageDirectionUI : MonoBehaviour
{
    // =========================================================
    // UI REFERENCES
    // =========================================================

    [Header("UI References")]

    public Image redFlash;

    public Image directionArrow;

    public Image directionGlow;


    // =========================================================
    // EFFECT SETTINGS
    // =========================================================

    [Header("Effect Settings")]

    [Tooltip("Thời gian toàn bộ hiệu ứng")]
    public float effectDuration = 0.65f;

    [Range(0f, 1f)]
    [Tooltip("Độ sáng viền đỏ")]
    public float maxFlashAlpha = 0.22f;

    [Range(0f, 1f)]
    [Tooltip("Độ sáng Arc")]
    public float maxArrowAlpha = 1f;

    [Range(0f, 1f)]
    [Tooltip("Độ sáng Glow")]
    public float maxGlowAlpha = 0.45f;


    // =========================================================
    // SCREEN EDGE
    // =========================================================

    [Header("Screen Edge")]

    [Tooltip("Khoảng cách Arc cách mép màn hình")]
    public float edgeMargin = 90f;

    [Tooltip("Arc bắt đầu nằm ngoài màn hình bao nhiêu")]
    public float startOutsideDistance = 70f;


    // =========================================================
    // ARC ANIMATION
    // =========================================================

    [Header("Arc Animation")]

    [Tooltip("Scale lúc bắt đầu")]
    public float startScale = 1.35f;

    [Tooltip("Scale cuối")]
    public float endScale = 1f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private CanvasGroup redFlashGroup;
    private CanvasGroup arrowGroup;
    private CanvasGroup glowGroup;

    private RectTransform arrowRect;
    private RectTransform glowRect;

    private RectTransform canvasRect;

    private Coroutine effectCoroutine;


    // =========================================================
    // STORED POSITION
    // =========================================================

    private Vector2 targetEdgePosition;

    private Vector2 targetDirection;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // CANVAS
        // =====================================================

        Canvas canvas =
            GetComponentInParent<Canvas>();

        if (canvas != null)
        {
            canvasRect =
                canvas.GetComponent<RectTransform>();
        }


        // =====================================================
        // RED FLASH
        // =====================================================

        if (redFlash != null)
        {
            redFlashGroup =
                redFlash.GetComponent<CanvasGroup>();

            if (redFlashGroup == null)
            {
                redFlashGroup =
                    redFlash.gameObject.AddComponent<CanvasGroup>();
            }

            redFlashGroup.alpha = 0f;

            redFlash.raycastTarget = false;
        }


        // =====================================================
        // ARROW
        // =====================================================

        if (directionArrow != null)
        {
            arrowGroup =
                directionArrow.GetComponent<CanvasGroup>();

            if (arrowGroup == null)
            {
                arrowGroup =
                    directionArrow.gameObject.AddComponent<CanvasGroup>();
            }

            arrowGroup.alpha = 0f;

            arrowRect =
                directionArrow.rectTransform;

            directionArrow.raycastTarget = false;

            arrowRect.localScale =
                Vector3.one * endScale;
        }


        // =====================================================
        // GLOW
        // =====================================================

        if (directionGlow != null)
        {
            glowGroup =
                directionGlow.GetComponent<CanvasGroup>();

            if (glowGroup == null)
            {
                glowGroup =
                    directionGlow.gameObject.AddComponent<CanvasGroup>();
            }

            glowGroup.alpha = 0f;

            glowRect =
                directionGlow.rectTransform;

            directionGlow.raycastTarget = false;

            glowRect.localScale =
                Vector3.one * endScale;
        }
    }


    // =========================================================
    // SHOW DAMAGE
    // =========================================================

    public void ShowDamage(Vector3 attackerPosition)
    {
        // =====================================================
        // CAMERA
        // =====================================================

        Camera cam =
            Camera.main;

        if (cam == null)
        {
            return;
        }


        // =====================================================
        // LOCAL PLAYER
        // =====================================================

        if (PlayerMovement.LocalPlayer == null)
        {
            return;
        }


        Transform player =
            PlayerMovement.LocalPlayer.transform;


        // =====================================================
        // WORLD DIRECTION
        // =====================================================

        Vector3 direction =
            attackerPosition -
            player.position;

        direction.y = 0f;


        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }


        direction.Normalize();


        // =====================================================
        // CAMERA FORWARD
        // =====================================================

        Vector3 cameraForward =
            cam.transform.forward;

        cameraForward.y = 0f;

        if (cameraForward.sqrMagnitude < 0.001f)
        {
            cameraForward =
                player.forward;

            cameraForward.y = 0f;
        }

        cameraForward.Normalize();


        // =====================================================
        // CAMERA RIGHT
        // =====================================================

        Vector3 cameraRight =
            cam.transform.right;

        cameraRight.y = 0f;

        if (cameraRight.sqrMagnitude < 0.001f)
        {
            cameraRight =
                Vector3.Cross(
                    Vector3.up,
                    cameraForward
                );
        }

        cameraRight.Normalize();


        // =====================================================
        // CAMERA SPACE DIRECTION
        // =====================================================

        float forwardAmount =
            Vector3.Dot(
                cameraForward,
                direction
            );

        float rightAmount =
            Vector3.Dot(
                cameraRight,
                direction
            );


        Vector2 screenDirection =
            new Vector2(
                rightAmount,
                forwardAmount
            ).normalized;


        // =====================================================
        // SAVE DIRECTION
        // =====================================================

        targetDirection =
            screenDirection;


        // =====================================================
        // CALCULATE EDGE POSITION
        // =====================================================

        targetEdgePosition =
            GetScreenEdgePosition(
                screenDirection
            );


        // =====================================================
        // ROTATE ARC
        // =====================================================

        float angle =
            Mathf.Atan2(
                screenDirection.y,
                screenDirection.x
            ) * Mathf.Rad2Deg;


        if (arrowRect != null)
        {
            arrowRect.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle - 90f
                );
        }


        if (glowRect != null)
        {
            glowRect.rotation =
                Quaternion.Euler(
                    0f,
                    0f,
                    angle - 90f
                );
        }


        // =====================================================
        // STOP OLD EFFECT
        // =====================================================

        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }


        // =====================================================
        // START EFFECT
        // =====================================================

        effectCoroutine =
            StartCoroutine(
                DamageEffectCoroutine()
            );
    }


    // =========================================================
    // CALCULATE SCREEN EDGE
    // =========================================================

    private Vector2 GetScreenEdgePosition(
        Vector2 direction
    )
    {
        if (canvasRect == null)
        {
            return Vector2.zero;
        }


        float halfWidth =
            canvasRect.rect.width * 0.5f;

        float halfHeight =
            canvasRect.rect.height * 0.5f;


        float x =
            direction.x;

        float y =
            direction.y;


        float scaleX =
            Mathf.Abs(x) > 0.001f
                ? (halfWidth - edgeMargin) /
                  Mathf.Abs(x)
                : float.MaxValue;


        float scaleY =
            Mathf.Abs(y) > 0.001f
                ? (halfHeight - edgeMargin) /
                  Mathf.Abs(y)
                : float.MaxValue;


        float scale =
            Mathf.Min(
                scaleX,
                scaleY
            );


        return direction * scale;
    }


    // =========================================================
    // EFFECT
    // =========================================================

    private IEnumerator DamageEffectCoroutine()
    {
        float duration =
            Mathf.Max(
                0.05f,
                effectDuration
            );


        // =====================================================
        // CALCULATE START POSITION
        // =====================================================

        Vector2 startPosition =
            targetEdgePosition +
            targetDirection *
            startOutsideDistance;


        // =====================================================
        // START
        // =====================================================

        if (arrowRect != null)
        {
            arrowRect.anchoredPosition =
                startPosition;

            arrowRect.localScale =
                Vector3.one *
                startScale;
        }


        if (glowRect != null)
        {
            glowRect.anchoredPosition =
                startPosition;

            glowRect.localScale =
                Vector3.one *
                (startScale * 1.08f);
        }


        if (redFlashGroup != null)
        {
            redFlashGroup.alpha = 0f;
        }

        if (arrowGroup != null)
        {
            arrowGroup.alpha = 0f;
        }

        if (glowGroup != null)
        {
            glowGroup.alpha = 0f;
        }


        // =====================================================
        // POP IN
        // =====================================================

        float popDuration =
            Mathf.Min(
                0.18f,
                duration * 0.3f
            );


        float timer = 0f;


        while (timer < popDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    popDuration
                );


            float eased =
                1f -
                Mathf.Pow(
                    1f - t,
                    3f
                );


            // =================================================
            // POSITION
            // =================================================

            Vector2 position =
                Vector2.Lerp(
                    startPosition,
                    targetEdgePosition,
                    eased
                );


            if (arrowRect != null)
            {
                arrowRect.anchoredPosition =
                    position;

                float scale =
                    Mathf.Lerp(
                        startScale,
                        endScale,
                        eased
                    );

                arrowRect.localScale =
                    Vector3.one *
                    scale;
            }


            if (glowRect != null)
            {
                glowRect.anchoredPosition =
                    position;

                float scale =
                    Mathf.Lerp(
                        startScale * 1.08f,
                        endScale * 1.05f,
                        eased
                    );

                glowRect.localScale =
                    Vector3.one *
                    scale;
            }


            // =================================================
            // ALPHA
            // =================================================

            float alpha =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            if (redFlashGroup != null)
            {
                redFlashGroup.alpha =
                    maxFlashAlpha *
                    alpha;
            }


            if (arrowGroup != null)
            {
                arrowGroup.alpha =
                    maxArrowAlpha *
                    alpha;
            }


            if (glowGroup != null)
            {
                glowGroup.alpha =
                    maxGlowAlpha *
                    alpha;
            }


            yield return null;
        }


        // =====================================================
        // HOLD
        // =====================================================

        float holdDuration =
            Mathf.Min(
                0.15f,
                duration * 0.25f
            );


        yield return
            new WaitForSeconds(
                holdDuration
            );


        // =====================================================
        // FADE
        // =====================================================

        float fadeDuration =
            Mathf.Max(
                0.05f,
                duration -
                popDuration -
                holdDuration
            );


        timer = 0f;


        while (timer < fadeDuration)
        {
            timer +=
                Time.deltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    fadeDuration
                );


            float alpha =
                1f -
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            // =================================================
            // SLIGHT OUTWARD MOVEMENT
            // =================================================

            Vector2 position =
                Vector2.Lerp(
                    targetEdgePosition,
                    targetEdgePosition +
                    targetDirection *
                    12f,
                    t
                );


            if (arrowRect != null)
            {
                arrowRect.anchoredPosition =
                    position;

                arrowRect.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        endScale,
                        endScale * 1.06f,
                        t
                    );
            }


            if (glowRect != null)
            {
                glowRect.anchoredPosition =
                    position;

                glowRect.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        endScale * 1.05f,
                        endScale * 1.12f,
                        t
                    );
            }


            // =================================================
            // ALPHA
            // =================================================

            if (redFlashGroup != null)
            {
                redFlashGroup.alpha =
                    maxFlashAlpha *
                    alpha;
            }


            if (arrowGroup != null)
            {
                arrowGroup.alpha =
                    maxArrowAlpha *
                    alpha;
            }


            if (glowGroup != null)
            {
                glowGroup.alpha =
                    maxGlowAlpha *
                    alpha;
            }


            yield return null;
        }


        // =====================================================
        // HIDE
        // =====================================================

        if (redFlashGroup != null)
        {
            redFlashGroup.alpha = 0f;
        }

        if (arrowGroup != null)
        {
            arrowGroup.alpha = 0f;
        }

        if (glowGroup != null)
        {
            glowGroup.alpha = 0f;
        }


        effectCoroutine = null;
    }
}