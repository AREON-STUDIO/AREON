using UnityEngine;

namespace AREON.UI
{
    /// <summary>
    /// Turns a world-space text object (TextMeshPro 3D) to face the camera so the
    /// temporary facility labels stay readable. Display only - no gameplay logic.
    /// </summary>
    public class FacilityWorldLabel : MonoBehaviour
    {
        [Tooltip("Camera transform to face. Assign the gameplay camera (the FollowCamera's camera).")]
        [SerializeField] private Transform cameraTransform;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Debug.LogWarning($"[FacilityWorldLabel] Camera not assigned on '{name}'. Label will not face the camera.", this);
            }
        }

        private void LateUpdate()
        {
            if (cameraTransform == null) return;
            transform.rotation = cameraTransform.rotation;
        }
    }
}
