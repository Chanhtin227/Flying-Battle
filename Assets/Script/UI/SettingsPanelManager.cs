using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameSettings
{
    
    public class SettingsPanelManager : MonoBehaviour
    {
        [System.Serializable]
        public class SettingsPanelEntry
        {
            [Tooltip("Tên gợi nhớ, không bắt buộc, chỉ để dễ nhìn trong Inspector")]
            public string label;

            [Tooltip("Nút bấm để mở/đóng panel này")]
            public Button button;

            [Tooltip("Panel tương ứng sẽ được bật/tắt")]
            public GameObject panel;
        }

        [Header("Danh sách các mục Settings (Ngôn ngữ, Âm thanh, Đồ họa...)")]
        [SerializeField] private List<SettingsPanelEntry> entries = new List<SettingsPanelEntry>();

        [Tooltip("Nếu bật: bấm lại đúng nút đang mở sẽ đóng panel đó lại. Nếu tắt: bấm nút đang mở sẽ không làm gì (chỉ đóng được bằng cách mở panel khác hoặc nút X riêng)")]
        [SerializeField] private bool allowCloseByClickingSameButton = true;

        private GameObject _currentOpenPanel;

        private void Awake()
        {
            foreach (var entry in entries)
            {
                if (entry.button == null || entry.panel == null) continue;

                entry.panel.SetActive(false);

                var capturedEntry = entry;
                entry.button.onClick.AddListener(() => OnEntryButtonClicked(capturedEntry));
            }
        }

        private void OnEntryButtonClicked(SettingsPanelEntry entry)
        {
            bool isThisPanelCurrentlyOpen = _currentOpenPanel == entry.panel;

            if (_currentOpenPanel != null)
            {
                _currentOpenPanel.SetActive(false);
                _currentOpenPanel = null;
            }

            if (isThisPanelCurrentlyOpen && allowCloseByClickingSameButton)
            {
                return;
            }

            entry.panel.SetActive(true);
            _currentOpenPanel = entry.panel;
        }

        public void CloseAllPanels()
        {
            foreach (var entry in entries)
            {
                if (entry.panel != null)
                    entry.panel.SetActive(false);
            }

            _currentOpenPanel = null;
        }
    }
}
