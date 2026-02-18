using UnityEngine;


/// <summary>
/// Move RectTransform based on the mouse position
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class MovableButton : MonoBehaviour
{
    private RectTransform rect;

    private bool moving = false;
    private Vector2 lastMousePosition;

    void Start()
    {
        rect = GetComponent<RectTransform>();
    }

    public void Move(Vector2 mousePos)
    {
        if (IsMouseInside(mousePos))
        {
            if (!moving)
                lastMousePosition = mousePos;
            moving = true;
        }
        else
        {
            moving = false;
        }
        if (moving)
        {
            Vector2 pos = mousePos;
            Vector2 delta = pos - lastMousePosition;
            rect.anchoredPosition += delta;
            lastMousePosition = pos;
        }
    }
    bool IsMouseInside(Vector2 mousePos)
    {
        return RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            mousePos,
            null
        );
    }


}
