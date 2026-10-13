using UnityEngine;

public class MapCameraFollow : MonoBehaviour
{
    // =========================================================
    // SETTINGS
    // =========================================================

    [Header("Map Camera")]

    [Tooltip("Độ cao của camera map so với player.")]
    public float height = 100f;


    [Tooltip("Camera có mượt khi bám player hay không.")]
    public bool smoothFollow = true;


    [Tooltip("Tốc độ camera bám player.")]
    public float followSpeed = 10f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Transform localPlayer;


    // =========================================================
    // UPDATE
    // =========================================================

    private void LateUpdate()
    {
        // =====================================================
        // TỰ TÌM LOCAL PLAYER
        // =====================================================

        if (PlayerMovement.LocalPlayer != null)
        {
            localPlayer =
                PlayerMovement.LocalPlayer.transform;
        }


        // =====================================================
        // CHƯA CÓ LOCAL PLAYER
        // =====================================================

        if (localPlayer == null)
        {
            return;
        }


        // =====================================================
        // TARGET POSITION
        // =====================================================

        Vector3 targetPosition =
            new Vector3(
                localPlayer.position.x,
                localPlayer.position.y + height,
                localPlayer.position.z
            );


        // =====================================================
        // FOLLOW
        // =====================================================

        if (smoothFollow)
        {
            transform.position =
                Vector3.Lerp(
                    transform.position,
                    targetPosition,
                    1f -
                    Mathf.Exp(
                        -followSpeed *
                        Time.deltaTime
                    )
                );
        }
        else
        {
            transform.position =
                targetPosition;
        }


        // =====================================================
        // TOP DOWN
        // =====================================================

        transform.rotation =
            Quaternion.Euler(
                90f,
                0f,
                0f
            );
    }
}