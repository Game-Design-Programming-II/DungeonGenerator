using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace DungeonGenerator.Character
{
    /// <summary>
    /// Handles top-down character locomotion and action events using Unity's Input System.
    /// Attach alongside a Rigidbody2D and PlayerInput configured for Invoke Unity Events.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CapsuleCollider2D))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerCharacterController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float moveSpeed = 10f;

        [Header("Dash")]
        [SerializeField] private float dashDistance = 7f;
        [SerializeField] private float dashDuration = 0.2f;
        [SerializeField] private float dashCooldown = 0.75f;

        [Header("View")]
        [SerializeField, Tooltip("Optional camera transform used to convert movement input into world space.")]
        private Transform cameraTransform;

        [Header("Rendering")]
        [SerializeField] private SpriteRenderer spriteRenderer;
        [SerializeField, Tooltip("Sorting layer name to use. Leave empty to keep existing layer.")]
        private string sortingLayerName = string.Empty;
        [SerializeField, Tooltip("Sorting order forcing the player to render above tiles.")]
        private int sortingOrder = 100;

        [Header("Action Events")]
        [SerializeField] private UnityEvent attackPerformed = new UnityEvent();
        [SerializeField] private UnityEvent interactPerformed = new UnityEvent();
        [SerializeField] private UnityEvent dashPerformed = new UnityEvent();

        private Rigidbody2D body;
        private PlayerInput playerInput;
        private Animator animator;

        private InputAction moveAction;
        private InputAction attackAction;
        private InputAction interactAction;
        private InputAction dashAction;

        private Vector2 moveInput;
        private Vector2 lastMoveDirection = Vector2.up;
        private bool isDashing;
        private Vector2 dashDirection;
        private float dashEndTime;
        private float nextDashReadyTime;

        private Transform CameraTransform
        {
            get
            {
                if (cameraTransform == null && Camera.main != null)
                {
                    cameraTransform = Camera.main.transform;
                }

                return cameraTransform;
            }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.freezeRotation = true;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            playerInput = GetComponent<PlayerInput>();
            animator = GetComponent<Animator>();
            ConfigureSpriteRenderer();
        }

        private void OnEnable()
        {
            if (playerInput == null)
            {
                enabled = false;
                return;
            }

            moveAction = TryGetAction("Move");
            attackAction = TryGetAction("Attack");
            interactAction = TryGetAction("Interact");
            dashAction = TryGetAction("Dash");

            if (moveAction != null)
            {
                moveAction.performed += OnMove;
                moveAction.canceled += OnMove;
            }

            if (attackAction != null)
            {
                attackAction.performed += OnAttackPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed += OnInteractPerformed;
            }

            if (dashAction != null)
            {
                dashAction.performed += OnDashPerformed;
            }
        }

        private void OnDisable()
        {
            if (moveAction != null)
            {
                moveAction.performed -= OnMove;
                moveAction.canceled -= OnMove;
            }

            if (attackAction != null)
            {
                attackAction.performed -= OnAttackPerformed;
            }

            if (interactAction != null)
            {
                interactAction.performed -= OnInteractPerformed;
            }

            if (dashAction != null)
            {
                dashAction.performed -= OnDashPerformed;
            }
        }

        private void FixedUpdate()
        {
            float deltaTime = Time.fixedDeltaTime;

            if (isDashing)
            {
                PerformDash(deltaTime);
            }
            else
            {
                HandleMovement(deltaTime);
            }
        }

        private void HandleMovement(float deltaTime)
        {
            Vector2 desiredDirection = Vector2.zero;

            if (moveInput.sqrMagnitude > 0.001f)
            {
                desiredDirection = TransformInputToWorld(moveInput).normalized;
                lastMoveDirection = desiredDirection;
            }

            Vector2 velocity = desiredDirection * moveSpeed;
            body.linearVelocity = velocity;

            if (velocity.sqrMagnitude <= 0.0001f && !isDashing)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void PerformDash(float deltaTime)
        {
            float dashSpeed = dashDistance / Mathf.Max(0.001f, dashDuration);
            Vector2 dashVelocity = dashDirection * dashSpeed;
            body.linearVelocity = dashVelocity;

            if (Time.time >= dashEndTime)
            {
                isDashing = false;
                body.linearVelocity = Vector2.zero;
            }
        }

        private Vector2 TransformInputToWorld(Vector2 input)
        {
            Transform currentCamera = CameraTransform;

            if (currentCamera == null)
            {
                return input;
            }

            Vector3 right3 = currentCamera.right;
            Vector3 up3 = currentCamera.up;

            Vector2 right = new Vector2(right3.x, right3.y).normalized;
            Vector2 up = new Vector2(up3.x, up3.y).normalized;

            Vector2 world = right * input.x + up * input.y;
            if (world.sqrMagnitude <= 0.0001f)
            {
                return input;
            }

            return world;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            moveInput = context.ReadValue<Vector2>();
            animator.SetFloat("X", moveInput.x);
            animator.SetFloat("Y", moveInput.y);
        }

        private void OnAttackPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                attackPerformed.Invoke();
            }
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                interactPerformed.Invoke();
            }
        }

        private void OnDashPerformed(InputAction.CallbackContext context)
        {
            if (!context.performed || isDashing || Time.time < nextDashReadyTime)
            {
                return;
            }

            Vector2 mouseDirection = GetMouseDirection();
            if (mouseDirection.sqrMagnitude < 0.001f)
            {
                return;
            }

            dashDirection = mouseDirection.normalized;
            dashEndTime = Time.time + dashDuration;
            nextDashReadyTime = Time.time + dashCooldown;
            lastMoveDirection = dashDirection;
            isDashing = true;
            dashPerformed.Invoke();
        }

        private InputAction TryGetAction(string actionName)
        {
            return playerInput.actions?.FindAction(actionName, throwIfNotFound: false);
        }

        private Vector2 GetMouseDirection()
        {
            if (CameraTransform == null)
            {
                return lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : Vector2.up;
            }

            Vector3 mouseScreen = Mouse.current != null ? Mouse.current.position.ReadValue() : (Vector3)Input.mousePosition;
            Vector3 worldPoint = CameraTransform.GetComponent<Camera>() != null
                ? CameraTransform.GetComponent<Camera>().ScreenToWorldPoint(mouseScreen)
                : Camera.main != null
                    ? Camera.main.ScreenToWorldPoint(mouseScreen)
                    : mouseScreen;

            Vector2 direction = new Vector2(worldPoint.x - transform.position.x, worldPoint.y - transform.position.y);
            if (direction.sqrMagnitude < 0.001f)
            {
                return lastMoveDirection.sqrMagnitude > 0.001f ? lastMoveDirection : Vector2.up;
            }

            return direction;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            moveSpeed = Mathf.Max(0f, moveSpeed);
            dashDistance = Mathf.Max(0f, dashDistance);
            dashDuration = Mathf.Max(0.05f, dashDuration);
            dashCooldown = Mathf.Max(0f, dashCooldown);
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }
            ConfigureSpriteRenderer();
        }
#endif

        private void ConfigureSpriteRenderer()
        {
            if (spriteRenderer == null)
            {
                spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            }

            if (spriteRenderer == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(sortingLayerName))
            {
                foreach (SortingLayer layer in SortingLayer.layers)
                {
                    if (layer.name == sortingLayerName)
                    {
                        spriteRenderer.sortingLayerName = sortingLayerName;
                        break;
                    }
                }
            }

            spriteRenderer.sortingOrder = sortingOrder;
        }
    }
}
