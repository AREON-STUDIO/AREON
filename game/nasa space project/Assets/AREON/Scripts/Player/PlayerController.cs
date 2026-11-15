
using UnityEngine;
using UnityEngine.InputSystem;

namespace AREON.Player
{
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(PlayerInput))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] private float walkSpeed = 6f;
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float groundStickForce = -2f;

        [Header("Mouse Look")]
        [SerializeField] private Transform cameraPivot;
        [SerializeField] private float mouseSensitivity = 0.15f;
        [SerializeField] private float minPitch = -80f;
        [SerializeField] private float maxPitch = 80f;

        [Header("Input")]
        [SerializeField] private string moveActionPath = "Player/Move";

        private CharacterController controller;
        private PlayerInput playerInput;
        private InputAction moveAction;

        private float verticalVelocity;
        private float yaw;
        private float pitch;

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
            playerInput = GetComponent<PlayerInput>();

            if (cameraPivot == null)
            {
                Debug.LogError("CameraPivot is not assigned!", this);
                enabled = false;
                return;
            }

            moveAction = playerInput.actions.FindAction(
                moveActionPath, false
            );

            if (moveAction == null)
            {
                Debug.LogError("Player/Move action not found!", this);
                enabled = false;
            }

            yaw = transform.eulerAngles.y;

            pitch = cameraPivot.localEulerAngles.x;

            if (pitch > 180f)
                pitch -= 360f;

            cameraPivot.localRotation =
            Quaternion.Euler(pitch, 0f, 0f);
        }

        private void OnEnable()
        {
            moveAction?.Enable();

            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void OnDisable()
        {
            moveAction?.Disable();

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            HandleMouseLook();
            HandleMovement();
        }


        private void HandleMouseLook()
        {
            if (Mouse.current == null)
                return;

            if (Keyboard.current != null &&
                Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                return;
            }

            if (Mouse.current.leftButton.wasPressedThisFrame &&
                Cursor.lockState != CursorLockMode.Locked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            if (Cursor.lockState != CursorLockMode.Locked)
                return;

            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            // YAW: Mouse X rotates the player horizontally.
            yaw += mouseDelta.x * mouseSensitivity;

            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            // PITCH: Mouse Y rotates the camera vertically.
            pitch -= mouseDelta.y * mouseSensitivity;

            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            cameraPivot.localRotation =
                Quaternion.Euler(pitch, 0f, 0f);
        }


        private void HandleMovement()
        {
            if (moveAction == null)
                return;

            Vector2 input = moveAction.ReadValue<Vector2>();
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 direction =
                transform.forward * input.y +
                transform.right * input.x;

            if (controller.isGrounded && verticalVelocity < 0f)
                verticalVelocity = groundStickForce;

            verticalVelocity += gravity * Time.deltaTime;

            Vector3 velocity = direction * walkSpeed;
            velocity.y = verticalVelocity;

            controller.Move(velocity * Time.deltaTime);
        }
    }
}
