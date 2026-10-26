
using UnityEngine;
using System;
using System.Collections;

public class DeathCameraEffect : MonoBehaviour
{
    // =====================================================
    // CAMERA
    // =====================================================

    [Header("Camera")]
    public Camera targetCamera;

    [Header("Death Camera Settings")]
    public float riseHeight = 8f;
    public float riseDuration = 1.8f;
    public float holdDuration = 0.5f;

    [Tooltip("Lech nhe de goc nhin dep hon")]
    public Vector3 topOffset =
        new Vector3(0f, 0f, -1.5f);

    // =====================================================
    // EQUIPMENT HUD
    // =====================================================

    [Header("Hide Equipment HUD During Effect")]
    public bool hideEquipmentHUD = true;

    [Tooltip("Tu tim EquipmentHUD neu chua gan.")]
    public bool autoFindEquipmentHUD = true;

    [Tooltip("Bat lai EquipmentHUD sau khi Camera ket thuc. Tat neu dung cho WIN/LOSE.")]
    public bool restoreEquipmentHUDAfterEffect = false;

    public GameObject equipmentHUD;

    // =====================================================
    // PRIVATE
    // =====================================================

    private bool isPlaying = false;
    private Coroutine effectCoroutine;

    private CanvasGroup equipmentCanvasGroup;

    private float originalAlpha = 1f;
    private bool originalInteractable = true;
    private bool originalBlocksRaycasts = true;

    private bool equipmentHUDHidden = false;

    // =====================================================
    // PLAY DEATH CAMERA
    // =====================================================

    public void PlayDeathCamera(
        Transform player,
        Action onFinished)
    {
        if (isPlaying)
            return;

        effectCoroutine = StartCoroutine(
            DeathCameraRoutine(player, onFinished)
        );
    }

    // =====================================================
    // HIDE EQUIPMENT HUD
    // =====================================================

    private void HideEquipmentHUD()
    {
        if (!hideEquipmentHUD)
            return;

        if (equipmentHUD == null && autoFindEquipmentHUD)
        {
            EquipmentHUD foundHUD =
                FindFirstObjectByType<EquipmentHUD>(
                    FindObjectsInactive.Include
                );

            if (foundHUD != null)
                equipmentHUD = foundHUD.gameObject;
        }

        if (equipmentHUD == null)
        {
            Debug.LogWarning(
                "[DeathCameraEffect] Khong tim thay EquipmentHUD.",
                this
            );
            return;
        }

        equipmentCanvasGroup =
            equipmentHUD.GetComponent<CanvasGroup>();

        if (equipmentCanvasGroup == null)
        {
            equipmentCanvasGroup =
                equipmentHUD.AddComponent<CanvasGroup>();
        }

        if (!equipmentHUDHidden)
        {
            originalAlpha = equipmentCanvasGroup.alpha;
            originalInteractable =
                equipmentCanvasGroup.interactable;
            originalBlocksRaycasts =
                equipmentCanvasGroup.blocksRaycasts;

            equipmentHUDHidden = true;
        }

        equipmentCanvasGroup.alpha = 0f;
        equipmentCanvasGroup.interactable = false;
        equipmentCanvasGroup.blocksRaycasts = false;
    }

    // =====================================================
    // RESTORE EQUIPMENT HUD
    // =====================================================

    private void RestoreEquipmentHUD()
    {
        if (!equipmentHUDHidden)
            return;

        if (equipmentCanvasGroup != null)
        {
            equipmentCanvasGroup.alpha = originalAlpha;
            equipmentCanvasGroup.interactable =
                originalInteractable;
            equipmentCanvasGroup.blocksRaycasts =
                originalBlocksRaycasts;
        }

        equipmentHUDHidden = false;
    }

    // =====================================================
    // DEATH CAMERA ROUTINE
    // =====================================================

    private IEnumerator DeathCameraRoutine(
        Transform player,
        Action onFinished)
    {
        isPlaying = true;

        // An HUD ngay khi bat dau hieu ung.
        HideEquipmentHUD();

        if (targetCamera == null)
            targetCamera = Camera.main;

        if (targetCamera == null || player == null)
        {
            FinishEffect(onFinished);
            yield break;
        }

        // =================================================
        // DISABLE THIRD PERSON CAMERA
        // =================================================

        ThirdPersonCamera thirdPersonCamera =
            targetCamera.GetComponent<ThirdPersonCamera>();

        if (thirdPersonCamera != null)
            thirdPersonCamera.enabled = false;

        // =================================================
        // START CAMERA
        // =================================================

        Vector3 startPosition =
            targetCamera.transform.position;

        Quaternion startRotation =
            targetCamera.transform.rotation;

        // =================================================
        // END POSITION
        // =================================================

        Vector3 endPosition =
            player.position +
            Vector3.up * riseHeight +
            topOffset;

        Vector3 lookPoint =
            player.position + Vector3.up;

        Vector3 direction = lookPoint - endPosition;

        Quaternion endRotation =
            direction.sqrMagnitude > 0.001f
                ? Quaternion.LookRotation(direction)
                : startRotation;

        // =================================================
        // CAMERA RISE
        // =================================================

        float timer = 0f;
        float safeRiseDuration = Mathf.Max(0.01f, riseDuration);

        while (timer < safeRiseDuration)
        {
            if (targetCamera == null)
            {
                FinishEffect(onFinished);
                yield break;
            }

            timer += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                timer / safeRiseDuration
            );

            float smoothT = t * t * (3f - 2f * t);

            targetCamera.transform.position =
                Vector3.Lerp(
                    startPosition,
                    endPosition,
                    smoothT
                );

            targetCamera.transform.rotation =
                Quaternion.Slerp(
                    startRotation,
                    endRotation,
                    smoothT
                );

            yield return null;
        }

        // =================================================
        // FINAL CAMERA POSITION
        // =================================================

        if (targetCamera != null)
        {
            targetCamera.transform.position = endPosition;
            targetCamera.transform.rotation = endRotation;
        }

        // =================================================
        // HOLD CAMERA
        // =================================================

        if (holdDuration > 0f)
        {
            yield return new WaitForSecondsRealtime(
                holdDuration
            );
        }

        FinishEffect(onFinished);
    }

    // =====================================================
    // FINISH
    // =====================================================

    private void FinishEffect(Action onFinished)
    {
        isPlaying = false;
        effectCoroutine = null;

        if (restoreEquipmentHUDAfterEffect)
        {
            RestoreEquipmentHUD();
        }

        onFinished?.Invoke();
    }

    // =====================================================
    // ON DISABLE
    // =====================================================

    private void OnDisable()
    {
        if (effectCoroutine != null)
        {
            StopCoroutine(effectCoroutine);
            effectCoroutine = null;
        }

        isPlaying = false;

        if (restoreEquipmentHUDAfterEffect)
        {
            RestoreEquipmentHUD();
        }
    }
}
