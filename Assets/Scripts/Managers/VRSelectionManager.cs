using System;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

public class VRSelectionManager : MonoBehaviour
{
    // GETTERS
    public bool SelectionExist => _selected != null;
    public XRGrabInteractable Selected => _selected;

    // PRIVATE
    private XRGrabInteractable _selected;
    private Vector3 _contactPoint;
    private Material _selectedMaterial;
    private Material _baseMaterial;

    public class SelectionChangedArgs
    {
        public XRGrabInteractable selection;
        public Vector3 contactPoint;

        public SelectionChangedArgs(XRGrabInteractable selection = null, Vector3 contactPoint = default)
        {
            this.selection = selection;
            this.contactPoint = contactPoint;
        }

    }

    // ACTIONS
    public Action<SelectionChangedArgs> OnSelectionChanged;
    public Action<InteractableParent> OnGroupDeleted;

    // SERIALIZED
    [SerializeField] private Camera VRCam;
    [SerializeField] Material selectedObjectMaterial;
    [SerializeField] ColliderVisual colliderVisual;

    private void Awake()
    {
        _selectedMaterial = selectedObjectMaterial;
    }


    public void ChangeSelected(SelectEnterEventArgs args)
    {
        XRGrabInteractable selected = args.interactableObject as XRGrabInteractable;

        if (AlreadySelected(selected)) return;

        // Unselect currently selected true -> avoid to notify twice 
        ClearSelection(true);

        // Change selection
        _selected = selected;

        // Set up new selected 
        if (_selected != null) 
        {
            _baseMaterial = selected.gameObject.GetComponent<MeshRenderer>().material;
            selected.gameObject.GetComponent<MeshRenderer>().material = _selectedMaterial;
            colliderVisual.ChangeTarget(_selected.GetComponent<BoxCollider>());

            // Get point where selection occurred
            if (args.interactorObject is XRRayInteractor rayInteractor &&
                rayInteractor.TryGetHitInfo(out Vector3 hitPosition, out _, out _, out _))
            {
                _contactPoint = hitPosition;
            }
            else
            {
                // Fallback for Poke and Direct: Use the hand/finger's attach position
                // This is where the 'finger tip' or 'hand center' is located
                _contactPoint = args.interactorObject.GetAttachTransform(args.interactableObject).position;
            }
        }

        // Notify of the change 
        OnSelectionChanged?.Invoke(new(_selected, _contactPoint));
    }


    // Need a separate method to avoid null reference issues with XR Interactable events
    public void ClearSelection(bool skipCallback = false)
    {
        if (_selected)
        {
            _selected.gameObject.GetComponent<MeshRenderer>().material = _baseMaterial;
            ReleaseCurrentlySelectedObject();
        }

        colliderVisual.ChangeTarget(null);

        _baseMaterial = null;
        _selected = null;

        if (skipCallback) return;

        OnSelectionChanged?.Invoke(new());
    }

    public void DeleteSelected()
    {
        if (_selected == null) return;

        colliderVisual.ChangeTarget(null);

        Destroy(_selected.gameObject);
         
        // I have to procede this "ugly" way because when an object is grabbed XRI moves it outside of
        // its parenting chain and there is no way to retrive the original parent 
        Transform container = GameObject.Find("Objects Container").transform;
        foreach (Transform parent in container)
        {
            if (parent.childCount <= 0 || (parent.childCount == 1 && parent.GetChild(0) == _selected.transform)) // Destroy is applied at the end of the frame 
            {
                OnGroupDeleted.Invoke(parent.GetComponent<InteractableParent>());
                Destroy(parent.gameObject);
            }
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
