using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMeasureView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI height;
    [SerializeField] TextMeshProUGUI forward;
    [SerializeField] TextMeshProUGUI right;

    [SerializeField] Transform controller;

    [SerializeField] InputActionReference openAction;

    private Transform _bellyButtonPoint;
    private BodyPointsManager _bodyPointsManager;
    private Canvas _canvas;

    private bool _open = false;

    private void Start()
    {
        _bodyPointsManager = FindAnyObjectByType<BodyPointsManager>();
        forward.fontSize /= 2;
        right.fontSize /= 2;

        height.color = Color.black;
        forward.color = Color.red;
        right.color = Color.red;

        forward.text = "Calibration required";
        right.text = "Calibration required";

        _canvas = GetComponent<Canvas>();
        _canvas.enabled = _open;
        
        openAction.action.Enable();
        openAction.action.performed += ShowMenu;

    }

    private void ShowMenu(InputAction.CallbackContext context)
    {
        _open = !_open;
        _canvas.enabled = _open;
    }

    private void Update()
    {
        Vector3 localPos = controller.localPosition;

        height.text = $"{localPos.y:F2}";

        if (_bellyButtonPoint == null)
        {
            if (_bodyPointsManager.BellyButton == null) return;
            
            _bellyButtonPoint = _bodyPointsManager.BellyButton;

            // Reset the font size to normal 
            forward.fontSize *= 2;
            right.fontSize *= 2;
            forward.color = Color.black;
            right.color = Color.black;

        }

        // Calculate the vector from belly to controller
        Vector3 offset = controller.position - _bellyButtonPoint.position;

        // Project the offset onto the belly's local axes
        float distanceForward = Vector3.Dot(offset, _bellyButtonPoint.forward);
        float distanceRight = Vector3.Dot(offset, _bellyButtonPoint.right);

        forward.text = $"{distanceForward:F2}";
        right.text = $"{distanceRight:F2}";
    }

    private void OnDestroy()
    {
        openAction.action.performed -= ShowMenu;
    }


}

