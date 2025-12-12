using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class TransformTypeButton : MonoBehaviour
{
    [SerializeField] private TransformMode _gizmoMode;
    public TransformMode Mode => _gizmoMode;

    private GizmoManager target;
    public GizmoManager Target
    {
        set
        {
            target = value;

            GetComponent<Button>().onClick.RemoveAllListeners();

            if (target == null)
            {
                gameObject.SetActive(false);
                return;
            }

            gameObject.SetActive(true);
            GetComponent<Button>().onClick.AddListener(() => { target.SetMode(_gizmoMode, target.transform); });
        }
        get { return target; }
    }
}
