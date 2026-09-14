using UnityEngine;

namespace GameAudio
{
  
    [RequireComponent(typeof(AudioSource))]
    public class MusicPlayer : MonoBehaviour
    {
        public static MusicPlayer Instance { get; private set; }

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
            _audioSource.loop = true;
            _audioSource.playOnAwake = false;

            if (AudioManager.Instance != null && AudioManager.Instance.MusicGroup != null)
            {
                _audioSource.outputAudioMixerGroup = AudioManager.Instance.MusicGroup;
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip == null) return;

            if (_audioSource.clip == clip && _audioSource.isPlaying) return;

            _audioSource.clip = clip;
            _audioSource.Play();
        }

        public void StopMusic()
        {
            _audioSource.Stop();
        }
    }
}
