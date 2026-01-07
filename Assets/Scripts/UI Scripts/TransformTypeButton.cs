using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TransformTypeButton : MonoBehaviour
{
    [SerializeField] private TransformMode _gizmoMode;
    public TransformMode Mode => _gizmoMode;
}
