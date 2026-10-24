using UnityEngine;
using Fusion;

public class JetpackVFX : MonoBehaviour
{
    [Header("Player References")]
    public PlayerMovement playerMovement;
    public PlayerEquipmentFusion equipmentFusion;

    [Header("Backpack 1")]
    public GameObject backpack1Model;
    public ParticleSystem leftJetFX;
    public ParticleSystem rightJetFX;
    [Min(1)] public int backpack1BaloID = 1;

    [Header("Backpack 2")]
    public GameObject backpack2Model;
    public ParticleSystem backpack2LeftJetFX;
    public ParticleSystem backpack2RightJetFX;
    [Min(1)] public int backpack2BaloID = 2;

    [Header("Existing Particle Settings")]
    public bool autoEnableParticleComponents = true;
    public bool activateInactiveParticleChildren = true;
    public bool playExistingParticles = false;

    [Header("Auto Stylized Rocket Flames")]
    public bool useAutoBlueFlames = true;

    [Header("Backpack 1 Engine Positions (fallback)")]
    public Transform backpack1LeftEngine;
    public Transform backpack1RightEngine;

    [Header("Backpack 2 Engine Positions (fallback)")]
    public Transform backpack2LeftEngine;
    public Transform backpack2RightEngine;

    [Header("Stylized Flame Settings")]
    [Min(0.1f)] public float flameLength = 1.6f;
    [Min(0.01f)] public float flameWidth = 0.16f;
    [Min(1f)] public float flameEmission = 120f;
    public Color coreFlameColor = new Color(1f, 0.95f, 0.65f, 1f);
    public Color outerFlameColor = new Color(1f, 0.55f, 0.15f, 1f);
    public Color smokeColor = new Color(0.9f, 0.45f, 0.75f, 0.85f);
    public Color emberColor = new Color(1f, 0.7f, 0.25f, 1f);
    public bool enableSparks = true;

    [Header("Flame Direction")]
    public bool followExistingParticleDirection = true;
    [Tooltip("Dao chieu lua 180 do khi theo Particle System goc. Bat neu lua dang phut len troi.")]
    public bool reverseExistingParticleDirection = true;
    public Vector3 flameRotation = new Vector3(90f, 0f, 0f);

    [Header("Debug")]
    public bool showVFXWarnings = true;
    [Min(0.5f)] public float warningInterval = 3f;

    [Header("Jetpack Sound")]
    public AudioClip jetpackFlySound;
    [Range(0f, 1f)] public float jetpackVolume = 0.65f;
    [Min(0.01f)] public float soundFadeSpeed = 3f;
    public AudioSource jetpackAudioSource;

    [Header("Local Player HUD")]
    public JetpackHUD localJetpackHUD;

    private bool soundStopping;
    private bool hudBound;
    private JetpackHUD boundHUD;
    private int currentBackpackID = int.MinValue;
    private int currentBackpackSlot = -1;
    private ParticleSystem activeLeftFX;
    private ParticleSystem activeRightFX;
    private GameObject activeBackpackModel;
    private float nextWarningTime;
    private readonly ParticleSystem[,] autoFlames = new ParticleSystem[4, 4];
    private Material flameMaterial;
    private Texture2D flameTexture;
    private bool autoFlamesCreated;

    private void Awake()
    {
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        if (equipmentFusion == null)
            equipmentFusion = GetComponentInParent<PlayerEquipmentFusion>();

        SetupAudio();
        activeLeftFX = leftJetFX;
        activeRightFX = rightJetFX;
        // Do not assume Backpack 1 is equipped before Fusion data arrives.
        activeBackpackModel = null;
        StopAllJets(true);
    }

    private void Start()
    {
        if (useAutoBlueFlames) CreateAutoFlames();
    }

