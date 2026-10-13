using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Collections;

public class BackToMainLoading : MonoBehaviour
{
    // =========================================================
    // SCENE
    // =========================================================

    [Header("Scene")]
    public string mainMenuSceneName = "SceneMain";


    // =========================================================
    // LOADING UI
    // =========================================================

    [Header("Loading UI")]

    public GameObject loadingPanel;

    [Tooltip("Kéo Image thanh xanh vào đây")]
    public Image progressFill;

    public TMP_Text percentText;

    public TMP_Text loadingText;

    public TMP_Text tipText;


    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Loading Settings")]

    [Tooltip("Thời gian tối thiểu để loading hiện ra")]
    public float minimumLoadingTime = 1.5f;

    [Tooltip("Tốc độ thanh loading chạy")]
    public float progressSpeed = 0.6f;

    [Tooltip("Tốc độ chạy dấu chấm của LOADING GAME")]
    public float loadingTextSpeed = 0.35f;


    // =========================================================
    // RANDOM TIPS
    // =========================================================

    [Header("Random Loading Tips")]

    [Tooltip("Khoảng thời gian đổi sang Tip mới")]
    public float tipChangeInterval = 2f;


    [TextArea]
    public string[] loadingTips =
    {
        "Tip: Press F to pick up items.",

        "Tip: Reload before entering a fight.",

        "Tip: Keep moving to avoid enemy fire.",

        "Tip: Use medkits when your health is low.",

        "Tip: Watch your ammo carefully.",

        "Tip: Use cover to survive longer.",

        "Tip: Melee weapons are useful at close range.",

        "Tip: Press Shift to run faster.",

        "Tip: Search chests for useful equipment.",

        "Tip: Stay alert and listen for nearby enemies."
    };


    // =========================================================
    // PRIVATE
    // =========================================================

    private bool isLoading = false;

    private Coroutine loadingTextCoroutine;

    private Coroutine tipCoroutine;

    private int lastTipIndex = -1;

