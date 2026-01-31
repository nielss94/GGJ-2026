using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement and rotation using the Input System and a camera-relative move direction.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private const float MoveInputThreshold = 0.1f;

    [Header("References")]
    [SerializeField] private GameObject model;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField][Range(0.01f, 1f)] private float rotationSpeed = 0.15f;

    private Rigidbody rb;
    private InputAction moveAction;
    private Vector2 moveInput;

    public Vector2 MoveDirection => moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (inputActions != null)
        {
            moveAction = inputActions.FindActionMap("Player").FindAction("Move");
        }
    }

    private void OnEnable()
    {
        moveAction?.Enable();
    }

    private void OnDisable()
    {
        moveAction?.Disable();
    }

    private void Update()
    {
        if (PlayerInputBlocker.IsInputBlocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        if (moveAction != null)
        {
            moveInput = moveAction.ReadValue<Vector2>();
        }
    }

    private void FixedUpdate()
    {
        if (rb == null || virtualCamera == null)
        {
            return;
        }

        Vector3 cameraForward = virtualCamera.transform.forward;
        Vector3 cameraRight = virtualCamera.transform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
        rb.MovePosition(transform.position + moveDirection * moveSpeed * Time.deltaTime);

        if (moveInput.sqrMagnitude > MoveInputThreshold * MoveInputThreshold && model != null)
        {
            model.transform.rotation = Quaternion.Slerp(
                model.transform.rotation,
                Quaternion.LookRotation(moveDirection),
                rotationSpeed);
        }
    }
}
