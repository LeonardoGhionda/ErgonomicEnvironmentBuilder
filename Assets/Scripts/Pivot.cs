using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class Pivot : MonoBehaviour
{
    [SerializeField] VRSelectionManager selectionManager;
    XRGrabInteractable _target;
    public bool PivotAtCenter { get; set; }

    private void Start()
    {
        selectionManager.OnSelectionChanged += ChangeTarget;
        PivotAtCenter = false;
    }

    private void LateUpdate()
    {
        if (_target == null) return;

        if (PivotAtCenter)
        {
            // Transform the local center of the collider into a world position
            transform.position = _target.transform.TransformPoint(_target.GetComponent<BoxCollider>().center);
        }
        else
        {
            transform.position = _target.transform.position;
        }

        // Scale
        float scale = _target.transform.lossyScale.MinComponent() / 2;
        scale = Mathf.Clamp(scale, 0f, .1f);
        transform.localScale = new(scale, scale, scale);
    }

    private void ChangeTarget(XRGrabInteractable interactable)
    {
        gameObject.SetActive(interactable != null);
        _target = interactable;
    }
}
