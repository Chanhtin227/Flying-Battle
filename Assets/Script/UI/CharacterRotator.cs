using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Gắn vào Raw Image "CharacterView".
/// Nhấn giữ chuột trái và kéo trái/phải để xoay nhân vật quanh trục Y.
/// </summary>
public class CharacterRotator : MonoBehaviour, IDragHandler
{
    [Header("Nhân vật (model hoặc Empty cha đặt ở chân)")]
    [SerializeField] private Transform character;

    [Header("Cài đặt")]
    [SerializeField] private float dragSpeed = 0.4f;   // tăng nếu muốn xoay nhanh hơn
    [SerializeField] private bool invertDirection = false; // bật nếu xoay ngược chiều kéo

    public void OnDrag(PointerEventData e)
    {
        if (character == null) return;

        float dir = invertDirection ? 1f : -1f;
        character.Rotate(0f, dir * e.delta.x * dragSpeed, 0f, Space.World);
    }
}
