using System;
using UnityEngine;

namespace GameSettings
{
    /// <summary>
    /// Nơi lưu cài đặt gameplay: độ nhạy chuột + chọn ảnh tâm súng.
    /// Là class static nên KHÔNG cần đặt GameObject nào vào scene:
    ///
    ///     float sens = GameplaySettings.MouseSensitivity;
    ///
    /// Mọi thay đổi tự động lưu vào PlayerPrefs và bắn sự kiện OnChanged.
    /// </summary>
    public static class GameplaySettings
    {
        public const float MinSensitivity = 0.1f, MaxSensitivity = 5f, DefaultSensitivity = 1f;
        public const float MinImageSize = 8f, MaxImageSize = 128f, DefaultImageSize = 32f;

        private const string KEY_SENS = "GS_MOUSE_SENS";
        private const string KEY_STYLE = "GS_XH_STYLE";
        private const string KEY_IMAGE_SIZE = "GS_XH_IMGSIZE";

        /// <summary>Bắn ra mỗi khi bất kỳ cài đặt nào thay đổi.</summary>
        public static event Action OnChanged;

        private static bool _loaded;
        private static float _sensitivity = DefaultSensitivity;
        private static int _styleIndex = -1;          // -1 = 4 vạch mặc định, >= 0 = ảnh PNG thứ N
        private static float _imageSize = DefaultImageSize;

        // Reset khi tắt "Domain Reload" trong Editor
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            OnChanged = null;
        }

        // ---------- Thuộc tính đọc ----------
        public static float MouseSensitivity { get { EnsureLoaded(); return _sensitivity; } }

        /// <summary>-1 = tâm súng 4 vạch mặc định; từ 0 trở lên = vị trí ảnh PNG trong CrosshairLibrary.</summary>
        public static int CrosshairStyleIndex { get { EnsureLoaded(); return _styleIndex; } }
        public static float CrosshairImageSize { get { EnsureLoaded(); return _imageSize; } }

        // ---------- Hàm ghi ----------
        public static void SetMouseSensitivity(float v)
        {
            EnsureLoaded();
            _sensitivity = Mathf.Clamp(v, MinSensitivity, MaxSensitivity);
            PlayerPrefs.SetFloat(KEY_SENS, _sensitivity);
            OnChanged?.Invoke();
        }

        public static void SetCrosshairStyleIndex(int index)
        {
            EnsureLoaded();
            _styleIndex = Mathf.Max(-1, index);
            PlayerPrefs.SetInt(KEY_STYLE, _styleIndex);
            OnChanged?.Invoke();
        }

        public static void SetCrosshairImageSize(float v)
        {
            EnsureLoaded();
            _imageSize = Mathf.Clamp(v, MinImageSize, MaxImageSize);
            PlayerPrefs.SetFloat(KEY_IMAGE_SIZE, _imageSize);
            OnChanged?.Invoke();
        }

        // ---------- Đặt lại mặc định ----------
        public static void ResetSensitivity()
        {
            SetMouseSensitivity(DefaultSensitivity);
        }

        public static void ResetCrosshair()
        {
            EnsureLoaded();
            _styleIndex = -1;
            _imageSize = DefaultImageSize;
            PlayerPrefs.SetInt(KEY_STYLE, _styleIndex);
            PlayerPrefs.SetFloat(KEY_IMAGE_SIZE, _imageSize);
            OnChanged?.Invoke();
        }

        /// <summary>Ghi xuống đĩa. UI tự gọi khi đóng panel; cũng tự gọi khi thoát game.</summary>
        public static void Save()
        {
            PlayerPrefs.Save();
        }

        // ---------- Nội bộ ----------
        private static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            _sensitivity = PlayerPrefs.GetFloat(KEY_SENS, DefaultSensitivity);
            _styleIndex = PlayerPrefs.GetInt(KEY_STYLE, -1);
            _imageSize = PlayerPrefs.GetFloat(KEY_IMAGE_SIZE, DefaultImageSize);

            Application.quitting -= Save;
            Application.quitting += Save;
        }
    }
}