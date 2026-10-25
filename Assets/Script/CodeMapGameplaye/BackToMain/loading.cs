using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // Khai báo thêm thư viện này
using TMPro;
using Fusion;

public class LoadingManager : MonoBehaviour
{
    [Header("Background Setup")]
    public Image backgroundImage;
    public Sprite[] backgrounds; 

    [Header("UI Elements")]
    public TextMeshProUGUI loadingTitle;
    public Image progressFill;
    public TextMeshProUGUI tipText;

    [Header("Settings")]
    public float loadingTextSpeed = 0.35f;
    public float tipChangeInterval = 2f;
    
    [Tooltip("Thời gian chờ mô phỏng thanh loading (giây)")]
    public float minimumLoadingTime = 3f; 

    [Tooltip("Tên scene sẽ load nếu đang ở chế độ Offline (VD: từ Intro vào MainMenu)")]
    public string offlineSceneToLoad = "SceneMain";

    [Header("Random Loading Tips")]
    [TextArea]
    public string[] tips =
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

    private bool isLoading = true;
    private int lastTipIndex = -1;

    void Start()
    {
        if (backgrounds != null && backgrounds.Length > 0)
        {
            int randomBg = Random.Range(0, backgrounds.Length);
            backgroundImage.sprite = backgrounds[randomBg];
        }

        StartCoroutine(LoadingTextEffect());
        StartCoroutine(RandomTipEffect());
        StartCoroutine(LoadAsyncOperation());
    }

    private IEnumerator LoadAsyncOperation()
    {
        float timer = 0f;

        while (timer < minimumLoadingTime)
        {
            timer += Time.unscaledDeltaTime;
            float progress = Mathf.Clamp01(timer / minimumLoadingTime);

            if (progressFill != null)
            {
                progressFill.fillAmount = progress;
            }

            yield return null;
        }

        // Hoàn thành thời gian chờ thì chuyển sang SceneMain
        SceneManager.LoadSceneAsync(offlineSceneToLoad);
        isLoading = false;
    }

    private IEnumerator LoadingTextEffect()
    {
        int dotCount = 0;
        while (isLoading)
        {
            if (loadingTitle != null)
            {
                string dots = new string('.', dotCount);
                loadingTitle.text = "loading" + dots;
            }

            dotCount++;
            if (dotCount > 3) dotCount = 0;

            yield return new WaitForSecondsRealtime(loadingTextSpeed);
        }
    }

    private IEnumerator RandomTipEffect()
    {
        while (isLoading)
        {
            ShowRandomTip();
            yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, tipChangeInterval));
        }
    }

    private void ShowRandomTip()
    {
        if (tipText == null || tips == null || tips.Length == 0) return;

        if (tips.Length == 1)
        {
            lastTipIndex = 0;
            tipText.text = tips[0];
            return;
        }

        int randomIndex = Random.Range(0, tips.Length);
        int safety = 0;

        while (randomIndex == lastTipIndex && safety < 20)
        {
            randomIndex = Random.Range(0, tips.Length);
            safety++;
        }

        lastTipIndex = randomIndex;
        tipText.text = tips[randomIndex];
    }
}