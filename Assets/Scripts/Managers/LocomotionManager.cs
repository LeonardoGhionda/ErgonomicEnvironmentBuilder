using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit.UI.BodyUI;

public class LocomotionManager : MonoBehaviour
{
    [SerializeField] InputActionReference leftHandMovement;
    private bool _leftHandMovementEnabled;

    [SerializeField] InputActionReference rightHandMovement;
    private bool _rightHandMovementEnabled;

    HandMenuManager _handMenuManager;

    private void Start()
    {
        _handMenuManager = FindAnyObjectByType<HandMenuManager>();
    }

    public void LockRightHandMovement(bool locked)
    {
        _rightHandMovementEnabled = !locked;
    }

    public void LockLeftHandMovement(bool locked)
    {
        _leftHandMovementEnabled = !locked;
    }

    public void HandMenuControl(bool value)
    {
        // This is necessary because when locomotion is locked and i click
        // controller triggerlocomotion turn on again and idk why.
        if (value) leftHandMovement.action.started += IfMenuOpenBlockAndLock;
        else leftHandMovement.action.started -= IfMenuOpenBlockAndLock;
    }

    public void IfMenuOpenBlockAndLock(InputAction.CallbackContext context)
    {
        LockLeftHandMovement(_handMenuManager.Open);
    }

    private void Update()
    {
        if (_leftHandMovementEnabled) leftHandMovement.action.Enable();
        else leftHandMovement.action.Disable();

        if (_rightHandMovementEnabled) rightHandMovement.action.Enable();
        else rightHandMovement.action.Disable();

    }
}
