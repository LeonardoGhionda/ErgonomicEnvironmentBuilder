using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class CircularMoveUI : MonoBehaviour
{
    private float _radius;
    public float Radius
    {
        get => _radius;
        set
        {
            _radius = value;
            MoveToAngle(_angle);
        }
    }

    RectTransform _rectTransform;
    float _angle;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _angle = _rectTransform.localEulerAngles.y;
        MoveToAngle(_angle);
    }

    public void Step(float degAngle)
    {
        if (degAngle < 0) degAngle += 360f;

        _angle += degAngle;
        _angle %= 360;
        MoveToAngle(_angle);
    }

    public void SetAngle(float degAngle)
    {
        if (degAngle < 0) degAngle += 360f;

        _angle = degAngle % 360;
        MoveToAngle(_angle);
    }

    private void MoveToAngle(float angle)
    {
        angle = (angle + 180f) % 360;
        // Position
        float x = -_radius * Mathf.Sin(angle * Mathf.Deg2Rad);
        float z = _radius * Mathf.Cos(angle * Mathf.Deg2Rad);

        _rectTransform.anchoredPosition3D = new Vector3(x, _rectTransform.anchoredPosition.y, z);

        // Rotation
        _rectTransform.localRotation = Quaternion.Euler(0, -angle + 180, 0);
    }

}
