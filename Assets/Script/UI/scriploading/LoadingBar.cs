using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LoadingBar : MonoBehaviour
{
    public Image fillImage;       // Image thanh fill
    public RectTransform bird;    // Birt
    public TextMeshProUGUI percentText;

    public float loadingSpeed = 0.2f;

    private float progress = 0f;

    void Update()
    {
        progress += loadingSpeed * Time.deltaTime;
        progress = Mathf.Clamp01(progress);

        // Cập nhật thanh loading
        fillImage.fillAmount = progress;

        // Di chuyển bird theo thanh loading
        MoveBird();

        // Hiện phần trăm
        if (percentText != null)
        {
            percentText.text = Mathf.RoundToInt(progress * 100) + "%";
        }
    }

    void MoveBird()
    {
        RectTransform barRect = fillImage.rectTransform;

        float width = barRect.rect.width;

        // Vị trí từ trái sang phải
        float x = Mathf.Lerp(-width / 2f, width / 2f, progress);

        bird.anchoredPosition = new Vector2(
            x,
            bird.anchoredPosition.y
        );
    }
}