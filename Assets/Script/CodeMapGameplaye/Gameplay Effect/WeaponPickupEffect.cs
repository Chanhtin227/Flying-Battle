
using UnityEngine;

public class WeaponPickupEffect : MonoBehaviour
{
    [Header("Visual Object")]
    public Transform visualObject;

    [Header("Rotation")]
    public bool enableRotation = true;
    public float rotationSpeed = 90f;

    [Header("Floating")]
    public bool enableFloating = true;
    public float floatHeight = 0.2f;
    public float floatSpeed = 2f;

    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;

    private void Start()
    {
        if (visualObject == null)
        {
            Debug.LogWarning(
                "WeaponPickupEffect: Chưa gán Visual Object!"
            );
            enabled = false;
            return;
        }

        initialLocalPosition = visualObject.localPosition;
        initialLocalRotation = visualObject.localRotation;
    }

    private void Update()
    {
        if (visualObject == null)
            return;

        // Xoay vũ khí quanh trục Y
        if (enableRotation)
        {
            visualObject.Rotate(
                Vector3.up,
                rotationSpeed * Time.deltaTime,
                Space.World
            );
        }

        // Vũ khí nhấp nhô lên xuống
        if (enableFloating)
        {
            float offset =
                Mathf.Sin(Time.time * floatSpeed)
                * floatHeight;

            visualObject.localPosition =
                initialLocalPosition +
                Vector3.up * offset;
        }
    }

    public void StopEffect()
    {
        enableRotation = false;
        enableFloating = false;

        if (visualObject != null)
        {
            visualObject.localPosition = initialLocalPosition;
            visualObject.localRotation = initialLocalRotation;
        }
    }
}
