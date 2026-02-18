using System;
using UnityEngine;

public class BodyPointsManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Main Camera or Head transform")]
    [SerializeField] private Transform headTransform;

    // Internal state
    private Vector3 _calibratedOffset;
    private bool _isCalibrated = false;
    private Transform _calibrationController;
    private Transform _bellyButtonEmpty;

    public Transform BellyButton => _bellyButtonEmpty;

    private void Update()
    {
        if (_isCalibrated && headTransform != null)
        {
            UpdateAnchorPosition();
        }
    }

    public void InitBellyButtonCalibration(Transform calibrateTransform)
    {
        _calibrationController = calibrateTransform;

        if (_bellyButtonEmpty == null)
        {
            _bellyButtonEmpty = new GameObject("Belly Button Position").transform;
        }
    }

    public void Calibrate()
    {
        if (_calibrationController == null || headTransform == null) return;

        // Create a rotation that only considers the Y axis (Yaw)
        Quaternion flatHeadRotation = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);

        // Calculate the vector from Head to the Controller in world space
        Vector3 worldDist = _calibrationController.position - headTransform.position;

        // Convert world distance to a local offset relative to the head's forward direction
        _calibratedOffset = Quaternion.Inverse(flatHeadRotation) * worldDist;

        _isCalibrated = true;

        // Force an immediate update to snap the visual to the correct spot
        UpdateAnchorPosition();

    }

    private void UpdateAnchorPosition()
    {
        // Get current head orientation ignoring pitch and roll
        Quaternion currentFlatRotation = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);

        // Calculate target world position by applying the stored local offset
        Vector3 targetPosition = headTransform.position + (currentFlatRotation * _calibratedOffset);

        // Direct assignment without Lerp for instant movement
        _bellyButtonEmpty.position = targetPosition;

        // Keep the visual rotation aligned with the body yaw
        _bellyButtonEmpty.rotation = currentFlatRotation;
    }
}