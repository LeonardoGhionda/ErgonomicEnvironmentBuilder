using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public Camera cam;

    private InputAction selectAction;
    private InputAction deselectAction;
    private InputAction deleteAction;

    private Interactable selected;

    public static readonly string parentTag = "Parent";
    private int parentID;

    static private int m_cnt = 0;
    public static int Cnt {
        get { return m_cnt++; }
    }



    private void Start()
    {
        selectAction = InputSystem.actions["RoomBuilderControl/Select"];
        deselectAction = InputSystem.actions["Ui/Close"];
        deleteAction = InputSystem.actions["RoomBuilderControl/Delete"];

        parentID = -1;
    }

    void Update()
    {
        if (deleteAction.WasPressedThisFrame())
        {
            DeleteSelected();
            return;
        }

        // Deselect object
        if (deselectAction.WasPressedThisFrame() && selected != null)
        {
            ChangeSelectedObject(null);
            return;
        }

        // Select object
        if (selectAction.WasPressedThisFrame())
        {
            Vector2 rayStart =
                cam.orthographic ?
                //selection from current mouse position
                Mouse.current.position.ReadValue() :  
                //selection from screen center of the camera
                new(Screen.width * 0.5f, Screen.height * 0.5f);


            // Ray from center
            Ray ray = cam.ScreenPointToRay(rayStart);

            if (Physics.Raycast(ray, out var hit))
            {
                var interactable = hit.collider.GetComponentInParent<InteractableObject>();
                // Change selected object or deselect if clicked on empty space
                ChangeSelectedObject(interactable);
            }
        }

    }

    /// <summary>
    /// Change selected object and call appropriate methods
    /// </summary>
    /// <param name="obj">selected object. Null for deselection</param>
    public void ChangeSelectedObject(Interactable obj)
    {
        //call OnDeselect
        if (selected != null) selected.OnDeselect();
        //deselect all if nothing is selected
        if (obj == null)
        {
            selected = null;
            parentID = -1;
            return;
        }
        var parentObj = FindParentWithTag(obj.transform, parentTag).GetComponent<Interactable>();
        //select parent object
        if (selected == null || parentObj.GetInstanceID() != parentID)
        {
            selected = parentObj;
            parentID = parentObj.GetInstanceID();
        }
        //select child
        else
        {
            selected = obj;
        }
        selected.OnSelect();
    }

    /// <summary>
    /// Find the closest parent with the provided tag
    /// </summary>
    /// <param name="child"></param>
    /// <param name="tag"></param>
    /// <returns></returns>
    Transform FindParentWithTag(Transform child, string tag)
    {

        if(child.CompareTag(tag))
            return child;

        Transform current = child.parent;

        while (current != null)
        {
            if (current.CompareTag(tag))
            {
                return current;
            }

            current = current.parent;
        }

        return null; // not found
    }

    public void DeleteSelected()
    {
        if (selected != null)
        {
            var obj = selected.gameObject;
            //select parent
            var newSelection = obj.transform.GetComponentInParent<InteractableParent>();

            if (newSelection.gameObject.GetInstanceID() == obj.GetInstanceID())
                newSelection = null;

            if (newSelection != null)
            {
                //if parent don't have any children delete parent too
                int childrenN = newSelection.transform.GetComponentsInChildren<Interactable>().Count();
                if (childrenN <= 2) //the parent itself and the object just deleted
                {
                    Destroy(newSelection.gameObject);
                    newSelection = null;
                }
            }

            //to avoid wrong Box Collider in the parent
            obj.transform.SetParent(null, false);

            Destroy(obj);
 
            ChangeSelectedObject(newSelection);
        }
    }
}
