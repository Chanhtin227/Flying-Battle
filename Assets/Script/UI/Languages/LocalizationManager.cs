using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    /// <summary>
    /// SINGLETON quản lý ngôn ngữ hiện tại của toàn bộ game.
    ///
    /// CÁCH DÙNG:
    /// 1. Tạo 1 GameObject rỗng (VD: "LocalizationManager") trong scene đầu tiên, gắn script này.
    /// 2. Kéo asset LocalizationDatabase vào ô "Database".
    /// 3. Ở bất kỳ đâu, chỉ cần gọi:
    ///        LocalizationManager.Instance.SetLanguage(LanguageCode.English);
    ///    -> TOÀN BỘ Text trong game (kể cả đang ẩn trong panel khác) sẽ tự đổi ngôn ngữ NGAY LẬP TỨC.
    ///    Không cần gọi thêm bất kỳ hàm refresh nào khác.
    /// </summary>
    public class LocalizationManager : MonoBehaviour
    {
        private const string PREF_KEY = "SELECTED_LANGUAGE";

        public static LocalizationManager Instance { get; private set; }

        [SerializeField] private LocalizationDatabase database;
        [SerializeField] private LanguageCode fallbackLanguage = LanguageCode.English;

        public LanguageCode CurrentLanguage { get; private set; }

        // Danh sách mọi component đang lắng nghe (đăng ký khi bật, hủy đăng ký khi tắt)
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
            return Application.systemLanguage switch
            {
                SystemLanguage.Vietnamese => LanguageCode.Vietnamese,
                SystemLanguage.Japanese => LanguageCode.Japanese,
                SystemLanguage.Chinese => LanguageCode.Chinese,
                SystemLanguage.ChineseSimplified => LanguageCode.Chinese,
                SystemLanguage.ChineseTraditional => LanguageCode.Chinese,
                _ => fallbackLanguage
            };
        }

        /// <summary>
        /// Đổi ngôn ngữ hiện tại của toàn bộ game. Gọi hàm này từ bất kỳ nút bấm nào
        /// (Dropdown, Card VI/EN, Toggle...). Mọi Text đã đăng ký sẽ tự cập nhật ngay,
        /// và toàn bộ scene (kể cả object đang ẩn) cũng được quét lại để đảm bảo không sót.
        /// </summary>
        public void SetLanguage(LanguageCode newLanguage)
        {
            CurrentLanguage = newLanguage;

            PlayerPrefs.SetInt(PREF_KEY, (int)newLanguage);
            PlayerPrefs.Save();

            RefreshAllRegisteredElements();
            RefreshEverythingInScene(); // Bắt luôn cả các object đang ẩn/inactive
        }

        /// <summary>Lấy chuỗi dịch theo Key, dùng ngôn ngữ hiện tại.</summary>
        public string GetText(string key)
        {
            if (database == null)
            {
                Debug.LogError("[LocalizationManager] Chưa gán Database!");
                return $"[{key}]";
            }

            return database.GetText(key, CurrentLanguage);
        }

        // ---------- Cơ chế đăng ký tự động ----------

        /// <summary>Được các component Localized* tự gọi khi chúng OnEnable. Không cần gọi tay.</summary>
        public static void Register(ILocalizedElement element)
        {
            if (!RegisteredElements.Contains(element))
                RegisteredElements.Add(element);

            // Cập nhật ngay khi vừa đăng ký, để hiện đúng ngôn ngữ ngay khi bật lên
            if (Instance != null)
                element.ApplyLanguage(Instance.CurrentLanguage);
        }

        /// <summary>Được các component Localized* tự gọi khi chúng OnDisable. Không cần gọi tay.</summary>
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

        /// <summary>
        /// Quét toàn bộ scene (bao gồm object đang ẩn) để chắc chắn 100% không có Text nào
        /// bị bỏ sót — ví dụ Text nằm trong 1 panel Settings khác đang tắt lúc đổi ngôn ngữ.
        /// </summary>
        private void RefreshEverythingInScene()
        {
            var allBehaviours = FindObjectsByType<MonoBehaviour>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None
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