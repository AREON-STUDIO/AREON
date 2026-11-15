using UnityEngine;

namespace AREON.Player
{
    /// <summary>
    /// Simple third-person follow camera. Keeps a fixed world-space offset
    /// from the target and looks at the target. No Cinemachine required.
    /// </summary>
    public class FollowCamera : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("The transform the camera follows (usually the player).")]
        [SerializeField] private Transform target;

        [Tooltip("World-space offset from the target to the camera position.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 5f, 7f);

        [Tooltip("Point above the target that the camera looks at.")]
        [SerializeField] private Vector3 lookOffset = new Vector3(0f, 1.5f, 0f);

        [Header("Smoothing")]
        [Tooltip("Time in seconds the camera takes to catch up with the target.")]
        [SerializeField] private float followSmoothTime = 0.15f;

        private Vector3 currentVelocity;

        private void Awake()
        {
            if (target == null)
            {
                Debug.LogError("[FollowCamera] No target assigned on " + name
                    + ". Assign the player transform in the Inspector.", this);
            }
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 desiredPosition = target.TransformPoint(offset);
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref currentVelocity, followSmoothTime);
            transform.LookAt(target.position + lookOffset);
        }

        private void OnValidate()
        {
            followSmoothTime = Mathf.Max(0.001f, followSmoothTime);
        }
    }
}