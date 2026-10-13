
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Xoay model C01 bang chuot trong RawImage.
/// Dung chung model voi ShopEquipmentPreview.
/// </summary>
public class CharacterRotator : MonoBehaviour, IDragHandler
{
    // =====================================================
    // CHARACTER
    // =====================================================

    [Header("C01 Preview Character")]
    [SerializeField]
    private Transform character;

    [Header("Shop Equipment Preview")]
    [SerializeField]
    private ShopEquipmentPreview equipmentPreview;

    // =====================================================
    // ROTATION SETTINGS
    // =====================================================

    [Header("Rotation")]
    [SerializeField]
    private float dragSpeed = 0.4f;

    [SerializeField]
    private bool invertDirection = false;

    // =====================================================
    // DRAG TO ROTATE
    // =====================================================

    public void OnDrag(PointerEventData e)
    {
        if (character == null || e == null)
            return;

        // Chi xoay khi keo chuot trai
        if (e.button != PointerEventData.InputButton.Left)
            return;

        float direction = invertDirection ? 1f : -1f;

        character.Rotate(
            0f,
            direction * e.delta.x * dragSpeed,
            0f,
            Space.World
        );
    }

    // =====================================================
    // CHECK CHARACTER CONNECTION
    // =====================================================

    [ContextMenu("TEST - Check Preview Character")]
    public void CheckCharacter()
    {
        if (character == null)
        {
            Debug.LogError(
                "[CharacterRotator] Character not assigned!",
                this
            );
            return;
        }

        // Khong can GetInstanceID()
        Debug.Log(
            "[CharacterRotator] Rotating Character: " +
            character.name,
            this
        );

        if (equipmentPreview == null)
        {
            Debug.LogWarning(
                "[CharacterRotator] Equipment Preview not assigned!",
                this
            );
            return;
        }

        Transform previewTarget = equipmentPreview.previewPlayer;

        if (previewTarget == null)
        {
            Debug.LogWarning(
                "[CharacterRotator] Preview Player not assigned!",
                this
            );
            return;
        }

        bool sameCharacter =
            previewTarget == character ||
            previewTarget.IsChildOf(character) ||
            character.IsChildOf(previewTarget);

        if (sameCharacter)
        {
            Debug.Log(
                "[CharacterRotator] SUCCESS! " +
                "CharacterRotator and ShopEquipmentPreview " +
                "use the same C01 model.",
                this
            );
        }
        else
        {
            Debug.LogWarning(
                "[CharacterRotator] WARNING! " +
                "CharacterRotator and ShopEquipmentPreview " +
                "are using different models.",
                this
            );
        }
    }
}
