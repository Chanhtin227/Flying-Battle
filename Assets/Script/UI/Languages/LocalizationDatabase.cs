using System;
using System.Collections.Generic;
using UnityEngine;

namespace Localization
{
 
    [Serializable]
    public class LocalizationEntry
    {
        public string Key;

        [TextArea(1, 3)] public string Vietnamese;
        [TextArea(1, 3)] public string English;

        public string GetText(LanguageCode language)
        {
            return language switch
            {
                LanguageCode.Vietnamese => Vietnamese,
                LanguageCode.English => English,
                _ => English
            };
        }
    }

  
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
