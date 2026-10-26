using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using System.Collections;
using System.Threading.Tasks;

public class BackToMainLoading : MonoBehaviour
{
    [Header("Scene Transition")]
    [Tooltip("Tên của Scene Loading trong Build Settings")]
    public string loadingSceneName = "Loading";

    private bool isLoading = false;
    private bool audioPausedByThisScript = false;

    public void BackToMain()
    {
        if (isLoading)
            return;

        StartCoroutine(BackToMainRoutine());
    }

    private IEnumerator BackToMainRoutine()
    {
        isLoading = true;

        PauseAllAudio();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        NetworkRunner runner = FindAnyObjectByType<NetworkRunner>();
        bool shutdownFinished = false;

        if (runner != null && runner.IsRunning)
        {
            _ = ShutdownRunner(runner, () => { shutdownFinished = true; });
        }
        else
        {
            shutdownFinished = true;
        }

        // Đợi ngắt kết nối mạng hoàn tất trước khi chuyển scene
        while (!shutdownFinished)
        {
            yield return null;
        }

        // Chuyển trực tiếp sang Scene Loading
        SceneManager.LoadScene(loadingSceneName);
    }

    private async Task ShutdownRunner(NetworkRunner runner, System.Action finished)
    {
        if (runner != null)
        {
            try
            {
                await runner.Shutdown();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[BackToMainLoading] Shutdown lỗi: " + e.Message);
            }
        }
        finished?.Invoke();
    }

    private void PauseAllAudio()
    {
        if (audioPausedByThisScript)
            return;

        AudioListener.pause = true;
        audioPausedByThisScript = true;
    }

    private void ResumeAllAudio()
    {
        if (!audioPausedByThisScript)
            return;

        AudioListener.pause = false;
        audioPausedByThisScript = false;
    }

    private void OnDestroy()
    {
        ResumeAllAudio();
    }
}