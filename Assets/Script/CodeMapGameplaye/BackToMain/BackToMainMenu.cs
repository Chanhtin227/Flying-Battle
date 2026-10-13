using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;

public class BackToMainMenu : MonoBehaviour
{
    // =========================================================
    // MAIN MENU SCENE
    // =========================================================

    [Header("Main Menu Scene")]

    [Tooltip("Tên scene Main Menu")]
    public string mainMenuSceneName = "SceneMain";


    // =========================================================
    // BUTTON
    // =========================================================

    public async void BackToMain()
    {
        Debug.Log(
            "[BackToMainMenu] Đang quay về Main Menu..."
        );


        // =====================================================
        // TÌM NETWORK RUNNER ĐANG TỒN TẠI
        // =====================================================

        NetworkRunner runner =
            FindAnyObjectByType<NetworkRunner>();


        // =====================================================
        // SHUTDOWN PHOTON FUSION
        // =====================================================

        if (runner != null)
        {
            if (runner.IsRunning)
            {
                Debug.Log(
                    "[BackToMainMenu] Shutdown NetworkRunner..."
                );

                await runner.Shutdown();

                Debug.Log(
                    "[BackToMainMenu] NetworkRunner đã shutdown."
                );
            }
        }
        else
        {
            Debug.LogWarning(
                "[BackToMainMenu] Không tìm thấy NetworkRunner."
            );
        }


        // =====================================================
        // LOAD MAIN MENU
        // =====================================================

        Debug.Log(
            "[BackToMainMenu] Load Scene: " +
            mainMenuSceneName
        );


        SceneManager.LoadScene(
            mainMenuSceneName
        );
    }
}