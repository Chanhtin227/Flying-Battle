using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

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
    public string sceneToLoad;
    public float loadingTextSpeed = 0.35f;
    public float tipChangeInterval = 2f;
    
    [Tooltip("Thời gian chờ tối thiểu trước khi chuyển scene (giây)")]
    public float minimumLoadingTime = 3f; 

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
        AsyncOperation gameLevel = SceneManager.LoadSceneAsync(sceneToLoad);
        
        // Ngăn Unity tự động chuyển scene khi load xong 90%
        gameLevel.allowSceneActivation = false;

        float timer = 0f;

        // Vòng lặp chạy cho đến khi đạt đủ thời gian chờ VÀ scene đã load xong
        while (timer < minimumLoadingTime || gameLevel.progress < 0.9f)
        {
            timer += Time.unscaledDeltaTime;

            // Tính toán tiến trình dựa trên cả thời gian chờ 3s và dữ liệu load thực tế
            float timeProgress = Mathf.Clamp01(timer / minimumLoadingTime);
            float loadProgress = Mathf.Clamp01(gameLevel.progress / 0.9f);
            
            // Thanh xanh sẽ chạy theo tiến trình nào chậm hơn để tránh việc thanh chạy xong nhưng vẫn phải đứng chờ
            float displayProgress = Mathf.Min(timeProgress, loadProgress);

            if (progressFill != null)
            {
                progressFill.fillAmount = displayProgress;
            }

            // Nếu thời gian đã đủ 3s và scene ngầm đã tải xong
            if (timer >= minimumLoadingTime && gameLevel.progress >= 0.9f)
            {
                gameLevel.allowSceneActivation = true;
            }

            yield return null;
        }

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
            if (dotCount > 3)
            {
                dotCount = 0;
            }

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
        if (tipText == null || tips == null || tips.Length == 0)
            return;

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