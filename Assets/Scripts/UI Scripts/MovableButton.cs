using UnityEngine;


/// <summary>
/// Move RectTransform based on the mouse position
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MovableButton : MonoBehaviour
{
    private AppActions input;
    private RectTransform rect;

    private bool moving = false;
    private Vector2 lastMousePosition;

    void Start()
    {
        input = StateManager.Instance.AppInput;
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        var pressed = input.Ui.MoveInterface.IsInProgress();
        if (pressed && IsMouseInside())
        {
            if (!moving)
            {
                lastMousePosition = input.Ui.Point.ReadValue<Vector2>();
            }
            moving = true;
        }
        else         
        {
            moving = false;
        }
        if (moving)
        {
            Vector2 mousePos = input.Ui.Point.ReadValue<Vector2>();
            Vector2 delta = mousePos - lastMousePosition;
            rect.anchoredPosition += delta;
            lastMousePosition = mousePos;
        }
    }
    bool IsMouseInside()
    {
        Vector2 screenPos = input.Ui.Point.ReadValue<Vector2>();

        return RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            screenPos,
            null
        );
    }


}
