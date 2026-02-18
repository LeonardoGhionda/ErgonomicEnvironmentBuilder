using Unity.Netcode;
using UnityEngine;

public class CopyTransform : NetworkBehaviour
{
    public Transform target;

    void Update()
    {
        if (!IsOwner) return;
        if (target == null)
        {
            Debug.LogError("CopyTransform: Target is not assigned.");
            return;
        }

        transform.SetPositionAndRotation(target.position - new Vector3(0, target.position.y, 0), target.rotation);
    }
}
