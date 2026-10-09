
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HitMarkerUI : MonoBehaviour
{
    // =====================================================
    // UI
    // =====================================================

    [Header("UI")]
    public CanvasGroup canvasGroup;
    public RectTransform markerRect;
    public Image markerImage;

    // =====================================================
    // HIT MARKER SETTINGS
    // =====================================================

    [Header("Hit Marker Settings")]
    public float showDuration = 0.15f;
    public float startScale = 1.35f;

    public Color normalColor = Color.white;
    public Color killColor = Color.red;

    // =====================================================
    // HIT MARKER SOUND
    // =====================================================

    [Header("Hit Marker Sound")]
    public AudioSource hitAudioSource;

    [Tooltip("Âm thanh khi bắn trúng Player")]
    public AudioClip hitSound;

    [Tooltip("Âm thanh khi hạ gục Player")]
    public AudioClip killSound;

    [Range(0f, 1f)]
    public float hitVolume = 0.7f;

    [Range(0f, 1f)]
    public float killVolume = 1f;

    // =====================================================
    // PRIVATE
    // =====================================================

    private Coroutine effectRoutine;

    // =====================================================
    // AWAKE
    // =====================================================

    private void Awake()
    {
        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (markerRect == null)
            markerRect = GetComponent<RectTransform>();

        if (markerImage == null)
            markerImage = GetComponentInChildren<Image>(true);

        if (hitAudioSource == null)
            hitAudioSource = GetComponent<AudioSource>();

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
    }

    // =====================================================
    // SHOW HIT
    // =====================================================

    public void ShowHit(bool isKill)
    {
        if (effectRoutine != null)
            StopCoroutine(effectRoutine);

        if (markerImage != null)
        {
            markerImage.color =
                isKill ? killColor : normalColor;
        }

        // Phát âm thanh khi trúng hoặc hạ gục
        PlayHitSound(isKill);

        effectRoutine = StartCoroutine(PlayEffect());
    }

    // =====================================================
    // PLAY HIT SOUND
    // =====================================================

    private void PlayHitSound(bool isKill)
    {
        if (hitAudioSource == null)
            return;

        AudioClip selectedClip =
            isKill && killSound != null
                ? killSound
                : hitSound;

        if (selectedClip == null)
            return;

        float volume =
            isKill ? killVolume : hitVolume;

        hitAudioSource.PlayOneShot(
            selectedClip,
            volume
        );
    }

    // =====================================================
    // PLAY EFFECT
    // =====================================================

    private IEnumerator PlayEffect()
    {
        float duration =
            Mathf.Max(0.01f, showDuration);

        float timer = 0f;

        while (timer < duration)
        {
            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / duration
            );

            if (canvasGroup != null)
                canvasGroup.alpha = 1f - t;

            if (markerRect != null)
            {
                markerRect.localScale = Vector3.Lerp(
                    Vector3.one * startScale,
                    Vector3.one,
                    t
                );
            }

            yield return null;
        }

        if (canvasGroup != null)
            canvasGroup.alpha = 0f;

        if (markerRect != null)
            markerRect.localScale = Vector3.one;

        effectRoutine = null;
    }
}
