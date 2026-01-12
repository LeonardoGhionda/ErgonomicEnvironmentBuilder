using Unity.VisualScripting;
using UnityEngine;

public class ColliderVisual : MonoBehaviour
{

    LineRenderer _lineRenderer;
    Camera _cam;
    Material _lineMaterial;

    float thicknessP = 0.0035f;
    float thicknessO = 0.005f;

    public ColliderVisual Init(Camera cam)
    {
        _lineMaterial = Resources.Load<Material>("Materials/ColliderVisualMat");
        if (_lineMaterial == null) Debug.LogError("Collider line material not found");
        _lineRenderer = gameObject.AddComponent<LineRenderer>();

        _lineRenderer.material = _lineMaterial;
        _lineRenderer.loop = false;
        _lineRenderer.useWorldSpace = false;
        _lineRenderer.enabled = false;

        _cam = cam;
        return this;
    }

    void Update()
    {
        // Safety check
        if (_cam == null || _lineRenderer == null)
        {
            Debug.LogError("ColliderVisual: not Initialized correctly");
            return;
        }

        // Adjust line thickness based on camera type
        if (_cam.orthographic)
            _lineRenderer.widthMultiplier = _cam.orthographicSize * thicknessO;
        else
            _lineRenderer.widthMultiplier = Vector3.Distance(_cam.transform.position, transform.position) * thicknessP;
    }

    private void OnDestroy()
    {
        if (_lineRenderer != null)
            Destroy(_lineRenderer);
    }

    public void ChangeTarget(BoxCollider boxCollider)
    {
        // Ensure LineRenderer exists
        if (_lineRenderer == null)
        {
            if(!gameObject.TryGetComponent(out _lineRenderer))
                _lineRenderer = gameObject.AddComponent<LineRenderer>();
        }

        if (boxCollider == null)
        {
            _lineRenderer.enabled = false;
            transform.SetParent(null);
            return;
        }

        Vector3 c = boxCollider.center;
        Vector3 s = boxCollider.size * 0.5f;

        // Define the 8 corners in Local Space
        Vector3[] corners = new Vector3[8]
        {
            c + new Vector3(-s.x, -s.y, -s.z), // 0: Bottom-Front-Left
            c + new Vector3( s.x, -s.y, -s.z), // 1: Bottom-Front-Right
            c + new Vector3( s.x, -s.y,  s.z), // 2: Bottom-Back-Right
            c + new Vector3(-s.x, -s.y,  s.z), // 3: Bottom-Back-Left
            c + new Vector3(-s.x,  s.y, -s.z), // 4: Top-Front-Left
            c + new Vector3( s.x,  s.y, -s.z), // 5: Top-Front-Right
            c + new Vector3( s.x,  s.y,  s.z), // 6: Top-Back-Right
            c + new Vector3(-s.x,  s.y,  s.z)  // 7: Top-Back-Left
        };

        Vector3[] linePoints = new Vector3[]
        {
            //bottom
            corners[0], corners[1], corners[2], corners[3], corners[0],
            corners[4],  
            //top
            corners[5], corners[6], corners[7], corners[4],
            corners[5],
            corners[1],
            corners[2],
            corners[6],
            corners[7],
            corners[3]
        };

        _lineRenderer.positionCount = linePoints.Length;
        _lineRenderer.SetPositions(linePoints);
        _lineRenderer.enabled = true;

        _lineRenderer.widthMultiplier = 0f;

        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.identity;
        transform.localScale = Vector3.one;
        // Parent to the box collider for correct positioning
        transform.SetParent(boxCollider.transform, false);
    }

    public void ClearTarget()
    {
        transform.SetParent(null);
        _lineRenderer.enabled = false;
    }
}


