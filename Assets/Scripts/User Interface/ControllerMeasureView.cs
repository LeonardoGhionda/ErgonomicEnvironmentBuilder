using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerMeasureView : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI height;
    [SerializeField] TextMeshProUGUI distance;

    [SerializeField] InputActionReference openAction;
    [SerializeField] float fontSizeWarning;

    private Transform _bellyButtonPoint;
    private BodyPointsManager _bodyPointsManager;
    private Canvas _canvas;

    private bool _open = false;

    private void Start()
    {
        _bodyPointsManager = FindAnyObjectByType<BodyPointsManager>();

        distance.color = Color.red;



        distance.fontSize = fontSizeWarning;
        distance.text = "Calibration required";

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
        Vector3 localPos = DependencyProvider.RightHand.localPosition;

        height.text = $"{localPos.y:F2}";

        if (_bellyButtonPoint == null)
        {
            if (_bodyPointsManager.BellyButton == null) return;

            _bellyButtonPoint = _bodyPointsManager.BellyButton;

            // Reset the font size to normal 
            distance.color = Color.black;
            distance.fontSize = height.fontSize;
        }

        // Calculate the vector from belly to controller
        Vector3 offset = DependencyProvider.RightHand.position - _bellyButtonPoint.position;

        // Project the offset onto the belly's local axes
        float distanceForward = Vector3.Dot(offset, _bellyButtonPoint.forward);
        float distanceRight = Vector3.Dot(offset, _bellyButtonPoint.right);

        distance.text = $"{Mathf.Sqrt(distanceForward * distanceForward + distanceRight * distanceRight):F2}";
    }

    private void OnDestroy()
    {
        openAction.action.performed -= ShowMenu;
    }


}

