using System;
using UnityEngine;
using UnityEngine.InputSystem;


//Make the rect transform move based on the mouse position when 
//the Mobe interface action is used 
[RequireComponent(typeof(RectTransform))]
public class MovableButton : MonoBehaviour
{
    private InputAction moveInterface;
    private RectTransform rect;

    private bool moving = false;
    private Vector2 lastMousePosition;

    void Start()
    {
        moveInterface = InputSystem.actions.FindAction("Ui/Move Interface");
        rect = GetComponent<RectTransform>();
    }

    void Update()
    {
        if (moveInterface == null)
        {
            Debug.LogError("MovableButton: Move action not found in Input System!");
            return;
        }

        var pressed = moveInterface.IsPressed();
        if (pressed && IsMouseInside())
        {
            if (!moving)
            {
                lastMousePosition = Mouse.current.position.ReadValue();
            }
            moving = true;
        }
        else         
        {
            moving = false;
        }
        if (moving)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 delta = mousePos - lastMousePosition;
            rect.anchoredPosition += delta;
            lastMousePosition = mousePos;
        }
    }
    bool IsMouseInside()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return false;

        Vector2 screenPos = mouse.position.ReadValue();

        return RectTransformUtility.RectangleContainsScreenPoint(
            rect,
            screenPos,
            null
        );
    }


}
