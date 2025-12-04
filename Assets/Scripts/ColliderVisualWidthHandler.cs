using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class ColliderVisualWidthHandler : MonoBehaviour
{

    LineRenderer lineRenderer;
    Camera cam;

    float thicknessP = 0.0035f;
    float thicknessO = 0.005f;

    void Start()
    {
        lineRenderer = GetComponent<LineRenderer>();
        if (lineRenderer == null) Debug.LogError("Line renderer not found");
        cam = Camera.main;
        if (cam == null) Debug.LogError("Camera not found");
    }


    void Update()
    {
        if (cam.orthographic)
            lineRenderer.widthMultiplier = cam.orthographicSize * thicknessO;
        else
            lineRenderer.widthMultiplier = Vector3.Distance(cam.transform.position, transform.position) * thicknessP;
    }
}
