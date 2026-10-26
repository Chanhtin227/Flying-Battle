using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroVideoController : MonoBehaviour
{
    [Header("Video")]
    public VideoPlayer videoPlayer;

    [Header("Logo / Tên game")]
    public CanvasGroup titleGroup;
    public RectTransform titleRect;
    public double showTitleAtSecond = 10.0;

    [Header("Chữ 'Chạm để vào game'")]
    public CanvasGroup tapTextGroup;
    public float delayAfterTitle = 1f;
    public float blinkSpeed = 0.8f;
    public float minAlpha = 0.3f;
    public float maxAlpha = 1f;

    [Header("Vùng chạm toàn màn hình")]
    public CanvasGroup tapAreaGroup;

    [Header("Chuyển scene")]
    [Tooltip("Nhập tên Scene Loading vào đây")]
    public string nextSceneName = "Loading"; 

    private bool titleShown = false;
    private bool isGoingNext = false;
    private Coroutine blinkCoroutine;
    private Vector3 titleOriginalScale;

    void Start()
    {
        titleOriginalScale = titleRect.localScale;

        titleGroup.alpha = 0;
        titleRect.localScale = Vector3.zero;
        tapTextGroup.alpha = 0;

        tapAreaGroup.alpha = 0;
        tapAreaGroup.interactable = false;
        tapAreaGroup.blocksRaycasts = false;
    }

    void Update()
    {
        if (!titleShown && videoPlayer.time >= showTitleAtSecond)
        {
            titleShown = true;
            StartCoroutine(ShowTitleThenTapPrompt());
        }
    }

    IEnumerator ShowTitleThenTapPrompt()
    {
        yield return StartCoroutine(AnimateTitleIn());
        yield return new WaitForSeconds(delayAfterTitle);

        tapAreaGroup.alpha = 0.5f;
        tapAreaGroup.interactable = true;
        tapAreaGroup.blocksRaycasts = true;

        yield return StartCoroutine(FadeCanvasGroup(tapTextGroup, 0f, 1f, 0.6f));
        blinkCoroutine = StartCoroutine(BlinkTapText());
    }

    IEnumerator AnimateTitleIn()
    {
        float duration = 1.2f;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;

            float scale = Mathf.SmoothStep(0f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.15f * (1 - t);
            titleRect.localScale = titleOriginalScale * scale;
            titleGroup.alpha = Mathf.SmoothStep(0f, 1f, t);

            yield return null;
        }

        titleRect.localScale = titleOriginalScale; 
        titleGroup.alpha = 1;
    }

    IEnumerator BlinkTapText()
    {
        while (true)
        {
            yield return StartCoroutine(FadeCanvasGroup(tapTextGroup, maxAlpha, minAlpha, blinkSpeed));
            yield return StartCoroutine(FadeCanvasGroup(tapTextGroup, minAlpha, maxAlpha, blinkSpeed));
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup group, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            group.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        group.alpha = to;
    }

    // Gán hàm này vào OnClick của vùng chạm / nút bấm
    public void GoToNextScene()
    {
        if (isGoingNext) return;
        isGoingNext = true;

        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        
        tapAreaGroup.interactable = false;
        tapAreaGroup.blocksRaycasts = false;

        // Chuyển thẳng sang scene được cấu hình
        SceneManager.LoadScene(nextSceneName);
    }
}