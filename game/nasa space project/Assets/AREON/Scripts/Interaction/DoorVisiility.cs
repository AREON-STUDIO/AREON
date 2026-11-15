
using UnityEngine;

public class DoorVisibility : MonoBehaviour
{
    private Renderer doorRenderer;

    private void Awake()
    {
        doorRenderer = GetComponent<Renderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorRenderer.enabled = false;
            Debug.Log("Player detected - Door hidden");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            doorRenderer.enabled = true;
            Debug.Log("Player exited - Door visible");
        }
    }
}
