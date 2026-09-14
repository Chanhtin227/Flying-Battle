using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace Localization
{
   
    [RequireComponent(typeof(Button))]
    public class UnityLocaleSwitchButton : MonoBehaviour
    {
        [Tooltip("Mã locale khớp với cột trong Localization Tables, VD: vi-VN, en-US")]
        [SerializeField] private string localeCode;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(SwitchToThisLocale);
        }

        private void SwitchToThisLocale()
        {
            var targetLocale = FindLocaleByCode(localeCode);

            if (targetLocale == null)
            {
                Debug.LogError($"[UnityLocaleSwitchButton] Không tìm thấy Locale có mã '{localeCode}'. " +
                                "Kiểm tra lại chính tả, hoặc mở Localization Tables để xem đúng mã.");
                return;
            }

            LocalizationSettings.SelectedLocale = targetLocale;

            // Lưu lựa chọn để lần sau mở game vẫn giữ đúng ngôn ngữ
            PlayerPrefs.SetString("SELECTED_LOCALE_CODE", localeCode);
            PlayerPrefs.Save();
        }

        private Locale FindLocaleByCode(string code)
        {
            foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
            {
                if (locale.Identifier.Code == code)
                    return locale;
            }
            return null;
        }
    }
}
