using System;
using UnityEngine;

public class BodyPointsManager : MonoBehaviour
{
    [Header("References")]
    [Tooltip("The Main Camera or Head transform")]
    [SerializeField] private Transform headTransform;

    [Header("Settings")]
    [Tooltip("Percentage of total height for belly button")]
    [Range(0.1f, 1f)]
    [SerializeField] private float bellyButtonHeightRatio = 0.6f;
    [Tooltip("Estimated length of the neck for pitch correction")]
    [SerializeField] private float neckPivotLength = 0.15f;

    private bool _isCalibrated = false;
    private Transform _bellyButtonEmpty;

    public Transform BellyButton
    {
        get { if (_isCalibrated) return _bellyButtonEmpty; else return null; }
    }

    private void Start()
    {
        if (_bellyButtonEmpty == null)
        {
            _bellyButtonEmpty = new GameObject("Belly Button Position").transform;
        }
    }

    private void Update()
    {
        if (_isCalibrated && headTransform != null)
        {
            UpdateAnchorPosition();
        }
    }

    public void Calibrate()
    {
        if (headTransform == null) return;

        _isCalibrated = true;
        UpdateAnchorPosition();
    }

    private void UpdateAnchorPosition()
    {
        float headPitch = headTransform.eulerAngles.x;

        if (headPitch > 180f)
        {
            headPitch -= 360f;
        }

        float pitchInRadians = headPitch * Mathf.Deg2Rad;
        float forwardShift = Mathf.Sin(pitchInRadians) * neckPivotLength;

        Vector3 flatForward = headTransform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 neckHorizontalPosition = headTransform.position - (flatForward * forwardShift);

        Vector3 targetPosition = neckHorizontalPosition;
        targetPosition.y = headTransform.position.y * bellyButtonHeightRatio;

        Quaternion flatRotation = Quaternion.Euler(0, headTransform.eulerAngles.y, 0);

        _bellyButtonEmpty.SetPositionAndRotation(targetPosition, flatRotation);

    }
}