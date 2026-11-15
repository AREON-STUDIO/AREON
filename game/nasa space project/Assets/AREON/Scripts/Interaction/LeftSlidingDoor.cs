
using UnityEngine;

public class LeftSlidingDoor : MonoBehaviour
{
    [Header("Door Panel")]
    [SerializeField] private Transform doorPanel;

    [Header("Movement")]
    [SerializeField]
    private Vector3 openOffset =
        new Vector3(-1.5f, 0f, 0f);

    [SerializeField] private float openSpeed = 2f;

    private Vector3 closedPosition;
    private Vector3 openPosition;

    private bool isOpen;

    private void Start()
    {
        closedPosition = doorPanel.localPosition;
        openPosition = closedPosition + openOffset;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            isOpen = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            isOpen = false;
    }

    private void Update()
    {
        Vector3 targetPosition =
            isOpen ? openPosition : closedPosition;

        doorPanel.localPosition = Vector3.Lerp(
            doorPanel.localPosition,
            targetPosition,
            Time.deltaTime * openSpeed
        );
    }
}
