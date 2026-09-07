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

    [Header("References")]
    public Camera cam; 

    void Start()
    {
        // Tự động tìm Camera con trong Prefab nếu bạn quên kéo thả
        if (cam == null)
        {
            cam = GetComponentInChildren<Camera>();
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Khởi tạo góc nhìn ban đầu
        yaw = transform.eulerAngles.y;
        pitch = transform.eulerAngles.x;
        
        if (pitch > 180f)
            pitch -= 360f;
    }

    void LateUpdate()
    {
        // QUAN TRỌNG: Dừng chạy nếu chưa có mục tiêu hoặc chưa có camera
        if (target == null || cam == null) 
            return;

        yaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        pitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

        Quaternion rotation = Quaternion.Euler(pitch, yaw, 0);

        // Điểm đặt camera (vai phải)
        Vector3 targetPos = target.position + rotation * shoulderOffset;

        // Camera lùi ra sau
        Vector3 cameraPos = targetPos - rotation * Vector3.forward * distance;

        // SỬ DỤNG BIẾN 'cam' THAY VÌ 'Camera.main' ĐỂ KHÔNG BỊ LỖI MULTIPLAYER
        cam.transform.position = cameraPos;
        cam.transform.rotation = rotation;

        // Object này đi theo nhân vật
        transform.position = target.position;
        transform.rotation = rotation;
    }
}