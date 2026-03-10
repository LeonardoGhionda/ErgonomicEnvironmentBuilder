using UnityEngine;
using UnityEngine.InputSystem;

public class DTSelectionManager : MonoBehaviour
{
    Interactable _selected;

    [SerializeField] ColliderVisual _colliderVisual;

    #region getter
    public bool SelectionExist => _selected != null;
    public GameObject SelectionGO => _selected.gameObject;
    public Transform SelectionTransform => _selected.transform;
    #endregion

    #region external dependency
    private Camera _cam;
    #endregion

    #region lifecycle
    public void Init(Camera cam)
    {
        _cam = cam;
    }
    #endregion

    public bool Select()
    {
        //ortho and perspective cam have 2 different ray start position
        Vector2 rayStart = _cam.orthographic
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

        Ray ray = _cam.ScreenPointToRay(rayStart);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Get interactable hit or return
            if (!hit.collider.TryGetComponent(out Interactable interactable))
            {
                ChangeSelectedObject(null);
                return false;
            }

            //interactable is a InteractableObject
            //every InteractableObject has an InteractableParent
            Interactable parent = interactable.transform.GetComponentInParent<InteractableParent>();

            //no past selection -> select parent 
            if (_selected == null)
            {
                int childCount = parent.transform.childCount;
                if (childCount > 1) ChangeSelectedObject(parent);
                //if only 1 child skip parent
                else ChangeSelectedObject(interactable);
            }
            //if past selection is not null check if its parent
            else
            {
                //parent of the currently selected Interactable 
                Interactable currentParent = _selected.transform.GetComponentInParent<InteractableParent>();

                // parent is selected -> select child
                if (_selected == parent) ChangeSelectedObject(interactable);
                // sibling is selected -> select child 
                else if (currentParent == parent) ChangeSelectedObject(interactable);
                //fisrt time selecting this "family" -> parent selected
                else ChangeSelectedObject(parent);
            }
            //selection found
            return true;
        }
        //selecton not found
        return false;
    }

    /// <summary>
    /// <para>Delete the currently selected Interactable</para>
    /// <para>If it's a parent, delete all children too</para>
    /// <para>If it's the last children, delete the parent</para>
    /// </summary>
    public void DeleteSelected()
    {
        if (_selected == null) return;

        _colliderVisual.ClearTarget();

        _selected.OnDeselect();

        Interactable nextSelected = null;

        //delete selected and all children
        if (_selected is InteractableParent)
        {
            // get children
            InteractableObject[] children = _selected.transform.GetComponentsInChildren<InteractableObject>();
            // destroy children
            foreach (InteractableObject child in children) Destroy(child.gameObject);
            // destroy selected
            Destroy(_selected.gameObject);
        }
        else
        {
            InteractableParent parent = _selected.GetComponentInParent<InteractableParent>();
            //no more children -> Destroy
            if (parent.GetComponentsInChildren<InteractableObject>().Length == 1)
                Destroy(parent.gameObject);
            //now parent is selected 
            else
                nextSelected = parent;
            //destroy children
            Destroy(_selected.gameObject);
        }

        _selected = null;
        ChangeSelectedObject(nextSelected);
    }

    public void ChangeSelectedObject(Interactable next)
    {
        // Deseleziona precedente
        if (_selected != null)
        {
            _colliderVisual.ChangeTarget(null);
            _selected.OnDeselect();
        }
        // Call Setup
        if (next != null)
        {
            next.OnSelect();
            _colliderVisual.ChangeTarget(next.GetComponent<BoxCollider>());
        }
        _selected = next;
    }
}