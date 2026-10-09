using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls ONLY the background music AudioSource on this GameObject.
/// Weapon SFX, footsteps and Jetpack audio must use separate AudioSources.
/// </summary>
[RequireComponent(typeof(AudioSource))]
[DisallowMultipleComponent]
public class SceneBackgroundMusic : MonoBehaviour
{
    [Header("Scene duoc phep phat nhac")]
    public string targetSceneName = "Map_True1";

    [Header("Background Music Volume")]
    [Range(0f, 1f)] public float normalVolume = 0.5f;
    [Range(0f, 1f)] public float attackVolume = 0.15f;
    [Min(0.01f)] public float fadeDownSpeed = 4f;
    [Min(0.01f)] public float fadeUpSpeed = 1.2f;
    [Min(0f)] public float attackHoldTime = 0.35f;

    [Header("Diagnostics")]
    public bool debugLog = false;

    private static SceneBackgroundMusic instance;
    private AudioSource musicSource;
    private float attackUntil;

    private void Awake()
    {
        // This script should be on AudioBG, NOT on the Player or WeaponAudio.
        musicSource = GetComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = true;
        musicSource.spatialBlend = 0f;
        musicSource.volume = Mathf.Clamp01(normalVolume);
        musicSource.mute = false;
        instance = this;
    }

    private void OnEnable()
    {
        SceneManager.activeSceneChanged += OnSceneChanged;
        RefreshForScene();
    }

    private void OnDisable()
    {
        SceneManager.activeSceneChanged -= OnSceneChanged;
        if (musicSource != null)
            musicSource.Stop();
        if (instance == this)
            instance = null;
    }

    private void OnDestroy()
    {
        if (instance == this)
            instance = null;
    }

    private void OnSceneChanged(Scene oldScene, Scene newScene)
    {
        RefreshForScene();
    }

    private void RefreshForScene()
    {
        if (musicSource == null)
            return;

        bool allowed = SceneManager.GetActiveScene().name == targetSceneName;
        if (!allowed)
        {
            musicSource.Stop();
            return;
        }

        if (musicSource.clip == null)
        {
            if (debugLog)
                Debug.LogWarning("[SceneBackgroundMusic] AudioBG has no music clip.", this);
            return;
        }

        musicSource.volume = Mathf.Clamp01(normalVolume);
        if (!musicSource.isPlaying)
            musicSource.Play();
    }

    private void Update()
    {
        if (musicSource == null || !musicSource.isPlaying)
            return;

        // Adjust ONLY musicSource. Never change AudioListener.volume,
        // AudioSource sources on players, AudioMixer master volume, or weapon clips.
        bool attacking = Time.unscaledTime < attackUntil;
        float target = attacking
            ? Mathf.Min(Mathf.Clamp01(attackVolume), Mathf.Clamp01(normalVolume))
            : Mathf.Clamp01(normalVolume);
        float speed = attacking ? fadeDownSpeed : fadeUpSpeed;

        musicSource.volume = Mathf.MoveTowards(
            musicSource.volume,
            target,
            Mathf.Max(0.01f, speed) * Time.unscaledDeltaTime);
    }

    // Called by the locally-controlled PlayerWeapon when a shot/melee attack occurs.
    public static void NotifyWeaponAttack()
    {
        if (instance == null || !instance.isActiveAndEnabled || instance.musicSource == null)
            return;

        instance.attackUntil = Mathf.Max(
            instance.attackUntil,
            Time.unscaledTime + Mathf.Max(0f, instance.attackHoldTime));

        if (instance.debugLog)
            Debug.Log("[SceneBackgroundMusic] Local attack: ducking background music only.", instance);
    }
}
