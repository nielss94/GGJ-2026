using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles player movement and rotation using the Input System and a camera-relative move direction.
/// </summary>
public class PlayerMovement : MonoBehaviour
{
    private const float MoveInputThreshold = 0.01f;

    [Header("References")]
    [SerializeField] private GameObject model;
    [SerializeField] private CinemachineCamera virtualCamera;
    [SerializeField] private InputActionAsset inputActions;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField][Range(0.01f, 1f)] private float rotationSpeed = 0.15f;
    [SerializeField][Range(0f, 0.5f)] private float inputDeadZone = 0.15f;

    private Rigidbody rb;
    private InputAction moveAction;
    private Vector2 moveInput;

    /// <summary>Raw move input (WASD / left stick).</summary>
    public Vector2 MoveDirection => moveInput;

    /// <summary>World-space move direction from input + camera. Updated in FixedUpdate. Zero when no input or no camera. Used e.g. by DashAbility when dashing towards movement input.</summary>
    public Vector3 WorldMoveDirection { get; private set; }

    /// <summary>Transform used for visual facing (rotated towards move/dash direction). Null if no model assigned. Use this to rotate the character to face a direction without rotating the root (e.g. dash).</summary>
    public Transform ModelTransform => model != null ? model.transform : null;

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
        // Only zero input when input is blocked (e.g. menus). When only movement is blocked (e.g. attack),
        // keep reading move input so dash can use it for direction (useMovementDirectionForDash).
        if (PlayerInputBlocker.IsInputBlocked)
        {
            moveInput = Vector2.zero;
            return;
        }
        if (moveAction != null)
        {
            Vector2 raw = moveAction.ReadValue<Vector2>();
            float mag = raw.magnitude;
            if (mag <= inputDeadZone)
            {
                moveInput = Vector2.zero;
            }
            else
            {
                float rescaled = (mag - inputDeadZone) / (1f - inputDeadZone);
                moveInput = raw.normalized * Mathf.Clamp01(rescaled);
            }
        }
    }

    private void FixedUpdate()
    {
        if (virtualCamera != null)
        {
            Vector3 cameraForward = virtualCamera.transform.forward;
            Vector3 cameraRight = virtualCamera.transform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();
            Vector3 moveDirection = cameraForward * moveInput.y + cameraRight * moveInput.x;
            WorldMoveDirection = moveDirection.sqrMagnitude > MoveInputThreshold * MoveInputThreshold ? moveDirection.normalized : Vector3.zero;
        }
        else
        {
            WorldMoveDirection = Vector3.zero;
        }

        if (rb == null || virtualCamera == null)
        {
            return;
        }

        // Skip applying movement when input or movement-only is blocked (e.g. dash, or light attack during swing).
        if (PlayerInputBlocker.IsInputBlocked || PlayerInputBlocker.IsMovementBlocked)
            return;

        if (WorldMoveDirection.sqrMagnitude >= 0.01f)
        {
            rb.MovePosition(transform.position + WorldMoveDirection * moveSpeed * Time.deltaTime);
        }

        if (WorldMoveDirection.sqrMagnitude > MoveInputThreshold && model != null)
        {
            model.transform.rotation = Quaternion.Slerp(
                model.transform.rotation,
                Quaternion.LookRotation(WorldMoveDirection),
                rotationSpeed);
        }
    }
}
