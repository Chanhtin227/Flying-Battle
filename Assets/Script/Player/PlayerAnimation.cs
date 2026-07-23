using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    Animator anim;

    [Header("Sound")]
    public AudioSource audioSource;
    public AudioClip batHitSound;

    void Awake()
    {
        anim = GetComponent<Animator>();
    }

    public void Move(float speed)
    {
        anim.SetFloat("Speed", speed);
    }

    public void Jump()
    {
        anim.SetTrigger("Jump");
    }

    public void Shoot()
    {
        anim.SetTrigger("Shoot");
    }

    public void MeleeAttack()
    {
        anim.SetTrigger("MeleeAttack");
    }

    public void Hit()
    {
        anim.SetTrigger("Hit");
    }

    public void Die()
    {
        anim.SetTrigger("Die");
    }

    public void SetMelee(bool value)
    {
        anim.SetBool("IsMelee", value);
    }

    // Hàm gọi từ Animation Event
    public void PlayBatSound()
    {
        if (audioSource != null && batHitSound != null)
        {
            audioSource.PlayOneShot(batHitSound);
        }
    }
}