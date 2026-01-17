using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float jumpHeight = 1.5f;
    [SerializeField] private float rotationSpeed = 720f;

    [Header("Gravity")]
    [SerializeField] private float gravity = -15f;
    [SerializeField] private float groundedGravity = -2f;

    [Header("Camera")]
    [Tooltip("Camera to use for relative movement. If empty, uses Camera.main")]
    [SerializeField] private Transform cameraTransform;

    private CharacterController controller;
    private InputSystem_Actions input;
    private Vector3 velocity;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        input = new InputSystem_Actions();

        if (cameraTransform == null && Camera.main != null)
        {
            cameraTransform = Camera.main.transform;
        }
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
        if (moveInput == Vector2.zero) return;

        // Get camera's forward and right vectors, flattened to ground plane
        Vector3 camForward = cameraTransform.forward;
        Vector3 camRight = cameraTransform.right;
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Calculate movement relative to camera
        Vector3 move = camForward * moveInput.y + camRight * moveInput.x;
        controller.Move(move * moveSpeed * Time.deltaTime);

        // Rotate player to face movement direction
        Quaternion targetRotation = Quaternion.LookRotation(move);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
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
