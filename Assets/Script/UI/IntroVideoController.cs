using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;
using System.Collections;

public class IntroVideoController : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public CanvasGroup titleGroup;
    public RectTransform titleRect;
    public double showTitleAtSecond = 10.0;
    public string nextSceneName = "MainMenu";

    [Header("Button hiện sau khi Title xong")]
    public CanvasGroup buttonGroup;
    public float delayAfterTitle = 1f;

    private bool titleShown = false;

    void Start()
    {
        titleGroup.alpha = 0;
        titleRect.localScale = Vector3.zero;

        buttonGroup.alpha = 0;
        buttonGroup.interactable = false;
        buttonGroup.blocksRaycasts = false;
    }

    void Update()
    {
        // Không còn reset khi video loop nữa — chỉ chạy 1 lần duy nhất
        if (!titleShown && videoPlayer.time >= showTitleAtSecond)
        {
            titleShown = true;
            StartCoroutine(ShowTitleThenButton());
        }
    }

    IEnumerator ShowTitleThenButton()
    {
        yield return StartCoroutine(AnimateTitleIn());
        yield return new WaitForSeconds(delayAfterTitle);
        yield return StartCoroutine(AnimateButtonIn());
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
            titleRect.localScale = Vector3.one * scale;
            titleGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        titleRect.localScale = Vector3.one;
        titleGroup.alpha = 1;
    }

    IEnumerator AnimateButtonIn()
    {
        float duration = 0.6f;
        float elapsed = 0f;

        buttonGroup.interactable = true;
        buttonGroup.blocksRaycasts = true;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            buttonGroup.alpha = Mathf.SmoothStep(0f, 1f, t);
            yield return null;
        }

        buttonGroup.alpha = 1;
    }

    public void GoToNextScene()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}