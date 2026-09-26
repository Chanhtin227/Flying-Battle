using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace GameCore
{
    /// <summary>
    /// Gắn vào 1 GameObject trong CHÍNH SCENE LOADING (scene trung gian, có Image
    /// thanh tiến trình). Tự động chạy ngay khi Scene Loading bắt đầu (Start),
    /// đọc tên Scene đích đã được IntroToLoadingButton ghi lại, rồi load nó
    /// bất đồng bộ kèm cập nhật % lên Image Fill.
    /// </summary>
    public class LoadingSceneController : MonoBehaviour
    {
        [Tooltip("Thanh Image kiểu Filled hiển thị % tiến trình (không bắt buộc)")]
        [SerializeField] private UnityEngine.UI.Image fillBarImage;

        [Tooltip("Text hiển thị số % (VD: '75%') (không bắt buộc)")]
        [SerializeField] private TMPro.TMP_Text progressText;

        [Tooltip("Thời gian tối thiểu (giây) hiển thị màn Loading, tránh load quá nhanh gây giật")]
        [SerializeField] private float minimumLoadingTime = 1f;

        [Tooltip("Scene dự phòng nếu quên không ghi tên đích (đỡ bị đứng màn hình Loading mãi)")]
        [SerializeField] private string fallbackSceneName = "SceneMain";

        private void Start()
        {
            string sceneToLoad = string.IsNullOrEmpty(SceneFlowData.NextSceneName)
                ? fallbackSceneName
                : SceneFlowData.NextSceneName;

            StartCoroutine(LoadTargetSceneRoutine(sceneToLoad));
        }

        private IEnumerator LoadTargetSceneRoutine(string sceneName)
        {
            UpdateProgressUI(0f);

            float elapsedTime = 0f;

            AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName);
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                elapsedTime += Time.deltaTime;

                float displayProgress = Mathf.Clamp01(operation.progress / 0.9f);
                UpdateProgressUI(displayProgress);

                yield return null;
            }

            while (elapsedTime < minimumLoadingTime)
            {
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            UpdateProgressUI(1f);

            operation.allowSceneActivation = true;
        }

        private void UpdateProgressUI(float progress01)
        {
            if (fillBarImage != null)
                fillBarImage.fillAmount = progress01;

            if (progressText != null)
                progressText.text = $"{Mathf.RoundToInt(progress01 * 100f)}%";
        }
    }
}
