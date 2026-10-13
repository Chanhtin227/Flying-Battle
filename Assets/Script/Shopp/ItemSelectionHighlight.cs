
using UnityEngine;
using UnityEngine.UI;

public class ItemSelectionHighlight : MonoBehaviour
{
    // ==========================================
    // GOLDEN BORDER
    // ==========================================

    [Header("Golden Border")]
    public GameObject goldenBorder;

    [Header("Glow Settings")]
    public bool animateGlow = true;

    [Min(0.1f)]
    public float glowSpeed = 3f;

    [Range(0f, 1f)]
    public float minAlpha = 0.65f;

    [Range(0f, 1f)]
    public float maxAlpha = 1f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private CanvasGroup borderGroup;

    // ==========================================
    // UNITY
    // ==========================================

    private void Awake()
    {
        SetupBorder();

        SetSelected(false);
    }

    private void SetupBorder()
    {
        // Tu tim GoldenBorder neu chua gan
        if (goldenBorder == null)
        {
            Transform child = transform.Find("GoldenBorder");

            if (child != null)
                goldenBorder = child.gameObject;
        }

        if (goldenBorder == null)
        {
            Debug.LogWarning(
                "[GoldenBorder] Missing GoldenBorder on " +
                gameObject.name,
                this
            );

            return;
        }

        borderGroup = goldenBorder.GetComponent<CanvasGroup>();

        if (borderGroup == null)
        {
            borderGroup =
                goldenBorder.AddComponent<CanvasGroup>();
        }

        borderGroup.blocksRaycasts = false;
        borderGroup.interactable = false;
        borderGroup.alpha = 1f;

        // Khung khong duoc chan click vao ItemButton
        Image borderImage =
            goldenBorder.GetComponent<Image>();

        if (borderImage != null)
            borderImage.raycastTarget = false;
    }

    // ==========================================
    // SET SELECTED
    // ==========================================

    public void SetSelected(bool selected)
    {
        if (goldenBorder == null)
            SetupBorder();

        if (goldenBorder == null)
            return;

        goldenBorder.SetActive(selected);

        if (selected && borderGroup != null)
            borderGroup.alpha = 1f;

        if (showDebugLogs)
        {
            Debug.Log(
                "[GoldenBorder] " + gameObject.name +
                (selected ? " SELECTED" : " DESELECTED"),
                this
            );
        }
    }

    // ==========================================
    // GLOW
    // ==========================================

    private void Update()
    {
        if (goldenBorder == null ||
            !goldenBorder.activeInHierarchy ||
            borderGroup == null)
            return;

        if (!animateGlow)
        {
            borderGroup.alpha = 1f;
            return;
        }

        float alpha =
            (Mathf.Sin(Time.unscaledTime * glowSpeed) + 1f)
            * 0.5f;

        borderGroup.alpha = Mathf.Lerp(
            minAlpha,
            maxAlpha,
            alpha
        );
    }

    // ==========================================
    // TEST
    // ==========================================

    [ContextMenu("TEST - Show Golden Border")]
    public void TestShowBorder()
    {
        SetupBorder();
        SetSelected(true);
    }

    [ContextMenu("TEST - Hide Golden Border")]
    public void TestHideBorder()
    {
        SetSelected(false);
    }
}
