using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
   
    public class LocalizationManager : MonoBehaviour
    {
        private const string PREF_KEY = "SELECTED_LANGUAGE";

        public static LocalizationManager Instance { get; private set; }

        [SerializeField] private LocalizationDatabase database;
        [SerializeField] private LanguageCode fallbackLanguage = LanguageCode.English;

        public LanguageCode CurrentLanguage { get; private set; }

        private static readonly List<ILocalizedElement> RegisteredElements = new List<ILocalizedElement>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            CurrentLanguage = LoadSavedLanguageOrDetect();
        }

        private LanguageCode LoadSavedLanguageOrDetect()
        {
            if (PlayerPrefs.HasKey(PREF_KEY))
            {
                return (LanguageCode)PlayerPrefs.GetInt(PREF_KEY);
            }

            // Chưa từng chọn -> tự nhận diện theo ngôn ngữ hệ thống của máy
            return Application.systemLanguage == SystemLanguage.Vietnamese
                ? LanguageCode.Vietnamese
                : fallbackLanguage;
        }

        
        public void SetLanguage(LanguageCode newLanguage)
        {
            CurrentLanguage = newLanguage;

            PlayerPrefs.SetInt(PREF_KEY, (int)newLanguage);
            PlayerPrefs.Save();

            RefreshAllRegisteredElements();
            RefreshEverythingInScene(); // Bắt luôn cả các object đang ẩn/inactive
        }

        public string GetText(string key)
        {
            if (database == null)
            {
                Debug.LogError("[LocalizationManager] Chưa gán Database!");
                return $"[{key}]";
            }

            return database.GetText(key, CurrentLanguage);
        }

      
        public static void Register(ILocalizedElement element)
        {
            if (!RegisteredElements.Contains(element))
                RegisteredElements.Add(element);

            // Cập nhật ngay khi vừa đăng ký, để hiện đúng ngôn ngữ ngay khi bật lên
            if (Instance != null)
                element.ApplyLanguage(Instance.CurrentLanguage);
        }

        public static void Unregister(ILocalizedElement element)
        {
            RegisteredElements.Remove(element);
        }

        private void RefreshAllRegisteredElements()
        {
            foreach (var element in RegisteredElements)
            {
                element.ApplyLanguage(CurrentLanguage);
            }
        }

       
        private void RefreshEverythingInScene()
        {
            var allBehaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include
            );

            int count = 0;
            foreach (var behaviour in allBehaviours)
            {
                if (behaviour is ILocalizedElement element)
                {
                    element.ApplyLanguage(CurrentLanguage);
                    count++;
                }
            }

            Debug.Log($"[LocalizationManager] Đã cập nhật {count} thành phần theo ngôn ngữ {CurrentLanguage}.");
        }
    }
}
