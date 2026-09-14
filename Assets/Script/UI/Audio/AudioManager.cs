using UnityEngine;
using UnityEngine.Audio;

namespace GameAudio
{
   
    public class AudioManager : MonoBehaviour
    {
        private const string KEY_MASTER = "AUDIO_MASTER_VOLUME";
        private const string KEY_MUSIC = "AUDIO_MUSIC_VOLUME";
        private const string KEY_SFX = "AUDIO_SFX_VOLUME";

        private const string PARAM_MASTER = "MasterVolume";
        private const string PARAM_MUSIC = "MusicVolume";
        private const string PARAM_SFX = "SFXVolume";

        public static AudioManager Instance { get; private set; }

        [Header("Audio Mixer")]
        [SerializeField] private AudioMixer mixer;

        [Header("Audio Mixer Groups (tuỳ chọn, dùng để gán cho AudioSource)")]
        [SerializeField] private AudioMixerGroup masterGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup sfxGroup;

        [Header("Giá trị mặc định (0.0 - 1.0) nếu người chơi chưa từng chỉnh")]
        [Range(0f, 1f)] [SerializeField] private float defaultMasterVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float defaultMusicVolume = 0.7f;
        [Range(0f, 1f)] [SerializeField] private float defaultSfxVolume = 1f;

        public float MasterVolume { get; private set; }
        public float MusicVolume { get; private set; }
        public float SfxVolume { get; private set; }

        public AudioMixerGroup MusicGroup => musicGroup;
        public AudioMixerGroup SfxGroup => sfxGroup;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            LoadSavedVolumes();
        }

        private void LoadSavedVolumes()
        {
            float master = PlayerPrefs.GetFloat(KEY_MASTER, defaultMasterVolume);
            float music = PlayerPrefs.GetFloat(KEY_MUSIC, defaultMusicVolume);
            float sfx = PlayerPrefs.GetFloat(KEY_SFX, defaultSfxVolume);

            SetMasterVolume(master);
            SetMusicVolume(music);
            SetSfxVolume(sfx);
        }

       
        public void SetMasterVolume(float linearVolume)
        {
            MasterVolume = linearVolume;
            ApplyVolumeToMixer(PARAM_MASTER, linearVolume);
            PlayerPrefs.SetFloat(KEY_MASTER, linearVolume);
            PlayerPrefs.Save();
        }

        public void SetMusicVolume(float linearVolume)
        {
            MusicVolume = linearVolume;
            ApplyVolumeToMixer(PARAM_MUSIC, linearVolume);
            PlayerPrefs.SetFloat(KEY_MUSIC, linearVolume);
            PlayerPrefs.Save();
        }

        public void SetSfxVolume(float linearVolume)
        {
            SfxVolume = linearVolume;
            ApplyVolumeToMixer(PARAM_SFX, linearVolume);
            PlayerPrefs.SetFloat(KEY_SFX, linearVolume);
            PlayerPrefs.Save();
        }

        
        private void ApplyVolumeToMixer(string parameterName, float linearVolume)
        {
            float clamped = Mathf.Clamp(linearVolume, 0.0001f, 1f);
            float decibel = Mathf.Log10(clamped) * 20f;

            if (mixer != null)
                mixer.SetFloat(parameterName, decibel);
        }
    }
}
