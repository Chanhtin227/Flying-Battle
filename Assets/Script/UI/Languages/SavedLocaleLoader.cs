using System.Collections;
using UnityEngine;
using UnityEngine.Localization.Settings;

namespace Localization
{
  
    public class SavedLocaleLoader : MonoBehaviour
    {
        private IEnumerator Start()
        {
            // Bắt buộc chờ Localization Settings khởi tạo xong trước khi đổi Locale
            yield return LocalizationSettings.InitializationOperation;

            if (PlayerPrefs.HasKey("SELECTED_LOCALE_CODE"))
            {
                string savedCode = PlayerPrefs.GetString("SELECTED_LOCALE_CODE");

                foreach (var locale in LocalizationSettings.AvailableLocales.Locales)
                {
                    if (locale.Identifier.Code == savedCode)
                    {
                        LocalizationSettings.SelectedLocale = locale;
                        break;
                    }
                }
            }
        }
    }
}
