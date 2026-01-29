using UnityEngine;
using System.Collections.Generic;
using Unity.VisualScripting;
using System;

public class MeasureManager : MonoBehaviour
{
    public enum MeasureStep {None, SelectFirst, SelectSecond }

    [Header("Settings")]
    [SerializeField] private float snapThreshold = 0.3f;

    [Header("References")]
    [SerializeField] private GameObject Cursor;
    [SerializeField] private GameObject MeasureLine;

    // Internal State
    private MeasureStep _currentStep = MeasureStep.SelectFirst;
    private Vector3 _startPoint;
    private List<DimensionObject> _activeDimensions = new List<DimensionObject>();

    // Dependency 
    Camera _cam;

    // Getters
    public bool IsMeasuring => _currentStep != MeasureStep.None;
    public MeasureStep CurrentStep
    {
        set { _currentStep = value; }
        get { return _currentStep; }
    }

    public void Init(Camera cam)
    {
        ResetTool();
        _cam = cam;
    }

    // --- MAIN API ---

    public void ResetTool()
    {
        _currentStep = MeasureStep.None;
        if (Cursor) Cursor.SetActive(false);

    }

    /// <summary>
    /// On user click, register the point based on the current step.
    /// </summary>
    public void RegisterClick()
    {
        if (Cursor == null)
        {
            Debug.LogError("MeasureManager: CursorGO not assigned!");
            return;
        }
        Vector3 clickPos = Cursor.transform.position;

        if (_currentStep == MeasureStep.SelectFirst)
        {
            _startPoint = clickPos;
            MeasureLine.SetActive(true); // show temporary line 
            _currentStep = MeasureStep.SelectSecond;
        }
        else
        {
            CreateDimension(_startPoint, clickPos);
            ResetTool(); 
        }
    }

#if USE_XR
    public void MoveCursor(Transform controller)
#else
    public void MoveCursor(Vector2 mousePos)
#endif
    {
        if (_currentStep != MeasureStep.None)
        {
#if USE_XR
            var cursorPos = GetSnapPoint(controller);
#else
            var cursorPos = GetSnapPoint(_cam, mousePos);
#endif
            Cursor.transform.position = cursorPos;

            //show temporary measurement line
            if (_currentStep == MeasureStep.SelectSecond)
            {
                MeasureLine.GetComponent<DimensionObject>().Initialize(_startPoint, cursorPos, _cam);
            }
        }
    }

    public void StartMeasure()
    {
        Cursor.SetActive(true);
        CurrentStep = MeasureStep.SelectFirst;
    }

    // --- PRIVATE HELPERS ---
    /// <summary>
    /// Calculates the best snap point based on mouse position.
    /// Updates the visual cursor automatically.
    /// </summary>
    /// 

#if USE_XR
    // VR Signature: Uses the Controller Transform (Origin + Forward)
    private Vector3 GetSnapPoint(Transform controller)
#else
// Desktop: Uses Camera + Mouse Coordinates
    private Vector3 GetSnapPoint(Camera cam, Vector2 mousePos)
#endif
    {
        Vector3 finalPoint = Vector3.zero;

        // 1. Generate the Ray based on the platform
#if USE_XR
        Ray ray = new Ray(controller.position, controller.forward);
#else
    Ray ray = cam.ScreenPointToRay(mousePos);
#endif

        // 2. Shared Raycast & Snapping Logic
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            MeshFilter meshFilter = hit.transform.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Note: Ensure GetClosestStructuralPoint handles the transform.localToWorldMatrix 
                // to support rotation/scaling correctly.
                Vector3 snapCandidate = GetClosestStructuralPoint(hit.point, meshFilter.sharedMesh, hit.transform);

                if (Vector3.Distance(hit.point, snapCandidate) < snapThreshold)
                {
                    finalPoint = snapCandidate;
                    return finalPoint;
                }
            }

