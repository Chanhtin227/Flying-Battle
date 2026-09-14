using UnityEngine;
using UnityEngine.UI;

namespace GameAudio
{
    
    public class AudioSettingsUI : MonoBehaviour
    {
        [Header("3 Slider chỉnh âm lượng")]
        [SerializeField] private Slider masterSlider;
        [SerializeField] private Slider musicSlider;
        [SerializeField] private Slider sfxSlider;

        private void OnEnable()
        {
            if (AudioManager.Instance == null)
            {
                Debug.LogError("[AudioSettingsUI] Không tìm thấy AudioManager trong scene!");
                return;
            }

            
            masterSlider.SetValueWithoutNotify(AudioManager.Instance.MasterVolume);
            musicSlider.SetValueWithoutNotify(AudioManager.Instance.MusicVolume);
            sfxSlider.SetValueWithoutNotify(AudioManager.Instance.SfxVolume);

            masterSlider.onValueChanged.AddListener(OnMasterSliderChanged);
            musicSlider.onValueChanged.AddListener(OnMusicSliderChanged);
            sfxSlider.onValueChanged.AddListener(OnSfxSliderChanged);
        }

        private void OnDisable()
        {
            masterSlider.onValueChanged.RemoveListener(OnMasterSliderChanged);
            musicSlider.onValueChanged.RemoveListener(OnMusicSliderChanged);
            sfxSlider.onValueChanged.RemoveListener(OnSfxSliderChanged);
        }

        private void OnMasterSliderChanged(float value)
        {
            AudioManager.Instance.SetMasterVolume(value);
        }

        private void OnMusicSliderChanged(float value)
        {
            AudioManager.Instance.SetMusicVolume(value);
        }

        private void OnSfxSliderChanged(float value)
        {
            AudioManager.Instance.SetSfxVolume(value);
        }
    }
}
