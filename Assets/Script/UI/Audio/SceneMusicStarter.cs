using UnityEngine;

namespace GameAudio
{
   
    public class SceneMusicStarter : MonoBehaviour
    {
        [Tooltip("Kéo file nhạc nền của SCENE NÀY vào đây")]
        [SerializeField] private AudioClip backgroundMusic;

        private void Start()
        {
            if (MusicPlayer.Instance != null)
            {
                MusicPlayer.Instance.PlayMusic(backgroundMusic);
            }
            else
            {
                Debug.LogError("[SceneMusicStarter] Không tìm thấy MusicPlayer trong scene!");
            }
        }
    }
}
