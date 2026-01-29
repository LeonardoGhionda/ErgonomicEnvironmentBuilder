using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_ApplyGravity : HM_Toggle
{
    VRSelectionManager _sm;
    XRGrabInteractable _target;
    TextMeshProUGUI _cardText;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _sm = _deps.selection;
        _sm.OnSelectionChanged += ChangeTarget;
        
        _cardText = GetComponentInChildren<TextMeshProUGUI>();
        if (_cardText == null) Debug.LogError("Missing Text Component");
    }

    // Override single choices made previously 
    override public void OnClick()
    {
        base.OnClick();

        if (_target != null)
        {
            var rb = _target.GetComponent<Rigidbody>();
            rb.useGravity = !rb.useGravity;
            rb.isKinematic = !rb.isKinematic;
        }
        else
        {
            Rigidbody[] rbs = GameObject
                           .FindObjectsByType<XRGrabInteractable>(FindObjectsSortMode.None)
                           .Select(x => x.GetComponent<Rigidbody>())
                           .NotNull()
                           .ToArray();

            foreach (var rb in rbs)
            {
                rb.useGravity = _state;
                rb.isKinematic = !_state;
            }
        }
           
    }

    void ChangeTarget(XRGrabInteractable selected)
    {
        _target = selected;
        _cardText.text = _target == null ? "Change\nGravity\n" : "Change\nSelected\nGravity";
        _state = _target == null ? false: _target.GetComponent<Rigidbody>().useGravity;
    }
}
