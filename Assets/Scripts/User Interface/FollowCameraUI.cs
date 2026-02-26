using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    [SerializeField] private Camera Camera;
    [SerializeField] private bool onlyY;

    private void Start()
    {
        if (Camera == null)
        {
            Debug.LogError("FollowCameraUI: Missing camera reference.");
        }
    }

    private void Update()
    {
        if (Camera == null) return;

        Vector3 direction = transform.position - Camera.transform.position;

        if (onlyY)
        {
            // Zero out the vertical difference to rotate only around the Y axis
            direction.y = 0;
        }

        // Check if vector is big enough to define a direction to avoid errors
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.forward = direction;
        }
    }
}