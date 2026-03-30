using UnityEngine;

public class FollowTarget : MonoBehaviour
{
    [SerializeField] Transform target;

    // Update is called once per frame
    void LateUpdate()
    {
        if (target == null) return;
        transform.position = target.position;
    }
}
