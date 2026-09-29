using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings
{
    /// <summary>
    /// Gắn vào GameObject cha của panel "Gameplay/Điều khiển" trong màn Cài đặt.
    /// Ô nào không dùng có thể để trống, script tự bỏ qua.
    /// </summary>
    public class GameplaySettingsUI : MonoBehaviour
    {
        [Header("Độ nhạy chuột")]
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text sensitivityValueText;

        [Header("Tâm súng - Chọn ảnh PNG")]
        [Tooltip("Cùng asset CrosshairLibrary mà CrosshairUI đang dùng")]
        [SerializeField] private CrosshairLibrary library;
        [Tooltip("Nơi các ô chọn ảnh được tự tạo ra (nếu chưa có Layout Group, script tự thêm Grid)")]
        [SerializeField] private RectTransform styleButtonContainer;
        [SerializeField] private Vector2 styleButtonSize = new Vector2(64f, 64f);
        [SerializeField] private Color styleNormalColor = new Color(1f, 1f, 1f, 0.15f);
        [SerializeField] private Color styleSelectedColor = new Color(1f, 0.7f, 0.1f, 0.9f);
        [SerializeField] private Slider imageSizeSlider;
        [SerializeField] private TMP_Text imageSizeValueText;

        [Header("Đặt lại mặc định")]
        [SerializeField] private Button resetSensitivityButton;
        [SerializeField] private Button resetCrosshairButton;

        private readonly List<KeyValuePair<int, Image>> _styleButtons = new List<KeyValuePair<int, Image>>();

        private void Awake()
        {
            Hook(sensitivitySlider, sensitivityValueText, GameplaySettings.MinSensitivity,
                 GameplaySettings.MaxSensitivity, false, "0.00", GameplaySettings.SetMouseSensitivity);
            Hook(imageSizeSlider, imageSizeValueText, GameplaySettings.MinImageSize,
                 GameplaySettings.MaxImageSize, true, "0", GameplaySettings.SetCrosshairImageSize);

            BuildStyleButtons();

            if (resetSensitivityButton != null)
                resetSensitivityButton.onClick.AddListener(() =>
                {
                    GameplaySettings.ResetSensitivity();
                    RefreshAll();
                });

            if (resetCrosshairButton != null)
                resetCrosshairButton.onClick.AddListener(() =>
                {
                    GameplaySettings.ResetCrosshair();
                    RefreshAll();
                });
        }

        private void OnEnable()
        {
            RefreshAll();
        }

        private void OnDisable()
        {
            // Ghi xuống đĩa khi đóng panel
            GameplaySettings.Save();
        }

        private void RefreshAll()
        {
            SetSlider(sensitivitySlider, sensitivityValueText, GameplaySettings.MouseSensitivity, "0.00");
            SetSlider(imageSizeSlider, imageSizeValueText, GameplaySettings.CrosshairImageSize, "0");
            RefreshStyleSelection();
        }

        // ---------- Chọn ảnh tâm súng PNG ----------

        /// <summary>Ô đầu tiên là kiểu 4 vạch mặc định, các ô sau là từng ảnh PNG trong CrosshairLibrary.</summary>
        private void BuildStyleButtons()
        {
            if (styleButtonContainer == null) return;

            // Xóa ô cũ (nếu có ô mẫu) để tránh bị nhân đôi
            for (int i = styleButtonContainer.childCount - 1; i >= 0; i--)
                Destroy(styleButtonContainer.GetChild(i).gameObject);

            if (styleButtonContainer.GetComponent<LayoutGroup>() == null)
            {
                var grid = styleButtonContainer.gameObject.AddComponent<GridLayoutGroup>();
                grid.cellSize = styleButtonSize;
                grid.spacing = new Vector2(8f, 8f);
            }

            _styleButtons.Clear();

            CreateStyleButton(-1, null);

            int count = library != null ? library.Count : 0;
            for (int i = 0; i < count; i++)
            {
                Sprite sprite = library.Get(i);
                if (sprite != null) CreateStyleButton(i, sprite);
            }
        }

        private void CreateStyleButton(int styleIndex, Sprite sprite)
        {
            var go = new GameObject("Style_" + styleIndex, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(styleButtonContainer, false);

            var background = go.GetComponent<Image>();
            background.color = styleNormalColor;

            var button = go.GetComponent<Button>();
            button.targetGraphic = background;

            if (sprite != null)
            {
                var icon = CreateChildImage(go.transform, "Icon");
                icon.sprite = sprite;
                icon.preserveAspect = true;
                Stretch(icon.rectTransform, 8f);
            }
            else
            {
                // Ô mặc định: dấu cộng nhỏ đại diện cho kiểu 4 vạch
                var horizontal = CreateChildImage(go.transform, "H");
                var vertical = CreateChildImage(go.transform, "V");
                horizontal.rectTransform.sizeDelta = new Vector2(styleButtonSize.x * 0.5f, 3f);
                vertical.rectTransform.sizeDelta = new Vector2(3f, styleButtonSize.y * 0.5f);
            }

            int captured = styleIndex;
            button.onClick.AddListener(() =>
            {
                GameplaySettings.SetCrosshairStyleIndex(captured);
                RefreshStyleSelection();
            });

            _styleButtons.Add(new KeyValuePair<int, Image>(styleIndex, background));
        }

        private static Image CreateChildImage(Transform parent, string childName)
        {
            var go = new GameObject(childName, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);

            var image = go.GetComponent<Image>();
            image.raycastTarget = false;
            var rt = image.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            return image;
        }

        private static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }

        /// <summary>Tô sáng ô đang chọn; chỉ cho chỉnh kích thước khi đang dùng ảnh PNG.</summary>
        private void RefreshStyleSelection()
        {
            int current = GameplaySettings.CrosshairStyleIndex;
            if (library == null || library.Get(current) == null) current = -1;

            foreach (var item in _styleButtons)
                item.Value.color = item.Key == current ? styleSelectedColor : styleNormalColor;

            if (imageSizeSlider != null) imageSizeSlider.interactable = current >= 0;
        }

        // ---------- Hàm hỗ trợ ----------
        private void Hook(Slider slider, TMP_Text label, float min, float max, bool whole,
                          string format, Action<float> setter)
        {
            if (slider == null) return;

            slider.minValue = min;
            slider.maxValue = max;
            slider.wholeNumbers = whole;

            slider.onValueChanged.AddListener(v =>
            {
                setter(v);
                UpdateLabel(label, v, format);
            });
        }

        private void SetSlider(Slider slider, TMP_Text label, float value, string format)
        {
            if (slider != null) slider.SetValueWithoutNotify(value);
            UpdateLabel(label, value, format);
        }

        private static void UpdateLabel(TMP_Text label, float value, string format)
        {
            if (label != null) label.text = value.ToString(format);
        }
    }
}