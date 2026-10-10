using UnityEngine;
using System.Collections;

public class ResultPanelEffect : MonoBehaviour
{
    [Header("References")]
    public CanvasGroup canvasGroup;
    public RectTransform panelRect;

    [Header("Timing")]
    public float fadeDuration = 0.25f;
    public float scaleDuration = 0.35f;
    public float settleDuration = 0.12f;

    [Header("Scale")]
    public Vector3 startScale = new Vector3(0.7f, 0.7f, 1f);
    public Vector3 overshootScale = new Vector3(1.05f, 1.05f, 1f);
    public Vector3 finalScale = Vector3.one;

    private Coroutine effectCoroutine;

    public enum ResultType { Win, Lose }

    [Header("Result Type")]
    public ResultType resultType = ResultType.Win;

    [Header("UI cần ẩn khi hiện Win / Lose")]
    public GameObject weaponHUD;
    public GameObject matchHUD;
    public GameObject topLeftUI;
    public GameObject jetpackHUD;

    [Header("Result Audio")]
    public AudioSource resultAudioSource;
    public AudioClip winSound;
    public AudioClip loseSound;
    [Range(0f, 1f)] public float resultVolume = 1f;

    private bool hasPlayedResultSound = false;

    private void OnEnable()
    {
        HideGameplayUI();
        if (!hasPlayedResultSound)
        {
            PlayResultSound();
            hasPlayedResultSound = true;
        }
        PlayEffect();
    }

    private void HideGameplayUI()
    {
        HideUIObject(weaponHUD);
        HideUIObject(matchHUD);
        HideUIObject(topLeftUI);
        HideUIObject(jetpackHUD);
    }

    private void HideUIObject(GameObject uiObject)
    {
        if (uiObject == null || !uiObject.activeSelf) return;

        // Không tắt object chứa chính ResultPanelEffect (hoặc cha của nó).
        // Nếu không panel kết quả và coroutine hiệu ứng cũng bị tắt.
        if (transform == uiObject.transform || transform.IsChildOf(uiObject.transform))
        {
            Debug.LogWarning("[ResultPanelEffect] Không thể ẩn '" + uiObject.name +
                "' vì chứa panel kết quả. Hãy gán riêng GameObject chứa gameplay HUD.");
            return;
        }

        uiObject.SetActive(false);
    }

    private void PlayResultSound()
    {
        AudioClip clip = resultType == ResultType.Win ? winSound : loseSound;
        if (resultAudioSource == null || clip == null) return;

        resultAudioSource.playOnAwake = false;
        resultAudioSource.loop = false;
        resultAudioSource.spatialBlend = 0f;
        resultAudioSource.Stop();
        resultAudioSource.PlayOneShot(clip, resultVolume);
    }

    public void PlayEffect()
    {
        if (!gameObject.activeInHierarchy)
            return;

        if (effectCoroutine != null)
            StopCoroutine(effectCoroutine);

        effectCoroutine = StartCoroutine(PlayRoutine());
    }

    private IEnumerator PlayRoutine()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (panelRect != null)
            panelRect.localScale = startScale;

        float timer = 0f;
        while (timer < fadeDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, fadeDuration));
            if (canvasGroup != null)
                canvasGroup.alpha = t;
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        timer = 0f;
        while (timer < scaleDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, scaleDuration));
            float smoothT = Mathf.SmoothStep(0f, 1f, t);
            if (panelRect != null)
                panelRect.localScale = Vector3.Lerp(startScale, overshootScale, smoothT);
            yield return null;
        }

        timer = 0f;
        while (timer < settleDuration)
        {
            timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(timer / Mathf.Max(0.01f, settleDuration));
            if (panelRect != null)
                panelRect.localScale = Vector3.Lerp(overshootScale, finalScale, t);
            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 1f;

        if (panelRect != null)
            panelRect.localScale = finalScale;

        effectCoroutine = null;
    }

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
    }
}