    private void SetupAudio()
    {
        if (jetpackAudioSource == null)
        {
            Transform existing = transform.Find("JetpackAudio");
            if (existing != null)
                jetpackAudioSource = existing.GetComponent<AudioSource>();
            if (jetpackAudioSource == null)
            {
                GameObject audioObject = new GameObject("JetpackAudio");
                audioObject.transform.SetParent(transform, false);
                jetpackAudioSource = audioObject.AddComponent<AudioSource>();
            }
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
        if (playerMovement == null)
            playerMovement = GetComponentInParent<PlayerMovement>();
        if (equipmentFusion == null)
            equipmentFusion = GetComponentInParent<PlayerEquipmentFusion>();

        bool valid = playerMovement != null &&
                     playerMovement.Object != null &&
                     playerMovement.Object.IsValid;

        if (hudBound && boundHUD == null) hudBound = false;
        if (!hudBound && valid && playerMovement.HasInputAuthority)
        {
            if (localJetpackHUD == null)
                localJetpackHUD = FindFirstObjectByType<JetpackHUD>();
            if (localJetpackHUD != null)
            {
                localJetpackHUD.Bind(playerMovement);
                boundHUD = localJetpackHUD;
                hudBound = true;
            }
        }

        if (valid && equipmentFusion != null)
        {
            int baloID = equipmentFusion.BaloID;
            int baloSlot = equipmentFusion.GetEquippedBaloSlot();
            if (currentBackpackID != baloID || currentBackpackSlot != baloSlot)
                ApplyBackpackVFX(baloID, baloSlot);
        }

        // Effects only. PlayerEquipmentFusion owns backpack model visibility.
        bool boosting = valid && playerMovement.IsJetpackBoosting &&
            currentBackpackSlot > 0 && activeBackpackModel != null &&
            activeBackpackModel.activeInHierarchy;
        if (boosting) PlayJets();
        else StopAllJets(false);
        UpdateJetpackAudio(boosting);
    }

    private void ApplyBackpackVFX(int baloID, int baloSlot)
    {
        StopAllJets(true);
        currentBackpackID = baloID;
        currentBackpackSlot = baloSlot;
        if (baloSlot == 2)
        {
            // Use the model containing the selected engine/particles.
            activeBackpackModel = backpack2Model != null
                ? backpack2Model
                : (equipmentFusion != null ? equipmentFusion.GetEquippedBaloModel() : null);
            activeLeftFX = backpack2LeftJetFX;
            activeRightFX = backpack2RightJetFX;
        }
        else if (baloSlot == 1)
        {
            activeBackpackModel = backpack1Model != null
                ? backpack1Model
                : (equipmentFusion != null ? equipmentFusion.GetEquippedBaloModel() : null);
            activeLeftFX = leftJetFX;
            activeRightFX = rightJetFX;
        }
        else
        {
            activeBackpackModel = null;
            activeLeftFX = null;
            activeRightFX = null;
        }
        if (showVFXWarnings)
            Debug.Log("[JetpackVFX] BaloID=" + baloID + " | Slot=" + baloSlot, this);
    }

    private void CreateAutoFlames()
    {
        if (autoFlamesCreated) return;
        autoFlamesCreated = true;
        CreateFlameMaterial();
        CreateEngineFlames(0, backpack1LeftEngine, leftJetFX);
        CreateEngineFlames(1, backpack1RightEngine, rightJetFX);
        CreateEngineFlames(2, backpack2LeftEngine, backpack2LeftJetFX);
        CreateEngineFlames(3, backpack2RightEngine, backpack2RightJetFX);
    }

    private void CreateFlameMaterial()
    {
        const int size = 64;
        flameTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        flameTexture.wrapMode = TextureWrapMode.Clamp;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = (x - 31.5f) / 31.5f;
                float dy = (y - 31.5f) / 31.5f;
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Pow(Mathf.Clamp01(1f - distance), 1.5f);
                flameTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        flameTexture.Apply();
        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null)
        {
            Debug.LogWarning("[JetpackVFX] Missing Sprites/Default shader.", this);
            return;
        }
        flameMaterial = new Material(shader);
        flameMaterial.mainTexture = flameTexture;
    }

    private void CreateEngineFlames(int index, Transform engine, ParticleSystem referenceFX)
    {
        // Use the original particle transform itself: same origin and rotation.
        // Engine transforms are only fallback when no old particle is assigned.
        bool follow = followExistingParticleDirection && referenceFX != null;
        Transform origin = follow ? referenceFX.transform : engine;
        if (origin == null) return;
        // Reference Particle System emits along its local +Z.
        // Reverse 180 degrees when the reference points opposite to the desired exhaust.
        Quaternion offsetRotation = follow
            ? (reverseExistingParticleDirection
                ? Quaternion.Euler(180f, 0f, 0f)
                : Quaternion.identity)
            : Quaternion.Euler(flameRotation);

        autoFlames[index, 0] = CreateStylizedParticle(origin, offsetRotation,
            "Stylized_CoreFlame", coreFlameColor, flameWidth * 0.45f,
            flameEmission * 1.2f, 0.16f, flameLength * 2.8f, 4f, true, false);
        autoFlames[index, 1] = CreateStylizedParticle(origin, offsetRotation,
            "Stylized_OuterFlame", outerFlameColor, flameWidth,
            flameEmission, 0.28f, flameLength * 2.2f, 10f, true, false);
        autoFlames[index, 2] = CreateStylizedParticle(origin, offsetRotation,
            "Stylized_PinkSmoke", smokeColor, flameWidth * 1.35f,
            flameEmission * 0.55f, 0.45f, flameLength * 1.2f, 18f, false, true);
        if (enableSparks)
            autoFlames[index, 3] = CreateStylizedParticle(origin, offsetRotation,
                "Stylized_Embers", emberColor, flameWidth * 0.18f,
                18f, 0.35f, flameLength * 3f, 12f, false, false);
    }

