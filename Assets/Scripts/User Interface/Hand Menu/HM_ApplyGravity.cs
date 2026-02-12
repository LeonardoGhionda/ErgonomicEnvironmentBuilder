using System.Reflection;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_ApplyGravity : HM_Toggle
{
    private XRGrabInteractable _target;
    private VRSelectionManager _sm;
    private static FieldInfo _gravityFieldInfo;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = _deps.selection;
        _sm.OnSelectionChanged += ChangeTarget;


        // Get private field "m_UsedGravity" trough Reflection 
        _gravityFieldInfo = typeof(XRGrabInteractable).GetField("m_UsedGravity", BindingFlags.NonPublic | BindingFlags.Instance);

        if (_gravityFieldInfo == null)
        { 
            // Something wrong happened
            Debug.LogError($"[ScaleManager] CRITICAL ERROR: 'm_UsedGravity' field do not exists anymore in XRGrabInteractable class,\n" +
                           "This behaviour can be caused ny an update of the XR Toolkit library.\n" +
                           "Need to update the varible name in this line: FieldInfo field = typeof(XRGrabInteractable).GetField(\"m_UsedGravity\", BindingFlags.NonPublic | BindingFlags.Instance);");

            // Fallback
            _state = false;
        }
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        if (_target != null)
        {
            base.OnClick();
            var rb = _target.GetComponent<Rigidbody>();
            rb.useGravity = !rb.useGravity;
            rb.isKinematic = !rb.isKinematic;
        }  
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    { 
        _target = args.selection;
        if (_target == null) return;

        _state = _target != null && (bool)_gravityFieldInfo.GetValue(_target);

        UpdateVisual();
    }
}
