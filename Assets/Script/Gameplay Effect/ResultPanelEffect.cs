using UnityEngine;
using System.Collections;

public class ResultPanelEffect : MonoBehaviour
{
    [Header("UI cần ẩn khi kết thúc trận đấu")]
    public GameObject weaponHUD;
    public GameObject matchHUD;
    public GameObject topLeftUI;

    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform panelRect;


    [Header("Timing")]

    public float fadeDuration =
        0.25f;

    public float scaleDuration =
        0.35f;

    public float settleDuration =
        0.12f;


    [Header("Scale")]

    public Vector3 startScale =
        new Vector3(
            0.7f,
            0.7f,
            1f
        );

    public Vector3 overshootScale =
        new Vector3(
            1.05f,
            1.05f,
            1f
        );

    public Vector3 finalScale =
        Vector3.one;


    private Coroutine effectCoroutine;


    // =========================================================
    // ON ENABLE
    // =========================================================

    private void OnEnable()
    {
        weaponHUD.SetActive(false);
        matchHUD.SetActive(false);
        topLeftUI.SetActive(false);
        PlayEffect();
    }


    // =========================================================
    // PLAY EFFECT
    // =========================================================

    public void PlayEffect()
    {
        if (!gameObject.activeInHierarchy)
            return;


        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );
        }


        effectCoroutine =
            StartCoroutine(
                PlayRoutine()
            );
    }


    // =========================================================
    // ROUTINE
    // =========================================================

    private IEnumerator PlayRoutine()
    {
        // =====================================================
        // RESET
        // =====================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                0f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                startScale;
        }


        // =====================================================
        // FADE
        // =====================================================

        float timer =
            0f;


        while (timer <
               fadeDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        fadeDuration
                    )
                );


            if (canvasGroup != null)
            {
                canvasGroup.alpha =
                    t;
            }


            yield return null;
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;
        }


        // =====================================================
        // SCALE UP
        // =====================================================

        timer =
            0f;


        while (timer <
               scaleDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        scaleDuration
                    )
                );


            float smoothT =
                Mathf.SmoothStep(
                    0f,
                    1f,
                    t
                );


            if (panelRect != null)
            {
                panelRect.localScale =
                    Vector3.Lerp(
                        startScale,
                        overshootScale,
                        smoothT
                    );
            }


            yield return null;
        }


        // =====================================================
        // SETTLE
        // =====================================================

        timer =
            0f;


        while (timer <
               settleDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    Mathf.Max(
                        0.01f,
                        settleDuration
                    )
                );


            if (panelRect != null)
            {
                panelRect.localScale =
                    Vector3.Lerp(
                        overshootScale,
                        finalScale,
                        t
                    );
            }


            yield return null;
        }


        // =====================================================
        // FINAL
        // =====================================================

        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                finalScale;
        }


        effectCoroutine =
            null;
    }


    // =========================================================
    // ON DISABLE
    // =========================================================

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(
                effectCoroutine
            );

            effectCoroutine =
                null;
        }


        if (canvasGroup != null)
        {
            canvasGroup.alpha =
                1f;
        }


        if (panelRect != null)
        {
            panelRect.localScale =
                finalScale;
        }
    }
}