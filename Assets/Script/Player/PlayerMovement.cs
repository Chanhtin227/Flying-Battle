using UnityEngine;
using Fusion;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public bool isRunning;
    public bool isJumping;
}

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour 
{
    public static PlayerMovement LocalPlayer;

    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 2f;

    public Transform cameraRoot;

    CharacterController cc;
    PlayerAnimation playerAnim;
    Vector3 velocity;

    [Networked] private Vector3 NetworkedPosition { get; set; }
    [Networked] private Quaternion NetworkedRotation { get; set; }
    [Networked] private NetworkBool HasNetworkedTransform { get; set; }
    
    // [THÊM MỚI] Biến đồng bộ tốc độ di chuyển để chạy Animation cho các máy khác (Proxy)
    [Networked] private float NetworkedAnimSpeed { get; set; }

    public override void Spawned()
    {
        cc = GetComponent<CharacterController>();
        playerAnim = GetComponent<PlayerAnimation>();

        if (HasInputAuthority)
        {
            LocalPlayer = this;
        }

        // [THÊM MỚI - SỬA LỖI LƠ LỬNG] 
        // Tắt CharacterController trên bản sao (Proxy) của người chơi khác.
        // Điều này cho phép transform.position được gán tự do trong hàm Render() mà không bị chặn.
        if (Object.IsProxy)
        {
            cc.enabled = false;
        }

        if (HasStateAuthority)
        {
            SyncNetworkedTransform();
        }
    }

    public NetworkInputData GetLocalInput()
    {
        NetworkInputData data = new NetworkInputData();

        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        if (cameraRoot != null)
        {
            Vector3 forward = cameraRoot.forward;
            Vector3 right = cameraRoot.right;

            forward.y = 0; right.y = 0;
            forward.Normalize(); right.Normalize();

            data.moveDirection = forward * v + right * h;
        }

        data.isRunning = Input.GetKey(KeyCode.LeftShift);
        data.isJumping = Input.GetKey(KeyCode.Space);
        return data;
    }

    public override void FixedUpdateNetwork()
    {
        if (GetInput(out NetworkInputData data))
        {
            float speed = data.isRunning ? runSpeed : walkSpeed;

            cc.Move(data.moveDirection * speed * Runner.DeltaTime);

            if (data.moveDirection.magnitude > 0.1f)
                transform.forward = data.moveDirection;

            // Tính toán tốc độ animation hiện tại
            float currentAnimSpeed = data.moveDirection.magnitude * (data.isRunning ? 1f : 0.5f);
            
            if (playerAnim != null)
                playerAnim.Move(currentAnimSpeed);

            if (cc.isGrounded)
            {
                velocity.y = -2;

                if (data.isJumping)
                {
                    velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                    if (playerAnim != null) playerAnim.Jump();
                }
            }
            velocity.y += gravity * Runner.DeltaTime;
            cc.Move(velocity * Runner.DeltaTime);

            if (HasStateAuthority)
            {
                SyncNetworkedTransform();
                // [THÊM MỚI] Lưu lại tốc độ Animation vào biến Networked để gửi cho Client
                NetworkedAnimSpeed = currentAnimSpeed;
            }
        }
    }

    public override void Render()
    {
        // [THÊM MỚI - SỬA LỖI KHÔNG CHẠY ANIMATION]
        // Nếu đây là nhân vật của người khác (Proxy), ép nó chạy Animation dựa trên dữ liệu mạng nhận được
        if (Object.IsProxy && playerAnim != null)
        {
            playerAnim.Move(NetworkedAnimSpeed);
        }

        if (HasStateAuthority || HasInputAuthority || !HasNetworkedTransform)
            return;

        const float lerpSpeed = 15f;

        if (Vector3.Distance(transform.position, NetworkedPosition) > 2f)
        {
            transform.SetPositionAndRotation(NetworkedPosition, NetworkedRotation);
            return;
        }

        transform.position = Vector3.Lerp(
            transform.position,
            NetworkedPosition,
            Time.deltaTime * lerpSpeed
        );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            NetworkedRotation,
            Time.deltaTime * lerpSpeed
        );
    }

    private void SyncNetworkedTransform()
    {
        NetworkedPosition = transform.position;
        NetworkedRotation = transform.rotation;
        HasNetworkedTransform = true;
    }
}