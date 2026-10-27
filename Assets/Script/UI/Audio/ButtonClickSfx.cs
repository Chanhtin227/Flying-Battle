
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace GameAudio
{
    [RequireComponent(typeof(Button))]
    public class ButtonClickSfx : MonoBehaviour,
        IPointerClickHandler,
        ISubmitHandler
    {
        [Header("Button Click Sound")]
        [Tooltip("Am thanh khi nguoi choi bam nut.")]
        [SerializeField] private AudioClip clickClip;

        private Button _button;

        private void Awake()
        {
            _button = GetComponent<Button>();
        }

        // =====================================================
        // CLICK BANG CHUOT HOAC CAM UNG
        // =====================================================

        public void OnPointerClick(PointerEventData eventData)
        {
            if (eventData.button !=
                PointerEventData.InputButton.Left)
                return;

            PlayClickSound();
        }

        // =====================================================
        // BAM BANG ENTER / SPACE / GAMEPAD
        // =====================================================

        public void OnSubmit(BaseEventData eventData)
        {
            PlayClickSound();
        }

        // =====================================================
        // PLAY SOUND
        // =====================================================

        private void PlayClickSound()
        {
            if (_button == null || !_button.IsActive() ||
                !_button.IsInteractable())
                return;

            if (clickClip == null)
                return;

            if (SfxPlayer.Instance == null)
                return;

            SfxPlayer.Instance.PlaySfx(clickClip);
        }
    }
}
