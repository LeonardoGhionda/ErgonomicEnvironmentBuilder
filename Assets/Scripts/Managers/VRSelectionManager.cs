using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSelectionManager : MonoBehaviour
{
    private XRGrabInteractable _selected;
    public bool SelectionExist => _selected != null;  
    public XRGrabInteractable Selected => _selected;


    private Material selectedMaterial;
    private Material baseMaterial;

    public Action<XRGrabInteractable> OnSelectionChanged;

    [SerializeField] private Camera VRCam;
    [SerializeField] Material selectedObjectMaterial;
    [SerializeField] ColliderVisual colliderVisual;

    private void Awake()
    {
        selectedMaterial = selectedObjectMaterial;
    }

    public void ChangeSelected(XRGrabInteractable selected)
    {
        if (AlreadySelected(selected)) return;

        ClearSelection();

        _selected = selected;


        if (_selected == null) return;

        baseMaterial = selected.gameObject.GetComponent<MeshRenderer>().material;
        selected.gameObject.GetComponent<MeshRenderer>().material = selectedMaterial;
        colliderVisual.ChangeTarget(_selected.GetComponent<BoxCollider>());

        OnSelectionChanged?.Invoke(_selected);
        
    }


    // Need a separate method to avoid null reference issues with XR Interactable events
    public void ClearSelection()
    {
        if (_selected)
        {
            _selected.gameObject.GetComponent<MeshRenderer>().material = baseMaterial;
            ReleaseCurrentlySelectedObject();
        }

        colliderVisual.ChangeTarget(null);

        baseMaterial = null;
        _selected = null;

        OnSelectionChanged?.Invoke(null);
    }

    public void DeleteSelected()
    {
        colliderVisual.ChangeTarget(null);

        if (_selected == null) return;

        Destroy(_selected.gameObject);
         
        // I have to procede this "ugly" way because when an object is grabbed XRI moves it outside of
        // its parenting chain and there is no way to retrive the original parent 
        Transform container = GameObject.Find("Objects Container").transform;
        foreach (Transform parent in container)
        {
            if (parent.childCount <= 0)
                Destroy(parent.gameObject);
        }


        ChangeSelected(null);
    }

    public void ReleaseCurrentlySelectedObject()
    {
        if (_selected != null)
        {
            var manager = GameObject.FindFirstObjectByType<XRInteractionManager>();
            IXRSelectInteractor interactor = _selected.firstInteractorSelecting;

            if (manager != null && interactor != null)
                manager.CancelInteractorSelection(interactor);

        }
    }

    public bool AlreadySelected(XRGrabInteractable grabbable) => grabbable == _selected;

}
