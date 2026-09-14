using UnityEngine;
using TMPro;

namespace Localization
{
    
    [RequireComponent(typeof(TMP_Text))]
    public class LocalizedTMPText : MonoBehaviour, ILocalizedElement
    {
        [Tooltip("Key tương ứng trong LocalizationDatabase, VD: settings_language_title")]
        [SerializeField] private string key;

        private TMP_Text _text;

        private void Awake()
        {
            _text = GetComponent<TMP_Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.Register(this); // Tự đăng ký + tự cập nhật ngay lập tức
        }

        private void OnDisable()
        {
            LocalizationManager.Unregister(this);
        }

        public void ApplyLanguage(LanguageCode language)
        {
            if (string.IsNullOrEmpty(key) || LocalizationManager.Instance == null) return;

            if (_text == null) _text = GetComponent<TMP_Text>();

            _text.text = LocalizationManager.Instance.GetText(key);
        }

        public void SetKey(string newKey)
        {
            key = newKey;
            if (LocalizationManager.Instance != null)
                ApplyLanguage(LocalizationManager.Instance.CurrentLanguage);
        }
    }
}
