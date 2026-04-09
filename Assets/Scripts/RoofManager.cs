using Unity.VisualScripting;
using UnityEngine;

public class RoofManager : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject != DependencyProvider.CurrentPlayer) return;

        gameObject.layer = LayerMask.NameToLayer("Default");
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Default");
    }

    private void OnTriggerExit(Collider other)
    { 
        if (other.gameObject != DependencyProvider.CurrentPlayer) return;

        gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
        transform.GetChild(0).gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");

    }

}