    private bool audioPausedByThisScript = false;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        // =====================================================
        // ẨN LOADING LÚC BẮT ĐẦU
        // =====================================================

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(false);
        }


        // =====================================================
        // RESET THANH XANH
        // =====================================================

        if (progressFill != null)
        {
            progressFill.fillAmount = 0f;
        }


        // =====================================================
        // RESET %
        // =====================================================

        if (percentText != null)
        {
            percentText.text = "0%";
        }


        // =====================================================
        // RESET LOADING TEXT
        // =====================================================

        if (loadingText != null)
        {
            loadingText.text = "LOADING GAME";
        }


        // =====================================================
        // RESET TIP
        // =====================================================

        if (tipText != null)
        {
            tipText.text = "";
        }
    }


    // =========================================================
    // BUTTON
    // =========================================================

    public void BackToMain()
    {
        if (isLoading)
            return;


        StartCoroutine(
            BackToMainRoutine()
        );
    }


    // =========================================================
    // LOADING ROUTINE
    // =========================================================

    private IEnumerator BackToMainRoutine()
    {
        isLoading = true;


        // =====================================================
        // TẮT TOÀN BỘ AUDIO KHI BẮT ĐẦU LOADING
        // =====================================================

        PauseAllAudio();


        // =====================================================
        // SHOW LOADING PANEL
        // =====================================================

        if (loadingPanel != null)
        {
            loadingPanel.SetActive(true);
        }


        // =====================================================
        // START LOADING TEXT EFFECT
        // =====================================================

        if (loadingTextCoroutine != null)
        {
            StopCoroutine(
                loadingTextCoroutine
            );
        }


        loadingTextCoroutine =
            StartCoroutine(
                LoadingTextEffect()
            );


        // =====================================================
        // START RANDOM TIP EFFECT
        // =====================================================

        if (tipCoroutine != null)
        {
            StopCoroutine(
                tipCoroutine
            );
        }


        tipCoroutine =
            StartCoroutine(
                RandomTipEffect()
            );


        // =====================================================
        // CURSOR
        // =====================================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible =
            true;


        // =====================================================
        // RESET LOADING
        // =====================================================

        UpdateLoadingUI(0f);


        // =====================================================
        // SHUTDOWN FUSION
        // =====================================================

        NetworkRunner runner =
            FindAnyObjectByType<NetworkRunner>();


        float fakeProgress =
            0f;


        float timer =
            0f;


        bool shutdownFinished =
            false;


        if (runner != null &&
            runner.IsRunning)
        {
            _ = ShutdownRunner(
                runner,
                () =>
                {
                    shutdownFinished =
                        true;
                }
            );
        }
        else
        {
            shutdownFinished =
                true;
        }


        // =====================================================
        // FAKE LOADING 0 -> 90%
        // =====================================================

        while (
            timer < minimumLoadingTime ||
            !shutdownFinished
        )
        {
            timer +=
                Time.unscaledDeltaTime;


            fakeProgress =
                Mathf.MoveTowards(
                    fakeProgress,
                    0.9f,
                    Time.unscaledDeltaTime *
                    progressSpeed
                );


            UpdateLoadingUI(
                fakeProgress
            );


            yield return null;
        }


        // =====================================================
        // LOAD SCENE ASYNC
        // =====================================================

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(
                mainMenuSceneName
            );


        if (operation == null)
        {
            Debug.LogError(
                "[BackToMainLoading] Không load được scene: " +
                mainMenuSceneName
            );


            StopLoadingEffects();


            isLoading =
                false;


            // Load scene thất bại thì bật lại audio.
            ResumeAllAudio();


            yield break;
        }


        operation.allowSceneActivation =
            false;


        while (!operation.isDone)
        {
            float sceneProgress =
                Mathf.Clamp01(
                    operation.progress /
                    0.9f
                );


            float finalProgress =
                Mathf.Lerp(
                    0.9f,
                    1f,
                    sceneProgress
                );


            UpdateLoadingUI(
                finalProgress
            );


            // =================================================
            // SCENE LOAD XONG
            // =================================================

            if (operation.progress >= 0.9f)
            {
                UpdateLoadingUI(
                    1f
                );


                // =============================================
                // DỪNG EFFECT
                // =============================================

                isLoading =
                    false;


                StopLoadingEffects();


                // =============================================
                // TEXT CUỐI
                // =============================================

                if (loadingText != null)
                {
                    loadingText.text =
                        "Loading... ";
                }


                // =============================================
                // GIỮ 100% MỘT CHÚT
                // =============================================

                yield return
                    new WaitForSecondsRealtime(
                        0.8f
                    );


                // =============================================
                // BẬT LẠI AUDIO CHO SCENE MAIN
                // =============================================

                ResumeAllAudio();


                operation.allowSceneActivation =
                    true;
            }


            yield return null;
        }
    }


    // =========================================================
    // LOADING TEXT EFFECT
    // =========================================================

    private IEnumerator LoadingTextEffect()
    {
        int dotCount =
            0;


        while (isLoading)
        {
            if (loadingText != null)
            {
                string dots =
                    new string(
                        '.',
                        dotCount
                    );


                loadingText.text =
                    "LOADING GAME" +
                    dots;
            }


            dotCount++;


            if (dotCount > 3)
            {
                dotCount = 0;
            }


            yield return
                new WaitForSecondsRealtime(
                    loadingTextSpeed
                );
        }
    }


    // =========================================================
    // RANDOM TIP EFFECT
    // =========================================================

    private IEnumerator RandomTipEffect()
    {
        while (isLoading)
        {
            ShowRandomTip();


            yield return
                new WaitForSecondsRealtime(
                    Mathf.Max(
                        0.1f,
                        tipChangeInterval
                    )
                );
        }
    }


    // =========================================================
    // SHOW RANDOM TIP
    // =========================================================

    private void ShowRandomTip()
    {
        if (tipText == null)
            return;


        if (loadingTips == null ||
            loadingTips.Length == 0)
        {
            tipText.text =
                "";

            return;
        }


        // =====================================================
        // CHỈ CÓ 1 TIP
        // =====================================================

        if (loadingTips.Length == 1)
        {
            lastTipIndex =
                0;


            tipText.text =
                loadingTips[0];


            return;
        }


        // =====================================================
        // RANDOM TIP KHÁC TIP TRƯỚC
        // =====================================================

        int randomIndex =
            Random.Range(
                0,
                loadingTips.Length
            );


        int safety =
            0;


        while (
            randomIndex == lastTipIndex &&
            safety < 20
        )
        {
            randomIndex =
                Random.Range(
                    0,
                    loadingTips.Length
                );


            safety++;
        }


        lastTipIndex =
            randomIndex;


        tipText.text =
            loadingTips[randomIndex];
    }


    // =========================================================
    // STOP EFFECTS
    // =========================================================

    private void StopLoadingEffects()
    {
        // =====================================================
        // LOADING TEXT
        // =====================================================

        if (loadingTextCoroutine != null)
        {
            StopCoroutine(
                loadingTextCoroutine
            );


            loadingTextCoroutine =
                null;
        }


        // =====================================================
        // RANDOM TIP
        // =====================================================

        if (tipCoroutine != null)
        {
            StopCoroutine(
                tipCoroutine
            );


            tipCoroutine =
                null;
        }
    }


    // =========================================================
    // AUDIO
    // =========================================================

    private void PauseAllAudio()
    {
        if (audioPausedByThisScript)
            return;


        AudioListener.pause = true;

        audioPausedByThisScript =
            true;
    }


    private void ResumeAllAudio()
    {
        if (!audioPausedByThisScript)
            return;


        AudioListener.pause = false;

        audioPausedByThisScript =
            false;
    }


    private void OnDestroy()
    {
        // Tránh trường hợp object bị hủy bất ngờ
        // nhưng AudioListener vẫn còn pause.
        ResumeAllAudio();
    }


    // =========================================================
    // SHUTDOWN RUNNER
    // =========================================================

    private async System.Threading.Tasks.Task ShutdownRunner(
        NetworkRunner runner,
        System.Action finished)
    {
        if (runner == null)
        {
            finished?.Invoke();

            return;
        }


        try
        {
            await runner.Shutdown();
        }
        catch (System.Exception e)
        {
            Debug.LogWarning(
                "[BackToMainLoading] Shutdown lỗi: " +
                e.Message
            );
        }


        finished?.Invoke();
    }


    // =========================================================
    // UPDATE UI
    // =========================================================

    private void UpdateLoadingUI(
        float value)
    {
        value =
            Mathf.Clamp01(
                value
            );


        // =====================================================
        // THANH XANH
        // =====================================================

        if (progressFill != null)
        {
            progressFill.fillAmount =
                value;
        }


        // =====================================================
        // %
        // =====================================================

        if (percentText != null)
        {
            int percent =
                Mathf.RoundToInt(
                    value *
                    100f
                );


            percentText.text =
                percent +
                "%";
        }
    }
}