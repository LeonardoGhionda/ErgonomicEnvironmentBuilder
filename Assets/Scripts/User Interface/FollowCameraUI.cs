using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    private Camera _camera;
    [SerializeField] private bool onlyY;

    private void Start()
    {
        _camera = DependencyProvider.CurrentCamera;
    }

    private void Update()
    {
        Vector3 direction = transform.position - _camera.transform.position;

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