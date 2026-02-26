using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

public class InputManager : MonoBehaviour
{
    [SerializeField] InputActionReference leftHandMovement;

    bool _handMenuOpen = false;

    HandMenuManager _handMenuManager;

    private void Start()
    {
        _handMenuManager = FindAnyObjectByType<HandMenuManager>();
        _handMenuManager.OnMenuStateChange += HandMenuState;
        _handMenuManager.OnMenuStateChange += LockLeftHandMovement;

        leftHandMovement.action.started += LockLeftHandMovement;
    }

    private void LockLeftHandMovement(bool open)
    {
        if (open) leftHandMovement.action.Disable();
        else leftHandMovement.action.Enable();
    }

    private void LockLeftHandMovement(InputAction.CallbackContext context)
    {
        if (_handMenuOpen) leftHandMovement.action.Disable();
        else leftHandMovement.action.Enable();
    }

    void HandMenuState(bool open)
    {
        _handMenuOpen = open;
    }
}
