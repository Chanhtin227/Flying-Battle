using UnityEngine;
using UnityEngine.UI;

namespace Localization
{
    
    [RequireComponent(typeof(Button))]
    public class LanguageSwitchButton : MonoBehaviour
    {
        [SerializeField] private LanguageCode language;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(HandleClick);
        }

        private void HandleClick()
        {
            if (LocalizationManager.Instance == null)
            {
                Debug.LogError("[LanguageSwitchButton] Không tìm thấy LocalizationManager trong scene!");
                return;
            }

            LocalizationManager.Instance.SetLanguage(language);
        }
    }
}
