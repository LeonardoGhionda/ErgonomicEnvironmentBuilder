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
        if (moving)
        {
            Vector2 pos = mousePos;
            Vector2 delta = pos - lastMousePosition;
            rect.anchoredPosition += delta;
            lastMousePosition = pos;
        }
    }

    public void MoveStart(Vector2 mousePos)
    {
        lastMousePosition = mousePos;
        moving = true;
    }
    public void MoveStop() => moving = false;


}
