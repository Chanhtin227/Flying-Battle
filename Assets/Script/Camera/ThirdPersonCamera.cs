using UnityEngine;

public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;

    public float mouseSensitivity = 150f;

    public Vector3 shoulderOffset = new Vector3(0.6f, 1.6f, 0f);

    public float distance = 4f;

    public float minPitch = -30f;
    public float maxPitch = 60f;

    float yaw;
    float pitch;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    void LateUpdate()
    {
        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // Điểm đặt camera (vai phải)
        Vector3 targetPos = target.position + rotation * shoulderOffset;

        // Camera lùi ra sau
        Vector3 cameraPos = targetPos - rotation * Vector3.forward * distance;

        Camera.main.transform.position = cameraPos;
        Camera.main.transform.rotation = rotation;

        // Object này đi theo nhân vật
        transform.position = target.position;
        transform.rotation = rotation;
    }
}