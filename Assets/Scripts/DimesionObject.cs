using UnityEngine;
using TMPro;

public class DimensionObject : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private TextMeshPro textLabel;
  
    private Camera _cam;
    public void Initialize(Vector3 p1, Vector3 p2, Camera camera)
    {
        _cam = camera;

        // visual setup (we will expand this later)
        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, p1);
        lineRenderer.SetPosition(1, p2);

        // calculate distance
        float dist = Vector3.Distance(p1, p2);
        textLabel.text = $"{dist:F2}m";

        // center text
        textLabel.transform.position = (p1 + p2) * 0.5f + Vector3.up * 0.2f;

        // orient text towards camera (billboard) or up
        textLabel.transform.rotation = Quaternion.Euler(90, 0, 0);

        gameObject.SetActive(true);
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        textLabel.fontSize = _cam.orthographic? 5f : 2.6f;

        // Text always face the camera
        textLabel.transform.LookAt(_cam.transform);
        textLabel.transform.Rotate(Vector3.up * 180);
    }
}