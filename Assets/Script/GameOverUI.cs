using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void ResetGame()
    {
        // Cho game chạy lại
        Time.timeScale = 1f;

        // Load lại scene hiện tại
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.name);
    }

    public void MainMenu()
    {
        // Cho game chạy lại
        Time.timeScale = 1f;

        SceneManager.LoadScene("MainMenu");
    }
}