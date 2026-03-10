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

        /* 
         * I need to know if gravity is on to highlight the button. 
         * Problem is that if object is grabbed, gravity is set temporary off.
         * XRGrabInteractable keep tracks of the previous value of gravity trough a private field called "m_UsedGravity", 
         * but this field is not accessible from outside the class,
         * so I have to use Reflection to get the value of the private field "m_UsedGravity" from XRGrabInteractable class. 
         */

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
            Destroy(_target.GetComponent<SnapFollow>()); // Remove snap if present

            if (_target.TryGetComponent<XRGrabInteractable>(out var grab)) // Remove movement lock if present 
            {
                if (grab.trackPosition == false)
                {
                    grab.trackPosition = true;
                    grab.trackRotation = true;
                    grab.trackScale = true;
                }
            }

            base.OnClick();
            Rigidbody rb = _target.GetComponent<Rigidbody>();
            rb.useGravity = _state;
            rb.isKinematic = !_state;
        }
    }

    void ChangeTarget(VRSelectionManager.SelectionChangedArgs args)
    {
        _target = args.selection;

        _state = _target != null && (bool)_gravityFieldInfo.GetValue(_target);

        UpdateVisual();
    }
}
