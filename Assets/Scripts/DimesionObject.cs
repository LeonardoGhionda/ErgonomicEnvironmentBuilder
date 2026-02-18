using System;
using System.Collections;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

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

    private bool _deleteMode = false;

    private GameObject _interactable;
    private BoxCollider _iCollider;
    private XRSimpleInteractable _iSimpleInt;

    public void Initialize(Vector3 p1, Vector3 p2, Camera camera, bool isFinal, Transform target1 = null, Transform target2 = null)
    {
        _cam = camera;
        _t1 = target1;
        _t2 = target2;

        // Store offset relative to target if it exists otherwise keep world position
        if (_t1 != null) _offset1 = _t1.InverseTransformPoint(p1);
        else _p1 = p1;

        if (_t2 != null) _offset2 = _t2.InverseTransformPoint(p2);
        else _p2 = p2;

        // Delte measure data initialization
        if (isFinal)
        {
            _interactable = new GameObject("delete collider", typeof(BoxCollider), typeof(XRSimpleInteractable));

            _iCollider = _interactable.GetComponent<BoxCollider>();

            _iSimpleInt = _interactable.GetComponent<XRSimpleInteractable>();
            _iSimpleInt.colliders.Add(_iCollider);
            _iSimpleInt.selectEntered.AddListener(DeleteMeasure);

            _interactable.transform.SetParent(transform, false);
        }

        // Initial draw
        gameObject.SetActive(true);

    }

    private void DeleteMeasure(SelectEnterEventArgs args)
    {
        BoxCollider hitCollider = args.interactableObject.colliders.ElementAt(0) as BoxCollider;

        if (_deleteMode) // Delete this gameobject
        {
            _iSimpleInt.selectEntered.RemoveListener(DeleteMeasure);
            Destroy(gameObject);
        }
        else // Start delete mode
        {
            _deleteMode = true;
            StartCoroutine(ExitDeleteModeTimer());
            textLabel.text = "Click again\nto delete";
            textLabel.color = Color.red;
        }

    }

    IEnumerator ExitDeleteModeTimer()
    {
        // Aspetta 3 secondi (tempo reale di gioco)
        yield return new WaitForSeconds(3f);

        _deleteMode = false;
        textLabel.color = Color.yellow;
    }

    private void Update()
    {
        if (_interactable != null)
            _interactable.transform.SetLocalPositionAndRotation(textLabel.transform.localPosition, textLabel.transform.localRotation);
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

        if (_deleteMode == false) // In deletemode keep delete text
        {
            // Update Text Value
            float dist = Vector3.Distance(_p1, _p2);
            textLabel.text = $"{dist:F2}m";
        }

        // Update Text Position
        textLabel.transform.position = (_p1 + _p2) * 0.5f + Vector3.up * 0.2f;

        // Update text size
        textLabel.fontSize = _cam.orthographic ? 2f : 1f;
        textLabel.fontSize *= p2pDistance * textScaleFactor;
        textLabel.fontSize = Mathf.Clamp(textLabel.fontSize, minTextSize, maxTextSize);

        if (_iCollider != null)
        {
            // Size the collider perfectly to the 3D text bounds
            float offset = 1.3f;
            _iCollider.size = new(Mathf.Abs(textLabel.textBounds.size.x) * offset, Mathf.Abs(textLabel.textBounds.size.y) * offset, 0.1f);
            _iCollider.center = textLabel.textBounds.center;
        }

        // Update text rotation
        textLabel.transform.LookAt(_cam.transform);
        textLabel.transform.Rotate(Vector3.up * 180);
    }
}