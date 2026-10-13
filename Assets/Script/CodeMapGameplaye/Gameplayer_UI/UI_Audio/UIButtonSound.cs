using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class UIButtonSound : MonoBehaviour
{
    // =========================================================
    // AUDIO
    // =========================================================

    [Header("Button Sound")]

    [Tooltip("AudioSource dùng để phát âm thanh")]
    public AudioSource audioSource;

    [Tooltip("Âm thanh khi bấm Button")]
    public AudioClip clickSound;

    [Range(0f, 1f)]
    public float volume = 1f;


    // =========================================================
    // BUTTON
    // =========================================================

    private Button button;


    // =========================================================
    // AWAKE
    // =========================================================

    private void Awake()
    {
        button = GetComponent<Button>();

        if (button != null)
        {
            button.onClick.AddListener(
                PlayClickSound
            );
        }
    }


    // =========================================================
    // PLAY SOUND
    // =========================================================

    public void PlayClickSound()
    {
        if (audioSource == null)
        {
            Debug.LogWarning(
                "[UIButtonSound] Chưa gán AudioSource!"
            );

            return;
        }


        if (clickSound == null)
        {
            Debug.LogWarning(
                "[UIButtonSound] Chưa gán Click Sound!"
            );

            return;
        }


        audioSource.PlayOneShot(
            clickSound,
            volume
        );
    }


    // =========================================================
    // DESTROY
    // =========================================================

    private void OnDestroy()
    {
        if (button != null)
        {
            button.onClick.RemoveListener(
                PlayClickSound
            );
        }
    }
}