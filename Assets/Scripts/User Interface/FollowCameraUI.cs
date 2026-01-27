using TMPro;
using UnityEngine;

public class FollowCameraUI : MonoBehaviour
{
    [SerializeField] Camera Camera;

    private void Start()
    {
        if (Camera == null) Debug.LogError("FollowCameraText: Missing camera");
    }

    private void Update()
    {
        Vector3 direction = transform.position - Camera.transform.position;

        // Check if vector is big enough to define a direction
        if (direction.sqrMagnitude > 0.001f)
        {
            transform.forward = direction;
        }
    }
}
