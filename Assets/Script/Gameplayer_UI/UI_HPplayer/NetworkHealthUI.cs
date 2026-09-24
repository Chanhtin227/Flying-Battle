using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;

public class NetworkHealthUI : MonoBehaviour
{
    // =========================================================
    // UI
    // =========================================================

    [Header("Health UI")]

    public Slider healthSlider;
    public TMP_Text hpText;


    // =========================================================
    // GAME OVER
    // =========================================================

    [Header("Game Over")]

    public GameObject gameOverUI;


    // =========================================================
    // LOCAL PLAYER
    // =========================================================

    private PlayerHealth localPlayerHealth;


    // =========================================================
    // START
    // =========================================================

    private void Start()
    {
        if (gameOverUI != null)
        {
            gameOverUI.SetActive(false);
        }


        InvokeRepeating(
            nameof(FindLocalPlayer),
            0.2f,
            0.5f
        );
    }


    // =========================================================
    // FIND LOCAL PLAYER
    // =========================================================

    private void FindLocalPlayer()
    {
        if (localPlayerHealth != null)
            return;


        PlayerHealth[] players =
            FindObjectsByType<PlayerHealth>(
                FindObjectsInactive.Exclude
            );


        foreach (PlayerHealth player in players)
        {
            if (player.HasInputAuthority)
            {
                localPlayerHealth = player;


                Debug.Log(
                    "NETWORK HEALTH UI → LOCAL PLAYER: " +
                    player.name
                );


                if (gameOverUI != null)
                {
                    player.gameOverUI =
                        gameOverUI;
                }


                CancelInvoke(
                    nameof(FindLocalPlayer)
                );


                return;
            }
        }
    }


    // =========================================================
    // UPDATE
    // =========================================================

    private void Update()
    {
        if (localPlayerHealth == null)
            return;


        UpdateHealthUI();
    }


    // =========================================================
    // UPDATE HEALTH UI
    // =========================================================

    private void UpdateHealthUI()
    {
        if (healthSlider != null)
        {
            healthSlider.maxValue =
                localPlayerHealth.maxHealth;


            healthSlider.value =
                localPlayerHealth.CurrentHealth;
        }


        if (hpText != null)
        {
            hpText.text =
                "HP: " +
                Mathf.CeilToInt(
                    localPlayerHealth.CurrentHealth
                );
        }
    }
}