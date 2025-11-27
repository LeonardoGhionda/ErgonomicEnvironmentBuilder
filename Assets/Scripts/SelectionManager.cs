using UnityEngine;
using UnityEngine.InputSystem;

public class SelectionManager : MonoBehaviour
{
    public Camera cam;

    private InputAction selectAction;
    private InputAction deselectAction;

    private InteractableObject selected;

    private void Start()
    {
        selectAction = InputSystem.actions["RoomBuilderControl/Select"];
        deselectAction = InputSystem.actions["Ui/Close"];
    }

    void Update()
    {
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
    public void ChangeSelectedObject(InteractableObject obj)
    {
        if (selected != null) selected.OnDeselected();
        selected = obj;
        if (selected != null) selected.OnSelected();
    }
}
