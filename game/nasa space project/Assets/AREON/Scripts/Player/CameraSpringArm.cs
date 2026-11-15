
using UnityEngine;

namespace AREON.Player
{
    public class CameraSpringArm : MonoBehaviour
    {
        [Header("Camera")]
        [SerializeField] private Transform cameraTransform;

        [Header("Spring Arm")]
        [SerializeField] private float armLength = 6f;
        [SerializeField] private float sphereRadius = 0.25f;
        [SerializeField] private float collisionOffset = 0.15f;
        [SerializeField] private float minimumDistance = 0.5f;

        [Header("Camera Angle")]
        [SerializeField] private float cameraPitch = 10f;

        [Header("Smoothing")]
        [SerializeField] private float returnSmoothTime = 0.2f;

        [Header("Collision")]
        [SerializeField] private LayerMask collisionLayers;

        private float currentLength;
        private float lengthVelocity;

        private void Awake()
        {
            currentLength = armLength;
        }

        private void LateUpdate()
        {
            if (cameraTransform == null)
                return;

            Vector3 origin = transform.position;
            Vector3 direction = -transform.forward;

            float targetLength = armLength;

            if (Physics.SphereCast(
                origin,
                sphereRadius,
                direction,
                out RaycastHit hit,
                armLength,
                collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                targetLength = Mathf.Clamp(
                    hit.distance - collisionOffset,
                    minimumDistance,
                    armLength
                );
            }

            if (targetLength < currentLength)
            {
                // Move inward immediately to avoid clipping.
                currentLength = targetLength;
            }
            else
            {
                // Smoothly extend when the obstacle disappears.
                currentLength = Mathf.SmoothDamp(
                    currentLength,
                    targetLength,
                    ref lengthVelocity,
                    returnSmoothTime
                );
            }

            cameraTransform.localPosition =
                new Vector3(0f, 0f, -currentLength);

            cameraTransform.localRotation =
                Quaternion.Euler(cameraPitch, 0f, 0f);
        }
    }
}
