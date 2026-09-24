using Fusion;
using UnityEngine;

public struct NetworkInputData : INetworkInput
{
    public Vector3 moveDirection;
    public Vector3 lookDirection;
    public NetworkBool isRunning;
    public NetworkBool isJumping;
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

    private CharacterController characterController;
    private PlayerAnimation playerAnim;
    private Vector3 velocity;

    // Position and rotation are replicated by NetworkTransform on the Player prefab.
    [Networked] private float NetworkedAnimSpeed { get; set; }

    public override void Spawned()
    {
        characterController = GetComponent<CharacterController>();
        playerAnim = GetComponent<PlayerAnimation>();

        if (HasInputAuthority)
            LocalPlayer = this;

        // Only the state authority may simulate the CharacterController. Every
        // client replica (including its locally controlled player) is driven by
        // NetworkTransform, so it cannot accumulate a second local simulation.
        if (!HasStateAuthority)
            characterController.enabled = false;
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (LocalPlayer == this)
            LocalPlayer = null;
    }

    public NetworkInputData GetLocalInput()
    {
        var data = new NetworkInputData();
        float horizontal = Input.GetAxisRaw("Horizontal");
        float vertical = Input.GetAxisRaw("Vertical");

        if (cameraRoot != null)
        {
            Vector3 forward = cameraRoot.forward;
            Vector3 right = cameraRoot.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            data.moveDirection = Vector3.ClampMagnitude(forward * vertical + right * horizontal, 1f);
            data.lookDirection = forward;
        }

        data.isRunning = Input.GetKey(KeyCode.LeftShift);
        data.isJumping = Input.GetKey(KeyCode.Space);
        return data;
    }

    public override void FixedUpdateNetwork()
    {
        // The host is the single simulation authority. Simulating the same
        // CharacterController on the input-authority client creates a second,
        // unsynchronised controller state and makes client movement drift or run
        // faster than the host.
        if (!HasStateAuthority)
            return;

        if (!GetInput(out NetworkInputData data))
            return;

        if (data.lookDirection.sqrMagnitude > 0.001f)
            transform.rotation = Quaternion.LookRotation(data.lookDirection, Vector3.up);

        float speed = data.isRunning ? runSpeed : walkSpeed;
        characterController.Move(data.moveDirection * speed * Runner.DeltaTime);

        float animationSpeed = data.moveDirection.magnitude * (data.isRunning ? 1f : 0.5f);
        if (playerAnim != null)
            playerAnim.Move(animationSpeed);

        if (characterController.isGrounded)
        {
            velocity.y = -2f;
            if (data.isJumping)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                if (playerAnim != null)
                    playerAnim.Jump();
            }
        }

        velocity.y += gravity * Runner.DeltaTime;
        characterController.Move(velocity * Runner.DeltaTime);

        NetworkedAnimSpeed = animationSpeed;
    }

    public override void Render()
    {
        if (!HasStateAuthority && playerAnim != null)
            playerAnim.Move(NetworkedAnimSpeed);
    }
}
