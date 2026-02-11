using UnityEngine;
using TMPro;

public class DimensionObject : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private TextMeshPro textLabel;
    [SerializeField, Range(0.0001f, 1.0f)] private float textScaleFactor = 0.5f;
    [SerializeField] float minTextSize = .5f;
    [SerializeField] float maxTextSize = 1.5f;

    [SerializeField, Range(0.0001f, 1.0f)] private float lineScaleFactor = 0.5f;
    [SerializeField] float minLineThickness = .05f;
    [SerializeField] float maxLineThickness = .2f;

    private Transform _t1, _t2;
    private Vector3 _p1, _p2; // Current world positions
    private Vector3 _offset1, _offset2; // Local offsets relative to targets
    private Camera _cam;

    public void Initialize(Vector3 p1, Vector3 p2, Camera camera, Transform target1 = null, Transform target2 = null)
    {
        _cam = camera;
        _t1 = target1;
        _t2 = target2;

        // Store offset relative to target if it exists otherwise keep world position
        if (_t1 != null)
            _offset1 = _t1.InverseTransformPoint(p1);
        else
            _p1 = p1;

        if (_t2 != null)
            _offset2 = _t2.InverseTransformPoint(p2);
        else
            _p2 = p2;

        // Initial draw
        UpdateVisuals();
        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        // Recalculate world positions if targets exist
        // This automatically handles Rotation and Scaling
        if (_t1 != null) _p1 = _t1.TransformPoint(_offset1);
        if (_t2 != null) _p2 = _t2.TransformPoint(_offset2);

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        // Update Line
        lineRenderer.SetPosition(0, _p1);
        lineRenderer.SetPosition(1, _p2);

        float p2pDistance = Vector3.Distance(_p1, _p2);

        // Update line thickness
        lineRenderer.widthMultiplier = p2pDistance * lineScaleFactor;
        lineRenderer.widthMultiplier = Mathf.Clamp(lineRenderer.widthMultiplier, minLineThickness, maxLineThickness);

        // Update Text Value
        float dist = Vector3.Distance(_p1, _p2);
        textLabel.text = $"{dist:F2}m";

        // Update Text Position
        textLabel.transform.position = (_p1 + _p2) * 0.5f + Vector3.up * 0.2f;

        // Update text size
        textLabel.fontSize = _cam.orthographic ? 2f : 1f;
        textLabel.fontSize *= p2pDistance * textScaleFactor;
        textLabel.fontSize = Mathf.Clamp(textLabel.fontSize, minTextSize, maxTextSize); 

        // Update text rotation
        textLabel.transform.LookAt(_cam.transform);
        textLabel.transform.Rotate(Vector3.up * 180);
    }
}