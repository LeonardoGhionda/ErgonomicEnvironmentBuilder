using UnityEngine;

abstract public class Interactable : MonoBehaviour
{
    abstract public void OnSelect();
    abstract public void OnDeselect();
}
