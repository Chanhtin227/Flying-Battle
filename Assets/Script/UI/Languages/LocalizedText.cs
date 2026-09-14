using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
   
    [RequireComponent(typeof(Text))]
    public class LocalizedText : MonoBehaviour, ILocalizedElement
    {
        [SerializeField] private string key;

        private Text _text;

        private void Awake()
        {
            _text = GetComponent<Text>();
        }

        private void OnEnable()
        {
            LocalizationManager.Register(this);
        }

        private void OnDisable()
        {
            LocalizationManager.Unregister(this);
        }

        public void ApplyLanguage(LanguageCode language)
        {
            if (string.IsNullOrEmpty(key) || LocalizationManager.Instance == null) return;

            if (_text == null) _text = GetComponent<Text>();

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
