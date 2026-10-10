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
    public string nextSceneName = "MainMenu";

    [Tooltip("Kéo object đang gắn BackToMainLoading vào đây để hiện Loading Panel khi bấm")]
    public BackToMainLoading loadingController;

    private bool titleShown = false;
    private bool isGoingNext = false;
    private Coroutine blinkCoroutine;
    private Vector3 titleOriginalScale;   // scale gốc của title (đã chỉnh trong Editor)

    void Start()
    {
        // Lưu lại scale bạn đã chỉnh trong Inspector
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

            // Hiệu ứng phóng ra có nảy nhẹ. Muốn bỏ nảy: scale = Mathf.SmoothStep(0f, 1f, t);
            float scale = Mathf.SmoothStep(0f, 1f, t) + Mathf.Sin(t * Mathf.PI) * 0.15f * (1 - t);
            titleRect.localScale = titleOriginalScale * scale;

            titleGroup.alpha = Mathf.SmoothStep(0f, 1f, t);

            yield return null;
        }

        titleRect.localScale = titleOriginalScale;   // trả về đúng scale gốc
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
        if (isGoingNext) return;   // chặn bấm nhiều lần
        isGoingNext = true;

        // Dừng hiệu ứng nhấp nháy và khóa vùng chạm
        if (blinkCoroutine != null) StopCoroutine(blinkCoroutine);
        tapAreaGroup.interactable = false;
        tapAreaGroup.blocksRaycasts = false;

        if (loadingController != null)
        {
            // Truyền scene đích rồi bật Loading Panel
            loadingController.mainMenuSceneName = nextSceneName;
            loadingController.BackToMain();
        }
        else
        {
            // Dự phòng nếu quên kéo loadingController
            Debug.LogWarning("[IntroVideoController] Chưa gán loadingController, load scene trực tiếp.");
            SceneManager.LoadScene(nextSceneName);
        }
    }
}