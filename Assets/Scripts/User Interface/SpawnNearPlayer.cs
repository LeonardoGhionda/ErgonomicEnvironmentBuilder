using UnityEngine;

public class SpawnNearPlayer : MonoBehaviour
{

    private Camera Camera => DependencyProvider.CurrentCamera;

    private void OnEnable()
    {
        transform.position = Camera.transform.position + Camera.transform.forward * .5f + Vector3.down * 0.4f;
        transform.forward = Camera.transform.forward;
    }
}
