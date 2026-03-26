using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class HM_MocapCalibration : HM_Base
{
    private HandMenuManager _handMenu;
    private XROriginMoCapSync _mocapSync;
    private LocomotionManager _locManager;
    private AppActions.VRCalibrationActions _input;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _handMenu = Managers.Get<HandMenuManager>();
        _locManager = Managers.Get<LocomotionManager>();
        _mocapSync = DependencyProvider.VRPlayer.GetComponent<XROriginMoCapSync>();
    }

    override public void OnClick()
    {
        base.OnClick();
        _handMenu.Show(false);
        _handMenu.Lock = true;
        _locManager.LockTeleport(true);

        _input = DependencyProvider.Input.VRCalibration;

        // Subscribe to events once
        _input.HeadOffset.performed += ChangeHeadOffset;
        _input.BodyRotation.performed += ChangeBodyRotation;
        _input.ConfirmCalibration.performed += StopCalibration;

        _input.Enable();
    }

    private void ChangeBodyRotation(InputAction.CallbackContext context)
    {
        float rotVal = context.action.ReadValue<Vector2>().x / 2f;
        _mocapSync.RotationOffset += rotVal;
    }

    private void ChangeHeadOffset(InputAction.CallbackContext context)
    {
        Vector2 offset = context.action.ReadValue<Vector2>() / 100f;
        var prev = _mocapSync.EyeOffset;
        _mocapSync.EyeOffset = new(prev.x, prev.y + offset.y, prev.z + offset.x);
    }

    private void StopCalibration(InputAction.CallbackContext context)
    {
        _handMenu.Lock = false;
        _locManager.LockTeleport(false);

        // Unsubscribe from events
        _input.HeadOffset.performed -= ChangeHeadOffset;
        _input.BodyRotation.performed -= ChangeBodyRotation;
        _input.ConfirmCalibration.performed -= StopCalibration;

        _input.Disable();
    }
}