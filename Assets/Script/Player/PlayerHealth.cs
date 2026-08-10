using UnityEngine;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public float maxHealth = 100f;
    public float currentHealth;

    [Header("UI")]
    public Slider healthSlider;
    public GameObject gameOverUI;

    private bool isDead = false;

    void Start()
    {
        // Đảm bảo game chạy bình thường khi bắt đầu
        Time.timeScale = 1f;

        currentHealth = maxHealth;

        if (healthSlider != null)
        {
            healthSlider.maxValue = maxHealth;
            healthSlider.value = currentHealth;
        }

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }
    }

    public void TakeDamage(float damage)
    {
        if (isDead) return;

        currentHealth -= damage;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (healthSlider != null)
        {
            healthSlider.value = currentHealth;
        }

        // Animation bị trúng đạn
        if (currentHealth > 0)
        {
            PlayerAnimation playerAnim = GetComponent<PlayerAnimation>();

            if (playerAnim != null)
            {
                playerAnim.Hit();
            }
        }

        Debug.Log("Player HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;

        isDead = true;

        Debug.Log("PLAYER DEAD!");

        // Chạy animation chết
        PlayerAnimation playerAnim = GetComponent<PlayerAnimation>();

        if (playerAnim != null)
        {
            playerAnim.Die();
        }

        // Tắt điều khiển Player
        PlayerMovement controller = GetComponent<PlayerMovement>();

        if (controller != null)
        {
            controller.enabled = false;
        }

        // Hiện Game Over UI
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }

        // Mở chuột
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // DỪNG TOÀN BỘ GAME
        Time.timeScale = 0f;
    }
}