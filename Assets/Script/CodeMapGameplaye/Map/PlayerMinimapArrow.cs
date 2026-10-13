
using UnityEngine;

public class PlayerMinimapArrow : MonoBehaviour
{
    // =========================================================
    // REFERENCES
    // =========================================================

    [Header("Player")]
    public Transform localPlayer;

    [Header("Arrow UI")]
    public RectTransform arrowRect;

    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Rotation Settings")]

    [Tooltip("Tốc độ xoay mượt của mũi tên.")]
    public float rotationSpeed = 15f;

    [Tooltip("Chỉnh nếu hình mũi tên không hướng lên trên.")]
    public float rotationOffset = 0f;

    public bool smoothRotation = true;

    // =========================================================
    // UPDATE
    // =========================================================

    private void LateUpdate()
    {
        // Tự tìm Player local trong Photon Fusion.
        if (PlayerMovement.LocalPlayer != null)
        {
            localPlayer =
                PlayerMovement.LocalPlayer.transform;
        }
        else
        {
            localPlayer = null;
        }

        if (localPlayer == null)
            return;

        if (arrowRect == null)
            return;

        // Lấy góc quay Y của Player trong thế giới.
        float playerYaw = localPlayer.eulerAngles.y;

        // MapCamera cố định hướng Bắc lên trên.
        // Player quay phải -> mũi tên quay phải.
        float targetAngle = -playerYaw + rotationOffset;

        Quaternion targetRotation =
            Quaternion.Euler(0f, 0f, targetAngle);

        if (smoothRotation)
        {
            float t = 1f -
                Mathf.Exp(-rotationSpeed * Time.deltaTime);

            arrowRect.localRotation =
                Quaternion.Slerp(
                    arrowRect.localRotation,
                    targetRotation,
                    t
                );
        }
        else
        {
            arrowRect.localRotation = targetRotation;
        }
    }
}
