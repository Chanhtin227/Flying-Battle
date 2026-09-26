using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
    /// <summary>
    /// 1 dòng dữ liệu dịch: 1 Key ứng với bản dịch ở mỗi ngôn ngữ.
    /// Ví dụ: Key = "settings_language_title", Vietnamese = "Ngôn ngữ", English = "Language"
    /// </summary>
    [Serializable]
    public class LocalizationEntry
    {
        public string Key;

        [TextArea(1, 3)] public string Vietnamese;
        [TextArea(1, 3)] public string English;
        [TextArea(1, 3)] public string Japanese;
        [TextArea(1, 3)] public string Chinese;

        public string GetText(LanguageCode language)
        {
            return language switch
            {
                LanguageCode.Vietnamese => Vietnamese,
                LanguageCode.English => English,
                LanguageCode.Japanese => Japanese,
                LanguageCode.Chinese => Chinese,
                _ => English
            };
        }
    }

    /// <summary>
    /// Database chứa toàn bộ chuỗi dịch trong game.
    /// Tạo asset: chuột phải trong Project -> Create -> Localization -> Database
    /// </summary>
    [CreateAssetMenu(fileName = "LocalizationDatabase", menuName = "Localization/Database")]
    public class LocalizationDatabase : ScriptableObject
    {
        public List<LocalizationEntry> Entries = new List<LocalizationEntry>();

        private Dictionary<string, LocalizationEntry> _lookup;

        private void BuildLookup()
        {
            _lookup = new Dictionary<string, LocalizationEntry>();

            foreach (var entry in Entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.Key)) continue;

                if (!_lookup.TryAdd(entry.Key, entry))
                {
                    Debug.LogWarning($"[LocalizationDatabase] Key bị trùng: {entry.Key}");
                }
            }
        }

        /// <summary>Lấy chuỗi dịch theo Key và ngôn ngữ. Trả về "[key]" nếu không tìm thấy.</summary>
        public string GetText(string key, LanguageCode language)
        {
            if (_lookup == null) BuildLookup();

            if (_lookup.TryGetValue(key, out var entry))
            {
                string text = entry.GetText(language);
                return string.IsNullOrEmpty(text) ? $"[{key}]" : text;
            }

            Debug.LogWarning($"[LocalizationDatabase] Không tìm thấy key: {key}");
            return $"[{key}]";
        }

        /// <summary>Gọi lại nếu bạn sửa Entries lúc runtime (hiếm khi cần).</summary>
        public void InvalidateCache() => _lookup = null;
    }
}