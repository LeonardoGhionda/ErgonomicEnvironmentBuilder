using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class ChangeGizmoModeDropdown : MonoBehaviour
{
    private TMP_Dropdown dropdown;

    private RuntimeGizmoTransform target;
    public RuntimeGizmoTransform Target
    {
        set
        {
            // disconnect old target
            if (target != null)
                target.OnModeChanged -= OnModeChangedHandler;

            target = value;

            // connect new target
            if (target != null)
            {
                target.OnModeChanged += OnModeChangedHandler;
                ApplyMode(target.GizmoMode);
            }
        }
    }

    private Vector3 lScale;

    void Awake()
    {
        lScale = transform.localScale;
        transform.localScale = Vector3.zero;

        dropdown = GetComponent<TMP_Dropdown>();
        dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    private void OnDropdownChanged(int index)
    {
        if (target == null) return;

        switch (index)
        {
            case 0:
                target.LocalTranform = true;
                break;
            case 1:
                target.LocalTranform = false;
                break;
        }
    }

    private void OnModeChangedHandler(GizmoMode gm)
    {
        ApplyMode(gm);
    }

    private void ApplyMode(GizmoMode gm)
    {
        if (gm == GizmoMode.None)
            transform.localScale = Vector3.zero;
        else
            transform.localScale = lScale;
    }
}
