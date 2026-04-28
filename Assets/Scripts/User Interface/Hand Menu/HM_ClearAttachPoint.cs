using UnityEngine;

public class HM_ClearAttachPoint : HM_Base
{

    VRSelectionManager _selectionManager;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _selectionManager = Managers.Get<VRSelectionManager>();
    }

    public override void OnClick()
    {
        base.OnClick();
        if (_selectionManager.SelectionExist)
        {
            foreach (var item in _selectionManager.Selected.GetComponentsInChildren<AttachPoint>())
            {
                Destroy(item);
            }
        }
    }
}

