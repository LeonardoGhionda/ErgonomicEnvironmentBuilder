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
        transform.forward = transform.position - Camera.transform.position;
    }
}
