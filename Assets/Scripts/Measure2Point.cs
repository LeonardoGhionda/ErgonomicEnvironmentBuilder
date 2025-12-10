using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class Measure2Point : MonoBehaviour
{
    Vector3 pos1, pos2;

    InputAction selectAction;

    readonly float thickness = .1f;

    bool measuring = false;
    bool Measuring
    {
        get { return measuring; }
        set 
        { 
            measuring = value;

            //disable selection and deselect current selection
            //FindAnyObjectByType<SelectionManager>().SelectionEnabled = !value;
        }
    }

    private void Awake()
    {
        //start measuring process when button is clicked
        GetComponent<Button>().onClick.AddListener(() => {
            Measuring = true;
            pos1 = Vector3.zero;
            pos2 = Vector3.zero;
        });

        selectAction = InputSystem.actions.FindAction("Ui/Click");
    }

    private void Update()
    {
        if (!Measuring) return;

        if(pos1 == Vector3.zero)
        { Debug.Log("Waiting point 1"); }
        if (pos2 == Vector3.zero)
        { Debug.Log("Waiting point 2"); }

        if (selectAction.WasPressedThisFrame())
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());


            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
            {
                if (pos1 == Vector3.zero) pos1 = hit.point;    
                else pos2 = hit.point;
            }
        }

        if (pos1 != Vector3.zero && pos2 != Vector3.zero)
        {
            //create measure 
            //-------------------

            Transform measure = GameObject.CreatePrimitive(PrimitiveType.Cylinder).transform;
            Destroy(measure.GetComponent<Collider>());
            measure.name = "Measure";

            Debug.Log($"[{pos1.x}, {pos1.y}]  [{pos2.x}, {pos2.y}]");

            Vector3 dir = pos2 - pos1;
            float dist = dir.magnitude;

            // midpoint
            measure.position = pos1 + dir * 0.5f;

            // scale: cylinder default height is 2 units (along Y axis)
            measure.localScale = new(thickness, dist * 0.5f, thickness);

            // rotate cylinder so its Y-axis aligns with the direction
            measure.rotation = Quaternion.FromToRotation(Vector3.up, dir);

            measure.GetComponent<MeshRenderer>().material = Resources.Load<Material>("Materials/Yellow_AlwaysOnTop");

            Measuring = false;
        }
    }
}
