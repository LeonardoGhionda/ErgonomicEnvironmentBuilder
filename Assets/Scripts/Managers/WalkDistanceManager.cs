using System.Collections.Generic;
using UnityEngine;
using VolumetricLines;

public class WalkDistanceManager : MonoBehaviour
{
    [SerializeField] private Transform BalancePoint;
    [SerializeField] private float DistanceThreshold = 0.01f;
    [SerializeField] private VolumetricLineBehavior LineTemplate;
    [SerializeField] int MaxLines = 100;

    private List<Vector2> _points = new();
    private List<GameObject> _lines = new();

    private float _totalDistance = 0f;
    private const float LINE_HEIGHT = 0.01f;

    private bool _pathVisible = true;

    private Transform _linesContainer;

    private void OnEnable()
    {
        if (BalancePoint == null)
        {
            BalancePoint = DependencyProvider.CurrentCamera.transform;
        }

        Vector2 startPoint = BalancePoint.position.horizontalPlane();
        _points.Add(startPoint);

        _linesContainer = new GameObject("WalkPathLines").transform;
        _linesContainer.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
    }

    private void Update()
    {
        Vector2 currentPoint = BalancePoint.position.horizontalPlane();
        Vector2 lastPoint = _points[^1];

        if (Vector2.Distance(currentPoint, lastPoint) > DistanceThreshold)
        {
            // Add new 
            _points.Add(currentPoint);

            // Update distance
            _totalDistance += Vector2.Distance(currentPoint, lastPoint);

            // Create visual line
            GameObject newLine = Instantiate(LineTemplate.gameObject, _linesContainer);
            newLine.SetActive(_pathVisible);
            _lines.Add(newLine);

            // Limit number of lines to prevent performance issues
            if (_lines.Count > MaxLines)
            {
                Destroy(_lines[0]);
                _lines.RemoveAt(0);
            }

            // Set line positions
            VolumetricLineBehavior vlb = newLine.GetComponent<VolumetricLineBehavior>();
            vlb.StartPos = new Vector3(lastPoint.x, LINE_HEIGHT, lastPoint.y);
            vlb.EndPos = new Vector3(currentPoint.x, LINE_HEIGHT, currentPoint.y);
        }
    }


    public void ShowPath(bool value)
    {
        _pathVisible = value;
        _linesContainer.gameObject.SetActive(_pathVisible);
    }

    public void OnDisable()
    {
        _points.Clear();
        foreach (var item in _lines)
        {
            Destroy(item);
        }
        _lines.Clear();
    }
}