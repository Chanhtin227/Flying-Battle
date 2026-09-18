using UnityEngine;
using Fusion;

public class PlayerHealth : NetworkBehaviour
{
    // =========================================================
    // HEALTH SETTINGS
    // =========================================================

    [Header("Health")]

    public float maxHealth = 100f;


    // =========================================================
    // NETWORK HEALTH
    // =========================================================

    [Header("Network Health")]

    [Networked]
    public float CurrentHealth { get; set; }

    [Networked]
    public NetworkBool IsDead { get; set; }


    // =========================================================
    // GAME OVER UI
    // =========================================================

    [Header("Game Over UI")]

    public GameObject gameOverUI;


    // =========================================================
    // SPAWN
    // =========================================================

    public override void Spawned()
    {
        // =====================================================
        // STATE AUTHORITY KHỞI TẠO MÁU
        // =====================================================

        if (HasStateAuthority)
        {
            CurrentHealth = maxHealth;
            IsDead = false;
        }


        // =====================================================
        // GAME OVER UI
        // =====================================================

        // Chỉ UI của Player local được sử dụng
        if (HasInputAuthority)
        {
            if (gameOverUI != null)
            {
                gameOverUI.SetActive(false);
            }
        }
    }


    // =========================================================
    // TAKE DAMAGE
    // =========================================================

    public void TakeDamage(float damage)
    {
        // =====================================================
        // CHỈ HOST / STATE AUTHORITY ĐƯỢC TRỪ MÁU
        // =====================================================

        if (!HasStateAuthority)
            return;


        // =====================================================
        // ĐÃ CHẾT
        // =====================================================

        if (IsDead)
            return;


        // =====================================================
        // DAMAGE
        // =====================================================

        CurrentHealth -= damage;

        CurrentHealth =
            Mathf.Clamp(
                CurrentHealth,
                0f,
                maxHealth
            );


        Debug.Log(
            "PLAYER " +
            Object.InputAuthority +
            " HP: " +
            CurrentHealth +
            " / " +
            maxHealth
        );


        // =====================================================
        // HIT
        // =====================================================

        if (CurrentHealth > 0)
        {
            Rpc_PlayHit();
        }


        // =====================================================
        // DEATH
        // =====================================================

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }


    // =========================================================
    // HIT RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayHit()
    {
        PlayerAnimation playerAnim =
            GetComponent<PlayerAnimation>();


        if (playerAnim != null)
        {
            playerAnim.Hit();
        }
    }


    // =========================================================
    // DIE
    // =========================================================

    private void Die()
    {
        // Chỉ Host
        if (!HasStateAuthority)
            return;


        if (IsDead)
            return;


        IsDead = true;


        Debug.Log(
            "PLAYER DEAD: " +
            Object.InputAuthority
        );


        // =====================================================
        // GỬI ANIMATION CHẾT
        // =====================================================

        Rpc_PlayDeath();


        // =====================================================
        // CHỈ TẮT CONTROLLER CỦA PLAYER ĐÃ CHẾT
        // =====================================================

        PlayerMovement controller =
            GetComponent<PlayerMovement>();


        if (controller != null)
        {
            controller.enabled = false;
        }


        // =====================================================
        // GAME OVER CHỈ HIỆN Ở MÁY CỦA PLAYER ĐÓ
        // =====================================================

        if (HasInputAuthority)
        {
            ShowGameOver();
        }
        else
        {
            Rpc_ShowGameOver();
        }
    }


    // =========================================================
    // DEATH ANIMATION RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.All
    )]
    private void Rpc_PlayDeath()
    {
        PlayerAnimation playerAnim =
            GetComponent<PlayerAnimation>();


        if (playerAnim != null)
        {
            playerAnim.Die();
        }
    }


    // =========================================================
    // GAME OVER RPC
    // =========================================================

    [Rpc(
        RpcSources.StateAuthority,
        RpcTargets.InputAuthority
    )]
    private void Rpc_ShowGameOver()
    {
        ShowGameOver();
    }


    // =========================================================
    // SHOW GAME OVER
    // =========================================================

    private void ShowGameOver()
    {
        // =====================================================
        // MỞ GAME OVER
        // =====================================================

        if (gameOverUI != null)
        {
            gameOverUI.SetActive(true);
        }


        // =====================================================
        // MỞ CHUỘT
        // =====================================================

        Cursor.lockState =
            CursorLockMode.None;

        Cursor.visible = true;


        // =====================================================
        // QUAN TRỌNG:
        // KHÔNG DÙNG Time.timeScale = 0
        // =====================================================

        // Không được dừng toàn bộ game multiplayer
    }
}