    private ParticleSystem CreateStylizedParticle(
        Transform parent, Quaternion localRotation, string particleName,
        Color startColor, float startSize, float emissionRate,
        float lifetime, float speed, float coneAngle,
        bool stretchFlame, bool smoky)
    {
        GameObject obj = new GameObject(particleName);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;
        obj.transform.localRotation = localRotation;

        ParticleSystem ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.gravityModifier = 0f;
        main.simulationSpace = smoky
            ? ParticleSystemSimulationSpace.World
            : ParticleSystemSimulationSpace.Local;
        main.maxParticles = 600;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = emissionRate;
        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = coneAngle;
        shape.radius = startSize * 0.25f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        AnimationCurve sizeCurve;
        if (smoky)
            sizeCurve = new AnimationCurve(new Keyframe(0f, 0.35f),
                new Keyframe(0.35f, 0.8f), new Keyframe(1f, 1.5f));
        else if (stretchFlame)
            sizeCurve = new AnimationCurve(new Keyframe(0f, 0.2f),
                new Keyframe(0.15f, 1f), new Keyframe(0.7f, 0.55f),
                new Keyframe(1f, 0f));
        else
            sizeCurve = new AnimationCurve(new Keyframe(0f, 0.4f),
                new Keyframe(0.25f, 1f), new Keyframe(1f, 0f));
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, sizeCurve);

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        Gradient gradient = new Gradient();
        if (smoky)
        {
            gradient.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.7f, 0.85f), 0f),
                new GradientColorKey(new Color(0.85f, 0.45f, 0.7f), 0.4f),
                new GradientColorKey(new Color(0.55f, 0.25f, 0.45f), 1f)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(0.55f, 0f),
                new GradientAlphaKey(0.45f, 0.3f),
                new GradientAlphaKey(0f, 1f)
            });
        }
        else if (stretchFlame)
        {
            gradient.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 1f, 0.85f), 0f),
                new GradientColorKey(new Color(1f, 0.75f, 0.25f), 0.2f),
                new GradientColorKey(new Color(1f, 0.35f, 0.1f), 0.7f),
                new GradientColorKey(new Color(0.55f, 0.1f, 0.05f), 1f)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(0.95f, 0f),
                new GradientAlphaKey(0.85f, 0.3f),
                new GradientAlphaKey(0.3f, 0.75f),
                new GradientAlphaKey(0f, 1f)
            });
        }
        else
        {
            gradient.SetKeys(new GradientColorKey[] {
                new GradientColorKey(new Color(1f, 0.9f, 0.6f), 0f),
                new GradientColorKey(new Color(1f, 0.65f, 0.2f), 0.5f),
                new GradientColorKey(new Color(1f, 0.35f, 0.1f), 1f)
            }, new GradientAlphaKey[] {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.7f, 0.35f),
                new GradientAlphaKey(0f, 1f)
            });
        }
        colorOverLifetime.color = gradient;

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = smoky ? 0.45f : 0.12f;
        noise.frequency = smoky ? 0.6f : 0.9f;
        noise.scrollSpeed = smoky ? 0.3f : 0.15f;
        noise.damping = true;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = smoky;
        if (smoky)
        {
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
            velocity.y = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
            velocity.z = new ParticleSystem.MinMaxCurve(-0.1f, 0.1f);
        }
        ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = stretchFlame
            ? ParticleSystemRenderMode.Stretch
            : ParticleSystemRenderMode.Billboard;
        renderer.lengthScale = stretchFlame ? 2.2f : 1f;
        renderer.velocityScale = stretchFlame ? 0.5f : 0f;
        if (flameMaterial != null) renderer.sharedMaterial = flameMaterial;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        return ps;
    }

    private void PlayJets()
    {
        if (activeBackpackModel == null || !activeBackpackModel.activeInHierarchy)
            return;
        if (playExistingParticles)
        {
            PlayParticle(activeLeftFX);
            PlayParticle(activeRightFX);
        }
        if (!useAutoBlueFlames) return;
        if (!autoFlamesCreated) CreateAutoFlames();
        if (currentBackpackSlot == 1)
        {
            PlayAutoFlameGroup(0);
            PlayAutoFlameGroup(1);
        }
        else if (currentBackpackSlot == 2)
        {
            PlayAutoFlameGroup(2);
            PlayAutoFlameGroup(3);
        }
    }

    private void PlayAutoFlameGroup(int index)
    {
        for (int i = 0; i < 4; i++)
        {
            ParticleSystem ps = autoFlames[index, i];
            if (ps == null || !ps.gameObject.activeInHierarchy) continue;
            ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = true;
            if (!ps.isPlaying) ps.Play(true);
        }
    }

    private void PlayParticle(ParticleSystem fx)
    {
        if (fx == null || activeBackpackModel == null ||
            !activeBackpackModel.activeInHierarchy) return;
        if (!fx.transform.IsChildOf(activeBackpackModel.transform))
        {
            WarnVFX("ParticleSystem khong nam trong balo: " + fx.name);
            return;
        }
        if (!fx.gameObject.activeInHierarchy)
        {
            if (!activateInactiveParticleChildren) return;
            Transform current = fx.transform;
            while (current != null && current != activeBackpackModel.transform)
            {
                if (!current.gameObject.activeSelf) current.gameObject.SetActive(true);
                current = current.parent;
            }
        }
        if (!fx.gameObject.activeInHierarchy) return;
        if (autoEnableParticleComponents)
        {
            ParticleSystemRenderer renderer = fx.GetComponent<ParticleSystemRenderer>();
            if (renderer != null) renderer.enabled = true;
        }
        if (!fx.isPlaying) fx.Play(true);
    }

    private void StopAllJets(bool clearParticles)
    {
        StopParticle(leftJetFX, clearParticles);
        StopParticle(rightJetFX, clearParticles);
        StopParticle(backpack2LeftJetFX, clearParticles);
        StopParticle(backpack2RightJetFX, clearParticles);
        for (int engine = 0; engine < 4; engine++)
            for (int i = 0; i < 4; i++)
                StopParticle(autoFlames[engine, i], clearParticles);
    }

    private void StopParticle(ParticleSystem fx, bool clearParticles)
    {
        if (fx == null) return;
        if (!fx.isPlaying && !fx.isEmitting)
        {
            if (clearParticles) fx.Clear(true);
            return;
        }
        fx.Stop(true, clearParticles
            ? ParticleSystemStopBehavior.StopEmittingAndClear
            : ParticleSystemStopBehavior.StopEmitting);
    }

    private void WarnVFX(string message)
    {
        if (!showVFXWarnings || Time.unscaledTime < nextWarningTime) return;
        nextWarningTime = Time.unscaledTime + Mathf.Max(0.5f, warningInterval);
        Debug.LogWarning("[JetpackVFX] " + message, this);
    }

    private void UpdateJetpackAudio(bool boosting)
    {
        if (jetpackAudioSource == null) return;
        if (jetpackFlySound == null)
        {
            if (jetpackAudioSource.isPlaying) jetpackAudioSource.Stop();
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
        else soundStopping = true;
        jetpackAudioSource.volume = Mathf.MoveTowards(
            jetpackAudioSource.volume,
            boosting ? jetpackVolume : 0f,
            Mathf.Max(0.01f, soundFadeSpeed) * Time.deltaTime);
        if (soundStopping && jetpackAudioSource.isPlaying &&
            jetpackAudioSource.volume <= 0.001f)
            jetpackAudioSource.Stop();
    }

    private void OnDisable()
    {
        StopAllJets(true);
        if (jetpackAudioSource != null)
        {
            jetpackAudioSource.Stop();
            jetpackAudioSource.volume = 0f;
        }
        hudBound = false;
        boundHUD = null;
        currentBackpackID = int.MinValue;
        currentBackpackSlot = -1;
    }

    private void OnDestroy()
    {
        if (flameMaterial != null) Destroy(flameMaterial);
        if (flameTexture != null) Destroy(flameTexture);
    }
}
