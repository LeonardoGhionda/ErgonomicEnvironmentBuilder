using System;
using System.Collections.Generic;
using UnityEngine;

public class MeasureManager : MonoBehaviour
{
    public enum MeasureStep {None, SelectFirst, SelectSecond }

    [Header("Settings")]
    [SerializeField] private float snapThreshold = 0.3f;

    [Header("References")]
    [SerializeField] private GameObject Cursor;
    [SerializeField, Range(0.0001f, 1.0f)] private float CursorScaleFactor = 0.5f;
    [SerializeField] private GameObject MeasureLine;

    // Internal State
    private MeasureStep _currentStep = MeasureStep.SelectFirst;
    private Vector3 _startPoint;
    private List<DimensionObject> _activeDimensions = new List<DimensionObject>();

    private Transform _t1, _t2;

    bool _init = false;

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
        _init = true;
    }

    public void StartMeasure()
    {
        Cursor.SetActive(true);
        CurrentStep = MeasureStep.SelectFirst;
    }


    private void Update()
    {
        if (_init == false) return;
        UpdateCursorVisual();
    }

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
            var res = GetSnapPoint(controller);

#else
            var cursorPos = GetSnapPoint(_cam, mousePos);
#endif
            Cursor.transform.position = res.point;

            if (CurrentStep == MeasureStep.SelectFirst)
            {
                _t1 = res.hitObject;
            }
            //show temporary measurement line
            if (_currentStep == MeasureStep.SelectSecond)
            {
                _t2 = res.hitObject;
                MeasureLine.GetComponent<DimensionObject>().Initialize(_startPoint, res.point, _cam, false, _t1);
            }
        }
    }

    // --- PRIVATE HELPERS ---


    /// <summary>
    /// Calculates the best snap point based on mouse position.
    /// Updates the visual cursor automatically.
    /// </summary>
    /// 

#if USE_XR
    // Calculates snap point and hit object from XR controller origin
    private (Vector3 point, Transform hitObject) GetSnapPoint(Transform controller)
#else
    // Calculates snap point and hit object from camera and mouse position
    private (Vector3 point, Transform hitObject) GetSnapPoint(Camera cam, Vector2 mousePos)
#endif
    {
        Vector3 finalPoint = Vector3.zero;
        GameObject finalObject = null;

        // Create the ray based on the active platform
#if USE_XR
        Ray ray = new Ray(controller.position, controller.forward);
#else
        Ray ray = cam.ScreenPointToRay(mousePos);
#endif

        // Perform raycast and handle structural snapping
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity))
        {
            finalObject = hit.transform.gameObject;
            MeshFilter meshFilter = hit.transform.GetComponent<MeshFilter>();

            if (meshFilter != null && meshFilter.sharedMesh != null)
            {
                // Find nearest structural vertex in world space
                Vector3 snapCandidate = GetClosestStructuralPoint(hit.point, meshFilter.sharedMesh, hit.transform);

                // Apply snap position if within allowed threshold
                if (Vector3.Distance(hit.point, snapCandidate) < snapThreshold)
                {
                    finalPoint = snapCandidate;
                    return (finalPoint, finalObject.transform);
                }
            }

            // Fallback to exact hit point if no valid snap candidate is found
            finalPoint = hit.point;
            return (finalPoint, finalObject.transform);
        }

        // Return defaults if the raycast hits nothing entirely
        return (finalPoint, finalObject.transform);
    }

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
            dim.Initialize(start, end, _cam, true, _t1, _t2);
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

    private void UpdateCursorVisual()
    {
        float nScale = Vector3.Distance(_cam.transform.position, Cursor.transform.position) * CursorScaleFactor;
        nScale = Mathf.Clamp(nScale, 0.00001f, 0.1f);
        Cursor.transform.localScale = new(nScale, nScale, nScale);
    }
}