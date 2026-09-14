using UnityEngine;
using UnityEngine.Audio;

namespace GameAudio
{
   
    [RequireComponent(typeof(AudioSource))]
    public class SfxPlayer : MonoBehaviour
    {
        public static SfxPlayer Instance { get; private set; }

        private AudioSource _audioSource;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            _audioSource = GetComponent<AudioSource>();
            _audioSource.playOnAwake = false;

            // Gán đúng group SFX từ AudioManager để chịu ảnh hưởng của Slider hiệu ứng
            if (AudioManager.Instance != null && AudioManager.Instance.SfxGroup != null)
            {
                _audioSource.outputAudioMixerGroup = AudioManager.Instance.SfxGroup;
            }
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (clip == null) return;
            _audioSource.PlayOneShot(clip, volumeScale);
        }
    }
}
