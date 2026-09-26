using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

namespace GameCore
{
    /// <summary>
    /// Gắn vào nút bấm trong SCENE VIDEO/INTRO (scene đầu tiên khi mở game).
    /// Khi bấm, sẽ:
    /// 1. Ghi nhớ tên scene ĐÍCH CUỐI CÙNG cần tới (VD: "SceneMain").
    /// 2. Chuyển ngay sang SCENE LOADING (scene trung gian, hiện thanh tiến trình).
    /// Scene Loading sẽ tự đọc lại tên đích này và load tiếp (xem LoadingSceneController.cs).
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class IntroToLoadingButton : MonoBehaviour
    {
        [Tooltip("Tên Scene Loading trung gian, VD: 'LoadingScene'")]
        [SerializeField] private string loadingSceneName = "LoadingScene";

        [Tooltip("Tên Scene ĐÍCH CUỐI CÙNG cần tới sau khi Loading xong, VD: 'SceneMain'")]
        [SerializeField] private string targetSceneName = "SceneMain";

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(OnButtonClicked);
        }

        private void OnButtonClicked()
        {
            // Ghi nhớ đích đến để Scene Loading biết cần load tiếp scene nào
            SceneFlowData.NextSceneName = targetSceneName;

            // Chuyển sang Scene Loading NGAY (dùng LoadScene thường vì Scene Loading
            // thường rất nhẹ, không cần thanh tiến trình cho bước chuyển này)
            SceneManager.LoadScene(loadingSceneName);
        }
    }
}
