using UnityEngine;
using UnityEngine.UI;
using Fusion;

/// <summary>
/// Local full-screen fade driven by the networked SpawnPlayer transition.
/// Attach to an ALWAYS ACTIVE scene object, such as BattleFadeManager.
/// The Canvas/Image are created automatically if not assigned.
/// </summary>
public class BattleTransitionFadeUI : MonoBehaviour
{
    [Header("Spawn System")]
    public SpawnPlayer spawnPlayer;

    [Header("Optional UI - Auto Created If Empty")]
    public Image blackImage;

    [Header("Fade Settings")]
    [Min(0.1f)] public float fadeBackSeconds = 1f;
    public int overlaySortOrder = 32000;

    [Header("Debug (Editor Only)")]
    public bool enableTestKey = true;
    public KeyCode testKey = KeyCode.F8;

    private bool battleSeen;
    private bool fadeBackActive;
    private float fadeBackElapsed;
    private float manualTestUntil;
    private bool warnedNoSpawner;

    private void Awake()
    {
        EnsureOverlay();
        SetAlpha(0f);
    }

    private void EnsureOverlay()
    {
        // An Image under a disabled parent cannot render even if its own
        // GameObject is enabled. This is common when BattleFade is inactive.
        if (blackImage != null && blackImage.gameObject.activeInHierarchy)
        {
            Canvas existingCanvas = blackImage.GetComponentInParent<Canvas>();
            if (existingCanvas != null)
            {
                existingCanvas.overrideSorting = true;
                existingCanvas.sortingOrder = overlaySortOrder;
            }
            blackImage.raycastTarget = false;
            return;
        }

        if (blackImage != null)
            Debug.LogWarning("[BattleFade] Assigned Black Image is inactive in hierarchy; creating an independent full-screen overlay.");

        GameObject canvasObject = new GameObject(
            "BattleFadeCanvas_Runtime",
            typeof(RectTransform),
            typeof(Canvas),
            typeof(CanvasScaler));
        // Keep the Canvas under this ALWAYS ACTIVE manager, not BattleFade.
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.overrideSorting = true;
        canvas.sortingOrder = overlaySortOrder;

        GameObject imageObject = new GameObject(
            "BlackFadeImage_Runtime", typeof(RectTransform), typeof(Image));
        imageObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = imageObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        blackImage = imageObject.GetComponent<Image>();
        blackImage.raycastTarget = false;
        blackImage.color = Color.clear;
    }

    private void Update()
    {
        if (blackImage == null) EnsureOverlay();
        if (blackImage == null) return;

#if UNITY_EDITOR
        if (enableTestKey && Input.GetKeyDown(testKey))
            manualTestUntil = Time.unscaledTime + 1f;

        if (Time.unscaledTime < manualTestUntil)
        {
            SetAlpha(1f);
            return;
        }
#endif

        if (spawnPlayer == null)
            spawnPlayer = FindFirstObjectByType<SpawnPlayer>();

        if (spawnPlayer == null || spawnPlayer.Object == null ||
            !spawnPlayer.Object.IsValid || spawnPlayer.Runner == null)
        {
            if (!warnedNoSpawner)
            {
                Debug.LogWarning("[BattleFade] No active SpawnPlayer NetworkObject found. Check scene spawnPlayer / Fusion scene setup.");
                warnedNoSpawner = true;
            }
            SetAlpha(0f);
            return;
        }
        warnedNoSpawner = false;

        // Begin fade-in while server is still keeping players on the waiting map.
        if (spawnPlayer.TransitionStarted && !spawnPlayer.BattleStarted)
        {
            battleSeen = false;
            fadeBackActive = false;

            float? remaining = spawnPlayer.TransitionTimer.RemainingTime(spawnPlayer.Runner);
            float fadeIn = Mathf.Max(0.1f, spawnPlayer.fadeToBlackSeconds);
            float total = fadeIn + Mathf.Max(0f, spawnPlayer.blackScreenHoldSeconds);
            float elapsed = total - Mathf.Max(0f, remaining ?? 0f);
            SetAlpha(Mathf.Clamp01(elapsed / fadeIn));
            return;
        }

        // The player has been teleported by state authority; reveal the battle map.
        if (spawnPlayer.BattleStarted)
        {
            if (!battleSeen)
            {
                battleSeen = true;
                fadeBackActive = spawnPlayer.TransitionStarted;
                fadeBackElapsed = 0f;
                Debug.Log("[BattleFade] Battle started. Fading from black to clear.");
            }

            if (fadeBackActive)
            {
                fadeBackElapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(fadeBackElapsed / Mathf.Max(0.1f, fadeBackSeconds));
                SetAlpha(1f - t);
                if (t >= 1f) fadeBackActive = false;
            }
            else SetAlpha(0f);
            return;
        }

        battleSeen = false;
        fadeBackActive = false;
        SetAlpha(0f);
    }

    private void SetAlpha(float alpha)
    {
        if (blackImage == null) return;
        blackImage.color = new Color(0f, 0f, 0f, Mathf.Clamp01(alpha));
        blackImage.raycastTarget = false;
    }
}
