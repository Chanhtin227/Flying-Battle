using UnityEngine;
using System.Collections;

public class DeathCameraEffect : MonoBehaviour
{
    [Header("Camera")]
    public Camera targetCamera;

    [Header("Death Camera Settings")]
    public float riseHeight = 8f;
    public float riseDuration = 1.8f;
    public float holdDuration = 0.5f;

    [Tooltip("Lệch nhẹ để góc nhìn đẹp hơn")]
    public Vector3 topOffset =
        new Vector3(
            0f,
            0f,
            -1.5f
        );

    private bool isPlaying = false;


    public void PlayDeathCamera(
        Transform player,
        System.Action onFinished)
    {
        if (isPlaying)
            return;


        StartCoroutine(
            DeathCameraRoutine(
                player,
                onFinished
            )
        );
    }


    private IEnumerator DeathCameraRoutine(
        Transform player,
        System.Action onFinished)
    {
        isPlaying = true;


        if (targetCamera == null)
        {
            targetCamera =
                Camera.main;
        }


        if (targetCamera == null ||
            player == null)
        {
            isPlaying = false;

            onFinished?.Invoke();

            yield break;
        }


        // =====================================================
        // TẮT CAMERA TPS
        // =====================================================

        ThirdPersonCamera thirdPersonCamera =
            targetCamera.GetComponent<ThirdPersonCamera>();


        if (thirdPersonCamera != null)
        {
            thirdPersonCamera.enabled =
                false;
        }


        // =====================================================
        // VỊ TRÍ CAMERA BAN ĐẦU
        // =====================================================

        Vector3 startPosition =
            targetCamera.transform.position;


        Quaternion startRotation =
            targetCamera.transform.rotation;


        // =====================================================
        // VỊ TRÍ CAMERA TRÊN CAO
        // =====================================================

        Vector3 endPosition =
            player.position +
            Vector3.up *
            riseHeight +
            topOffset;


        Vector3 lookPoint =
            player.position +
            Vector3.up *
            1f;


        Quaternion endRotation =
            Quaternion.LookRotation(
                lookPoint -
                endPosition
            );


        // =====================================================
        // BAY LÊN
        // =====================================================

        float timer =
            0f;


        while (timer <
               riseDuration)
        {
            timer +=
                Time.unscaledDeltaTime;


            float t =
                Mathf.Clamp01(
                    timer /
                    riseDuration
                );


            float smoothT =
                t *
                t *
                (3f - 2f * t);


            targetCamera
                .transform
                .position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    smoothT
                );


            targetCamera
                .transform
                .rotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    smoothT
                );


            yield return null;
        }


        // =====================================================
        // CHỐT GÓC NHÌN
        // =====================================================

        targetCamera.transform.position =
            endPosition;


        targetCamera.transform.LookAt(
            lookPoint
        );


        // =====================================================
        // GIỮ CAMERA TRÊN CAO
        // =====================================================

        yield return
            new WaitForSecondsRealtime(
                holdDuration
            );


        isPlaying = false;


        onFinished?.Invoke();
    }
}