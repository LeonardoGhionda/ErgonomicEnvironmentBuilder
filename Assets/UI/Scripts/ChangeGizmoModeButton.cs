using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class ChangeGizmoModeButton : MonoBehaviour
{
    private RuntimeGizmoTransform target;
    public RuntimeGizmoTransform Target
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
            GetComponent<Button>().onClick.AddListener(() => { target.SetMode(gizmoMode); });
        }
        get { return target; }
    }

    [SerializeField] private GizmoMode gizmoMode;

}
