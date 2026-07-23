using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    public float walkSpeed = 2f;
    public float runSpeed = 5f;
    public float gravity = -20f;
    public float jumpHeight = 2f;

    public Transform cameraRoot;

    CharacterController cc;
    PlayerAnimation playerAnim;

    Vector3 velocity;

    void Start()
    {
        cc = GetComponent<CharacterController>();
        playerAnim = GetComponent<PlayerAnimation>();
    }

    void Update()
    {
        Move();
    }

    void Move()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 forward = cameraRoot.forward;
        Vector3 right = cameraRoot.right;

        forward.y = 0;
        right.y = 0;

        forward.Normalize();
        right.Normalize();

        Vector3 move = forward * v + right * h;

        bool running = Input.GetKey(KeyCode.LeftShift);

        float speed = running ? runSpeed : walkSpeed;

        cc.Move(move * speed * Time.deltaTime);

        if (move.magnitude > 0.1f)
            transform.forward = forward;

        playerAnim.Move(move.magnitude * (running ? 1f : 0.5f));

        if (cc.isGrounded)
        {
            velocity.y = -2;

            if (Input.GetKeyDown(KeyCode.Space))
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2 * gravity);
                playerAnim.Jump();
            }
        }

        velocity.y += gravity * Time.deltaTime;
        cc.Move(velocity * Time.deltaTime);
    }
}