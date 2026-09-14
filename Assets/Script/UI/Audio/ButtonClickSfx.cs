using UnityEngine;
using UnityEngine.UI;

namespace GameAudio
{
   
    [RequireComponent(typeof(Button))]
    public class ButtonClickSfx : MonoBehaviour
    {
        [Tooltip("Âm thanh khi bấm nút này. Nếu để trống sẽ không phát âm thanh.")]
        [SerializeField] private AudioClip clickClip;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
            _button.onClick.AddListener(PlayClickSound);
        }

        private void PlayClickSound()
        {
            if (clickClip != null && SfxPlayer.Instance != null)
            {
                SfxPlayer.Instance.PlaySfx(clickClip);
            }
        }
    }
}
