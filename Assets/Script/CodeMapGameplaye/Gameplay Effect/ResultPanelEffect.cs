
using UnityEngine;
using System.Collections;

public class ResultPanelEffect : MonoBehaviour
{
    // =====================================================
    // REFERENCES
    // =====================================================

    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform panelRect;

    // =====================================================
    // TIMING
    // =====================================================

    [Header("Timing")]
    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.35f;
    public float settleDuration = 0.12f;

    // =====================================================
    // SCALE
    // =====================================================

    [Header("Scale")]
    public Vector3 startScale =
        new Vector3(0.7f, 0.7f, 1f);

    public Vector3 overshootScale =
        new Vector3(1.05f, 1.05f, 1f);

    public Vector3 finalScale = Vector3.one;

    private Coroutine effectCoroutine;

    // =====================================================
    // RESULT TYPE
    // =====================================================

    public enum ResultType
    {
        Win,
        Lose
    }

    [Header("Result Type")]
    public ResultType resultType = ResultType.Win;

    // =====================================================
    // GAMEPLAY HUD
    // =====================================================

    [Header("Hide HUD Before Win / Lose Effect")]
    public GameObject weaponHUD;
    public GameObject matchHUD;
    public GameObject topLeftUI;
    public GameObject jetpackHUD;

    [Header("Optional Extra UI")]
    public GameObject[] extraUIToHide;

    [Header("HUD Settings")]
    [Tooltip("An HUD bang CanvasGroup, khong tat GameObject.")]
    public bool hideHUDOnResult = true;

    // =====================================================
    // RESULT AUDIO
    // =====================================================

    [Header("Result Audio")]
    public AudioSource resultAudioSource;
    public AudioClip winSound;
    public AudioClip loseSound;

    [Range(0f, 1f)]
    public float resultVolume = 1f;

    private bool hasPlayedResultSound = false;

    // =====================================================
    // ON ENABLE
    // =====================================================

    private void OnEnable()
    {
        if (hideHUDOnResult)
            HideGameplayUI();

        if (!hasPlayedResultSound)
        {
            PlayResultSound();
            hasPlayedResultSound = true;
        }

        PlayEffect();
    }

    // =====================================================
    // PRE RESULT EFFECT
    // =====================================================

    // Goi ham nay tai thoi diem bat dau hieu ung
    // truoc WIN / LOSE neu muon an HUD som hon.
    //
    // Ham nay KHONG bat WinPanel / LosePanel.
    // KHONG dung coroutine cua hieu ung.
    public void BeginPreResultEffect()
    {
        if (hideHUDOnResult)
            HideGameplayUI();

        Debug.Log(
            "[ResultPanelEffect] " +
            "Pre-result: Gameplay HUD hidden.",
            this
        );
    }

    // =====================================================
    // HIDE GAMEPLAY UI
    // =====================================================

    public void HideGameplayUI()
    {
        if (!hideHUDOnResult)
            return;

        HideUIObject(weaponHUD);
        HideUIObject(matchHUD);
        HideUIObject(topLeftUI);
        HideUIObject(jetpackHUD);

        if (extraUIToHide != null)
        {
            foreach (GameObject ui in extraUIToHide)
            {
                HideUIObject(ui);
            }
        }
    }

    // =====================================================
    // SAFE HUD HIDE
    // =====================================================

    private void HideUIObject(GameObject uiObject)
    {
        if (uiObject == null)
            return;

        // Khong an panel ket qua hoac object cha
        // chua panel ket qua.
        if (uiObject.transform == transform ||
            transform.IsChildOf(uiObject.transform))
        {
            Debug.LogWarning(
                "[ResultPanelEffect] Khong an '" +
                uiObject.name +
                "' vi no chua Result Panel.",
                this
            );

            return;
        }

        // Khong dung SetActive(false).
        // Chi an hinh anh UI.
        CanvasGroup group =
            uiObject.GetComponent<CanvasGroup>();

        if (group == null)
        {
            group =
                uiObject.AddComponent<CanvasGroup>();
        }

        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    // =====================================================
    // AUDIO
    // =====================================================

    private void PlayResultSound()
    {
        AudioClip clip =
            resultType == ResultType.Win
            ? winSound
            : loseSound;

        if (resultAudioSource == null ||
            clip == null)
        {
            return;
        }

        resultAudioSource.playOnAwake = false;
        resultAudioSource.loop = false;
        resultAudioSource.spatialBlend = 0f;

        resultAudioSource.Stop();

        resultAudioSource.PlayOneShot(
            clip,
            resultVolume
        );
    }

    // =====================================================
    // PLAY RESULT EFFECT
    // =====================================================

    public void PlayEffect()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (hideHUDOnResult)
            HideGameplayUI();

        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        effectCoroutine =
            StartCoroutine(PlayRoutine());
    }

    // =====================================================
    // RESULT ANIMATION
    // =====================================================

    private IEnumerator PlayRoutine()
    {
        // -----------------------------------
        // INITIAL STATE
        // -----------------------------------

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (panelRect != null)
            panelRect.localScale = startScale;

        // -----------------------------------
        // FADE IN
        // -----------------------------------

        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer /
                Mathf.Max(0.01f, fadeDuration)
            );

            if (canvasGroup != null)
                canvasGroup.alpha = t;

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        // -----------------------------------
        // SCALE UP
        // -----------------------------------

        timer = 0f;

        while (timer < scaleDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer /
                Mathf.Max(0.01f, scaleDuration)
            );

            float smoothT =
                Mathf.SmoothStep(0f, 1f, t);

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

        // -----------------------------------
        // SETTLE
        // -----------------------------------

        timer = 0f;

        while (timer < settleDuration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer /
                Mathf.Max(0.01f, settleDuration)
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

        // -----------------------------------
        // FINAL STATE
        // -----------------------------------

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (panelRect != null)
            panelRect.localScale = finalScale;

        effectCoroutine = null;
    }

    // =====================================================
    // ON DISABLE
    // =====================================================

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (panelRect != null)
            panelRect.localScale = finalScale;

        hasPlayedResultSound = false;

        // Khong bat lai HUD o day.
        // Khi tran ket thuc HUD can giu an.
    }
}
