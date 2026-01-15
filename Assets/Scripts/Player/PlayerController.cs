using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundedGravity = -2f;

    private CharacterController controller;
    private InputSystem_Actions input;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new InputSystem_Actions();
    }

    private void OnEnable()
    {
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();
    }

    private void OnDestroy()
    {
        input.Dispose();
    }

    private void Update()
    {
        HandleMovement();
        HandleGravityAndJump();
    }

    private void HandleMovement()
    {
        Vector2 moveInput = input.Player.Move.ReadValue<Vector2>();

        // Convert 2D input to 3D movement (top-down: X = left/right, Z = forward/back)
        Vector3 move = new Vector3(moveInput.x, 0f, moveInput.y);
        controller.Move(move * moveSpeed * Time.deltaTime);
    }

    private void HandleGravityAndJump()
    {
        bool grounded = controller.isGrounded;

        if (grounded)
        {
            velocity.y = groundedGravity;
        }
        else
        {
            velocity.y += gravity * Time.deltaTime;
        }

        // Apply gravity first, then check jump
        controller.Move(velocity * Time.deltaTime);

        // Jump check after gravity move (so isGrounded is fresh)
        if (input.Player.Jump.WasPressedThisFrame())
        {
            Debug.Log($"Jump pressed! Grounded: {controller.isGrounded}");
            if (controller.isGrounded)
            {
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
                controller.Move(velocity * Time.deltaTime);
            }
        }
    }
}
