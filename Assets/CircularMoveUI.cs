using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CircularMoveUI : MonoBehaviour
{
    [SerializeField] float Radius;
    [SerializeField] float Speed;

    RectTransform _rectTransform;
    float _angle;

    private void Start()
    {
        _rectTransform = GetComponent<RectTransform>();
        _angle = _rectTransform.rotation.y;
        MoveToAngle(_angle);
    }

    private void FixedUpdate()
    {
        _angle += (Speed * Time.deltaTime);
        _angle %= 360f; // Keep angle within 0-360 degrees
        Debug.Log($"Angle: {_angle}");
        MoveToAngle(_angle);
    }

    private void MoveToAngle(float angle)
    {
        // 1. Convert Degrees to Radians for Sin/Cos
        float angleRadians = angle * Mathf.Deg2Rad;
        
        // 2. Calculate position on X/Y plane (Standard UI)
        float x = Radius * Mathf.Cos(angleRadians);
        float z = Radius * Mathf.Sin(angleRadians);
        
        _rectTransform.position = new Vector3(x, _rectTransform.position.y, z);

        // 3. Rotation
        _rectTransform.localRotation = Quaternion.Euler(0, angle, 0);
    }

}
