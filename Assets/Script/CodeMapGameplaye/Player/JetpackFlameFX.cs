
using UnityEngine;

public class JetpackFlameFX : MonoBehaviour
{
    [Header("Jetpack Engine Positions")]
    public Transform leftEngine;
    public Transform rightEngine;

    [Header("Flame Settings")]
    public Color flameColor = new Color(0.05f, 0.65f, 1f);
    public Color flameCoreColor = Color.white;

    [Range(0.2f, 5f)]
    public float flameLength = 1.5f;

    [Range(10f, 200f)]
    public float flameParticles = 90f;

    [Range(0.01f, 0.5f)]
    public float flameWidth = 0.12f;

    [Header("Spark Settings")]
    public bool enableSparks = true;
    public float sparksPerSecond = 18f;

    private ParticleSystem[] effects;
    private Material particleMaterial;
    private Texture2D particleTexture;
    private bool initialized;
    private bool isFlying;

    private void Awake()
    {
        Initialize();
    }

    private void Initialize()
    {
        if (initialized) return;

        if (leftEngine == null || rightEngine == null)
        {
            Debug.LogWarning(
                "JetpackFlameFX: Assign both engine transforms.",
                this
            );
            return;
        }

        CreateParticleMaterial();

        effects = new ParticleSystem[6];

        // Left engine
        effects[0] = CreateEffect(
            leftEngine, "BlueFlame_Left",
            flameColor, flameWidth, flameParticles,
            0.3f, flameLength * 2.8f, 8f
        );

        effects[1] = CreateEffect(
            leftEngine, "WhiteCore_Left",
            flameCoreColor, flameWidth * 0.48f,
            flameParticles * 0.65f,
            0.22f, flameLength * 2.2f, 5f
        );

        effects[2] = CreateEffect(
            leftEngine, "Sparks_Left",
            new Color(0.3f, 0.9f, 1f),
            flameWidth * 0.3f,
            enableSparks ? sparksPerSecond : 0f,
            0.45f, flameLength * 2.5f, 22f
        );

        // Right engine
        effects[3] = CreateEffect(
            rightEngine, "BlueFlame_Right",
            flameColor, flameWidth, flameParticles,
            0.3f, flameLength * 2.8f, 8f
        );

        effects[4] = CreateEffect(
            rightEngine, "WhiteCore_Right",
            flameCoreColor, flameWidth * 0.48f,
            flameParticles * 0.65f,
            0.22f, flameLength * 2.2f, 5f
        );

        effects[5] = CreateEffect(
            rightEngine, "Sparks_Right",
            new Color(0.3f, 0.9f, 1f),
            flameWidth * 0.3f,
            enableSparks ? sparksPerSecond : 0f,
            0.45f, flameLength * 2.5f, 22f
        );

        initialized = true;
        SetFlying(false);
    }

    private void CreateParticleMaterial()
    {
        // Create a soft circular particle texture.
        const int size = 64;
        particleTexture = new Texture2D(
            size, size, TextureFormat.RGBA32, false
        );

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - size / 2f) /
                           (size / 2f);
                float ny = (y + 0.5f - size / 2f) /
                           (size / 2f);

                float distance = Mathf.Sqrt(nx * nx + ny * ny);
                float alpha = Mathf.Pow(
                    Mathf.Clamp01(1f - distance), 1.5f
                );

                particleTexture.SetPixel(
                    x, y, new Color(1f, 1f, 1f, alpha)
                );
            }
        }

        particleTexture.Apply();

        Shader shader = Shader.Find("Sprites/Default");

        if (shader == null)
        {
            Debug.LogError(
                "JetpackFlameFX: Cannot find Sprites/Default shader."
            );
            return;
        }

        particleMaterial = new Material(shader);
        particleMaterial.mainTexture = particleTexture;
    }

    private ParticleSystem CreateEffect(
        Transform parent,
        string effectName,
        Color color,
        float width,
        float rate,
        float lifetime,
        float speed,
        float angle
    )
    {
        GameObject obj = new GameObject(effectName);
        obj.transform.SetParent(parent, false);
        obj.transform.localPosition = Vector3.zero;

        // Particle cone emits along local +Z.
        // Rotate it toward the character's local -Y.
        obj.transform.localRotation =
            Quaternion.Euler(90f, 0f, 0f);

        ParticleSystem ps =
            obj.AddComponent<ParticleSystem>();

        var main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 1f;
        main.startLifetime = lifetime;
        main.startSpeed = speed;
        main.startSize = width;
        main.startColor = color;
        main.gravityModifier = 0f;
        main.simulationSpace =
            ParticleSystemSimulationSpace.World;
        main.maxParticles = 500;

        var emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = rate;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType =
            ParticleSystemShapeType.Cone;
        shape.angle = angle;
        shape.radius = width * 0.35f;

        var sizeOverLifetime = ps.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new
            ParticleSystem.MinMaxCurve(
                1f,
                new AnimationCurve(
                    new Keyframe(0f, 0.4f),
                    new Keyframe(0.25f, 1f),
                    new Keyframe(1f, 0f)
                )
            );

        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.white, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0.75f, 0.4f),
                new GradientAlphaKey(0f, 1f)
            }
        );

        colorOverLifetime.color = gradient;

        var renderer =
            obj.GetComponent<ParticleSystemRenderer>();

        renderer.renderMode =
            ParticleSystemRenderMode.Billboard;

        if (particleMaterial != null)
            renderer.sharedMaterial = particleMaterial;

        ps.Stop(true,
            ParticleSystemStopBehavior.StopEmittingAndClear);

        return ps;
    }

    // Call this from the existing Jetpack flight script.
    public void SetFlying(bool flying)
    {
        if (!initialized)
            Initialize();

        if (!initialized) return;
        if (isFlying == flying) return;

        isFlying = flying;

        foreach (ParticleSystem ps in effects)
        {
            if (ps == null) continue;

            if (flying)
            {
                if (!ps.isPlaying)
                    ps.Play();
            }
            else
            {
                ps.Stop(
                    true,
                    ParticleSystemStopBehavior.StopEmitting
                );
            }
        }
    }

    [ContextMenu("Test - Start Jetpack Flames")]
    private void TestStart()
    {
        SetFlying(true);
    }

    [ContextMenu("Test - Stop Jetpack Flames")]
    private void TestStop()
    {
        SetFlying(false);
    }

    private void OnDestroy()
    {
        if (particleMaterial != null)
            Destroy(particleMaterial);

        if (particleTexture != null)
            Destroy(particleTexture);
    }
}
