using TMPro;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(TMP_Dropdown))]
public class ChangeGizmoModeDropdown : MonoBehaviour
{

    private TMP_Dropdown dropdown;

    private RuntimeGizmoTransform target;
    public RuntimeGizmoTransform Target
    { set { target = value; } }

    private Vector3 lScale;

    void Start()
    {
        //save scale to recover it later
        lScale = transform.localScale;
        //start hidden because default mode is None 
        transform.localScale = Vector3.zero;
        
        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(index =>
        {
            switch (index)
            {
                case 0://Local
                    target.LocalTranform = true;
                    break;
                case 1://Global
                    target.LocalTranform = false;
                    break;
            }
        });

        target.OnModeChanged += gm =>
        {
            //hide dropdown if mode is none
            if (gm == GizmoMode.None)
                transform.localScale = Vector3.zero;
            else
                transform.localScale = lScale;
        };
    }
}
