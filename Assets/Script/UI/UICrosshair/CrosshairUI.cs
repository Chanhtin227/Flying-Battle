using UnityEngine;
using UnityEngine.UI;

namespace GameSettings
{
    /// <summary>
    /// Vẽ tâm súng và TỰ CẬP NHẬT khi người chơi đổi cài đặt. Hỗ trợ 2 kiểu:
    ///  - Mặc định: 4 vạch + 1 chấm giữa, thông số cố định (hằng số bên dưới).
    ///  - Kiểu ảnh: 1 ảnh PNG trong CrosshairLibrary, người chơi chọn trong Cài đặt.
    ///
    /// CÁCH DÙNG:
    /// 1. Tạo GameObject rỗng con của Canvas, tên "Crosshair", Anchor = giữa màn hình, Pos (0,0).
    ///    Gắn script này vào.
    /// 2. Kéo asset CrosshairLibrary vào ô "Library".
    /// 3. Không cần tạo sẵn Image con, script tự tạo lúc chạy.
    /// 4. Dùng lại script này ở 2 nơi: trong màn chơi và trong panel Cài đặt (ô xem trước).
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class CrosshairUI : MonoBehaviour
    {
        // Thông số kiểu 4 vạch mặc định (đổi ở đây nếu muốn)
        private const float LineLength = 8f;
        private const float LineThickness = 2f;
        private const float LineGap = 4f;
        private const float DotSize = 3f;
        private static readonly Color LineColor = Color.green;

        [Header("Danh sách ảnh tâm súng (cùng asset với GameplaySettingsUI)")]
        [SerializeField] private CrosshairLibrary library;

        private Image top, bottom, left, right, dot, spriteImage;
        private Image[] _lineParts;

        private void Awake()
        {
            top = CreatePart("Top");
            bottom = CreatePart("Bottom");
            left = CreatePart("Left");
            right = CreatePart("Right");
            dot = CreatePart("Dot");
            spriteImage = CreatePart("Sprite");
            spriteImage.preserveAspect = true;

            _lineParts = new[] { top, bottom, left, right };
        }

        private void OnEnable()
        {
            GameplaySettings.OnChanged += Apply;
            Apply();
        }

        private void OnDisable()
        {
            GameplaySettings.OnChanged -= Apply;
        }

        private Image CreatePart(string partName)
        {
            var go = new GameObject(partName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(transform, false);

            var img = go.GetComponent<Image>();
            img.raycastTarget = false; // Không chặn click chuột lên UI

            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            return img;
        }

        /// <summary>Đọc cài đặt hiện tại và vẽ lại tâm súng.</summary>
        public void Apply()
        {
            if (top == null) return;

            // Ảnh đã chọn không còn trong thư viện -> tự rơi về kiểu 4 vạch, không lỗi.
            Sprite sprite = library != null ? library.Get(GameplaySettings.CrosshairStyleIndex) : null;

            if (sprite != null) ApplySprite(sprite);
            else ApplyLines();
        }

        private void ApplySprite(Sprite sprite)
        {
            foreach (var part in _lineParts) part.gameObject.SetActive(false);
            dot.gameObject.SetActive(false);

            float size = GameplaySettings.CrosshairImageSize;
            spriteImage.gameObject.SetActive(true);
            spriteImage.sprite = sprite;
            Place(spriteImage, new Vector2(size, size), Vector2.zero, Color.white);
        }

        private void ApplyLines()
        {
            spriteImage.gameObject.SetActive(false);
            foreach (var part in _lineParts) part.gameObject.SetActive(true);
            dot.gameObject.SetActive(true);

            float offset = LineGap + LineLength * 0.5f;

            Place(top, new Vector2(LineThickness, LineLength), new Vector2(0, offset), LineColor);
            Place(bottom, new Vector2(LineThickness, LineLength), new Vector2(0, -offset), LineColor);
            Place(left, new Vector2(LineLength, LineThickness), new Vector2(-offset, 0), LineColor);
            Place(right, new Vector2(LineLength, LineThickness), new Vector2(offset, 0), LineColor);
            Place(dot, new Vector2(DotSize, DotSize), Vector2.zero, LineColor);
        }

        private static void Place(Image img, Vector2 size, Vector2 position, Color color)
        {
            img.rectTransform.sizeDelta = size;
            img.rectTransform.anchoredPosition = position;
            img.color = color;
        }
    }
}