            finalPoint = hit.point;
            return finalPoint;
        }

        return finalPoint;
    }

    /*
    private Vector3 GetSnapPoint(Camera cam, Vector2 mousePos)
    {
        Vector3 finalPoint = Vector3.zero;
        Ray ray = cam.ScreenPointToRay(mousePos);

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            // Retrieve visual mesh from MeshFilter, regardless of Collider type
            MeshFilter meshFilter = hit.transform.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Pass the Mesh and Transform directly instead of the Collider
                // Note: You must update GetClosestStructuralPoint signature to accept (Vector3, Mesh, Transform)
                Vector3 snapCandidate = GetClosestStructuralPoint(hit.point, meshFilter.sharedMesh, hit.transform);

                // Check threshold
                if (Vector3.Distance(hit.point, snapCandidate) < snapThreshold)
                {
                    finalPoint = snapCandidate;
                    return finalPoint;
                }
            }

            // Fallback: Snap to surface
            finalPoint = hit.point;
            return finalPoint;
        }

        return finalPoint;
    }
    */

    /// <summary>
    /// Creates a new permanent dimension line between two points.
    /// </summary>
    public void CreateDimension(Vector3 start, Vector3 end)
    {
        if (MeasureLine == null) return;

        GameObject obj = Instantiate(MeasureLine);
        DimensionObject dim = obj.GetComponent<DimensionObject>();

        MeasureLine.SetActive(false); // hide temporary line

        if (dim != null)
        {
            dim.Initialize(start, end, _cam);
            _activeDimensions.Add(dim);
        }
        else
        {
            Debug.LogError("MeasureManager: Dimension prefab does not have a DimensionObject component!");
        }
    }

    public void HideCursor() => UpdateCursor(false, Vector3.zero);

    // --- INTERNAL LOGIC ---

    private void UpdateCursor(bool active, Vector3 pos)
    {
        if (Cursor == null)
        {
            Debug.LogError("Snap Cursor Visual not assigned in MeasureManager!");
            return;
        }

        Cursor.SetActive(active);
        if (active) Cursor.transform.position = pos;
    }

    /// <summary>
    /// Tryes to find the closest structural point (vertex, edge center or tringle center) on the mesh to the hit point.
    /// </summary>
    /// <returns>Vector3 snap point</returns>
    private Vector3 GetClosestStructuralPoint(Vector3 hitPoint, Mesh mesh, Transform trans)
    {
        Vector3 bestPoint = hitPoint;
        float minDstSqr = float.MaxValue;

        // Work in local space to avoid transforming every vertex
        Vector3 localHit = trans.InverseTransformPoint(hitPoint);

        Vector3[] verts = mesh.vertices;
        int[] tris = mesh.triangles;

        // Helper to check if a point is the new closest
        void CheckCandidate(Vector3 candidate)
        {
            float dstSqr = (localHit - candidate).sqrMagnitude;
            if (dstSqr < minDstSqr)
            {
                minDstSqr = dstSqr;
                bestPoint = candidate;
            }
        }

        // 1. Check Vertices
        for (int i = 0; i < verts.Length; i++)
        {
            CheckCandidate(verts[i]);
        }

        // 2. Check Edges and Face Centers
        // Iterate through triangles (sets of 3 indices)
        for (int i = 0; i < tris.Length; i += 3)
        {
            Vector3 v0 = verts[tris[i]];
            Vector3 v1 = verts[tris[i + 1]];
            Vector3 v2 = verts[tris[i + 2]];

            // Face Center (Centroid)
            Vector3 faceCenter = (v0 + v1 + v2) / 3f;
            CheckCandidate(faceCenter);

            // Edge Centers (Midpoints)
            CheckCandidate((v0 + v1) * 0.5f);
            CheckCandidate((v1 + v2) * 0.5f);
            CheckCandidate((v2 + v0) * 0.5f);
        }

        // Convert result back to world space
        return trans.TransformPoint(bestPoint);
    }

    public void ClearAllMeasures()
    {
        foreach (var dim in _activeDimensions)
        {
            if (dim != null)
            {
                Destroy(dim.gameObject);
            }
        }
    }
}