using UnityEngine;
using Fusion;

/// <summary>
/// Visual/audio effect for each networked player. HUD is bound only for the local player.
/// </summary>
public class JetpackVFX : MonoBehaviour
{
    [Header("Jetpack Particles")]
    public ParticleSystem leftJetFX;
    public ParticleSystem rightJetFX;

    [Header("Player Movement")]
    public PlayerMovement playerMovement;

    [Header("Jetpack Sound")]
    [Tooltip("Looping jetpack/rocket engine AudioClip")]
    public AudioClip jetpackFlySound;
    [Range(0f, 1f)] public float jetpackVolume = 0.65f;
    [Min(0.01f)] public float soundFadeSpeed = 3f;
    [Tooltip("Leave empty to create a 3D AudioSource automatically")]
    public AudioSource jetpackAudioSource;

    [Header("Local Player HUD (Optional)")]
    [Tooltip("HUD in the scene Canvas. You can leave this blank; the local player will find it automatically.")]
    public JetpackHUD localJetpackHUD;

    private bool soundStopping;
    private bool hudBound;
    private JetpackHUD boundHUD;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();

        SetupAudio();
        StopJets(true);
    }

    private void SetupAudio()
    {
        if (jetpackAudioSource == null)
        {
            GameObject audioObject = new GameObject("JetpackAudio");
            audioObject.transform.SetParent(transform, false);
            jetpackAudioSource = audioObject.AddComponent<AudioSource>();
        }

        jetpackAudioSource.playOnAwake = false;
        jetpackAudioSource.loop = true;
        jetpackAudioSource.spatialBlend = 1f;
        jetpackAudioSource.minDistance = 2f;
        jetpackAudioSource.maxDistance = 25f;
        jetpackAudioSource.rolloffMode = AudioRolloffMode.Logarithmic;
        jetpackAudioSource.volume = 0f;
        jetpackAudioSource.clip = jetpackFlySound;
    }

    private void Update()
    {
        bool valid = playerMovement != null &&
                     playerMovement.Object != null &&
                     playerMovement.Object.IsValid;

        // Find and bind HUD only on the player controlled by THIS machine.
        // The scene HUD is not part of the network player prefab.
        if (hudBound && boundHUD == null)
            hudBound = false;

        if (!hudBound && valid && playerMovement.HasInputAuthority)
        {
            if (localJetpackHUD == null)
                localJetpackHUD = FindFirstObjectByType<JetpackHUD>();

            if (localJetpackHUD != null)
            {
                localJetpackHUD.Bind(playerMovement);
                hudBound = true;
                boundHUD = localJetpackHUD;
            }
        }

        bool boosting = valid && playerMovement.IsJetpackBoosting;
        if (boosting)
            PlayJets();
        else
            StopJets(false);

        UpdateJetpackAudio(boosting);
    }

    private void PlayJets()
    {
        if (leftJetFX != null && !leftJetFX.isPlaying)
            leftJetFX.Play();
        if (rightJetFX != null && !rightJetFX.isPlaying)
            rightJetFX.Play();
    }

    private void StopJets(bool clearParticles)
    {
        ParticleSystemStopBehavior behavior = clearParticles
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting;

        if (leftJetFX != null && leftJetFX.isEmitting)
            leftJetFX.Stop(true, behavior);
        if (rightJetFX != null && rightJetFX.isEmitting)
            rightJetFX.Stop(true, behavior);
    }

    private void UpdateJetpackAudio(bool boosting)
    {
        if (jetpackAudioSource == null)
            return;

        if (jetpackFlySound == null)
        {
            if (jetpackAudioSource.isPlaying)
                jetpackAudioSource.Stop();
            return;
        }

        if (jetpackAudioSource.clip != jetpackFlySound)
            jetpackAudioSource.clip = jetpackFlySound;

        if (boosting)
        {
            soundStopping = false;
            if (!jetpackAudioSource.isPlaying)
            {
                jetpackAudioSource.volume = 0f;
                jetpackAudioSource.Play();
            }
        }
        else
        {
            soundStopping = true;
        }

        jetpackAudioSource.volume = Mathf.MoveTowards(
            jetpackAudioSource.volume,
            boosting ? jetpackVolume : 0f,
            Mathf.Max(0.01f, soundFadeSpeed) * Time.deltaTime
        );

        if (soundStopping && jetpackAudioSource.isPlaying &&
            jetpackAudioSource.volume <= 0.001f)
            jetpackAudioSource.Stop();
    }

    private void OnDisable()
    {
        StopJets(true);
        if (jetpackAudioSource != null)
        {
            jetpackAudioSource.Stop();
            jetpackAudioSource.volume = 0f;
        }
        hudBound = false;
        boundHUD = null;
    }
}
