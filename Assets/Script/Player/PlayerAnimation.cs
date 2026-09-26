using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator anim;


    // =========================================================
    // SOUND
    // =========================================================

    [Header("Sound")]

    public AudioSource audioSource;

    public AudioClip batHitSound;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        anim = GetComponent<Animator>();


        // Nếu Animator nằm ở object con
        if (anim == null)
        {
            anim = GetComponentInChildren<Animator>();
        }
    }


    // =========================================================
    // MOVE
    // =========================================================

    public void Move(float speed)
    {
        if (anim == null)
            return;

        anim.SetFloat("Speed", speed);
    }


    // =========================================================
    // JUMP
    // =========================================================

    public void Jump()
    {
        if (anim == null)
            return;

        anim.SetTrigger("Jump");
    }


    // =========================================================
    // SHOOT
    // =========================================================

    public void Shoot()
    {
        if (anim == null)
            return;

        anim.SetTrigger("Shoot");
    }


    // =========================================================
    // MELEE
    // =========================================================

    public void MeleeAttack()
    {
        if (anim == null)
            return;

        anim.SetTrigger("MeleeAttack");
    }


    // =========================================================
    // HIT
    // =========================================================

    public void Hit()
    {
        if (anim == null)
            return;

        anim.SetTrigger("Hit");
    }


    // =========================================================
    // DIE
    // =========================================================

    public void Die()
    {
        if (anim == null)
            return;

        anim.SetTrigger("Die");
    }


    // =========================================================
    // RESPAWN
    // =========================================================

    public void Respawn()
    {
        if (anim == null)
            return;


        // =====================================================
        // RESET TRIGGER
        // =====================================================

        anim.ResetTrigger("Die");
        anim.ResetTrigger("Hit");
        anim.ResetTrigger("Shoot");
        anim.ResetTrigger("MeleeAttack");
        anim.ResetTrigger("Jump");


        // =====================================================
        // RESET ANIMATOR
        // =====================================================

        anim.Rebind();

        anim.Update(0f);


        // =====================================================
        // RESET PARAMETERS
        // =====================================================

        anim.SetFloat("Speed", 0f);

        anim.SetBool("IsMelee", false);


        // =====================================================
        // ĐẢM BẢO ANIMATION CHẠY LẠI
        // =====================================================

        anim.speed = 1f;
    }


    // =========================================================
    // SET MELEE
    // =========================================================

    public void SetMelee(bool value)
    {
        if (anim == null)
            return;

        anim.SetBool("IsMelee", value);
    }


    // =========================================================
    // BAT SOUND
    // =========================================================

    public void PlayBatSound()
    {
        if (
            audioSource != null &&
            batHitSound != null
        )
        {
            audioSource.PlayOneShot(
                batHitSound
            );
        }
    }
}