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
    private Vector3 _p1, _p2;
    private Vector3 _offset1, _offset2;
    private Camera _cam;

    private bool _deleteMode = false;

    private GameObject _interactable;
    private BoxCollider _iCollider;
    private XRSimpleInteractable _iSimpleInt;
    private bool _isHeight = false;

    public void Initialize(Vector3 p1, Vector3 p2, Camera camera, bool isFinal, Transform target1 = null, Transform target2 = null, bool isHeight = false)
    {
        _cam = camera;
        _t1 = target1;
        _t2 = target2;

        if (_t1 != null) _offset1 = _t1.InverseTransformPoint(p1);
        else _p1 = p1;

        if (_t2 != null) _offset2 = _t2.InverseTransformPoint(p2);
        else _p2 = p2;

        if (isFinal)
        {
            _interactable = new GameObject("delete collider");
            _interactable.transform.SetParent(transform, false);

            _iCollider = _interactable.AddComponent<BoxCollider>();
            _iCollider.isTrigger = false;

            Rigidbody rb = _interactable.AddComponent<Rigidbody>();
            rb.isKinematic = true;

            _iSimpleInt = _interactable.AddComponent<XRSimpleInteractable>();
            _iSimpleInt.selectEntered.AddListener(DeleteMeasure);
        }

        _isHeight = isHeight;

        gameObject.SetActive(true);
    }

    public void ResetPosition(Vector3 p1, Vector3 p2, Transform target1 = null, Transform target2 = null)
    {
        _t1 = target1;
        _t2 = target2;

        if (_t1 != null) _offset1 = _t1.InverseTransformPoint(p1);
        else _p1 = p1;

        if (_t2 != null) _offset2 = _t2.InverseTransformPoint(p2);
        else _p2 = p2;
    }

    private void DeleteMeasure(SelectEnterEventArgs args)
    {
        if (_deleteMode)
        {
            _iSimpleInt.selectEntered.RemoveListener(DeleteMeasure);
            Destroy(gameObject);
        }
        else
        {
            _deleteMode = true;
            _ = StartCoroutine(ExitDeleteModeTimer());
            textLabel.text = "Click again\nto delete";
            textLabel.color = Color.red;
        }
    }

    IEnumerator ExitDeleteModeTimer()
    {
        yield return new WaitForSeconds(3f);

        _deleteMode = false;
        textLabel.color = Color.yellow;
    }

    private void LateUpdate()
    {
        if (_cam == null) return;

        if (_t1 != null) _p1 = _t1.TransformPoint(_offset1);
        if (_t2 != null) _p2 = _t2.TransformPoint(_offset2);

        if (_isHeight)
        {
            _p2 = _p1;
            _p2.y = 0f;
        }

        UpdateVisuals();
    }

    private void UpdateVisuals()
    {
        lineRenderer.SetPosition(0, _p1);
        lineRenderer.SetPosition(1, _p2);

        float p2pDistance = Vector3.Distance(_p1, _p2);

        lineRenderer.widthMultiplier = p2pDistance * lineScaleFactor;
        lineRenderer.widthMultiplier = Mathf.Clamp(lineRenderer.widthMultiplier, minLineThickness, maxLineThickness);

        if (_deleteMode == false)
        {
            float dist = Vector3.Distance(_p1, _p2);
            textLabel.text = $"{dist:F2}m";
        }

        textLabel.transform.position = (_p1 + _p2) * 0.5f + Vector3.up * 0.2f;

        textLabel.fontSize = _cam.orthographic ? 2f : 1f;
        textLabel.fontSize *= p2pDistance * textScaleFactor;
        textLabel.fontSize = Mathf.Clamp(textLabel.fontSize, minTextSize, maxTextSize);

        textLabel.transform.LookAt(_cam.transform);
        textLabel.transform.Rotate(Vector3.up * 180);

        if (_iCollider != null)
        {
            float offset = 1.3f;
            _iCollider.size = new Vector3(Mathf.Abs(textLabel.textBounds.size.x) * offset, Mathf.Abs(textLabel.textBounds.size.y) * offset, 0.1f);
            _iCollider.center = textLabel.textBounds.center;

            _interactable.transform.SetLocalPositionAndRotation(textLabel.transform.localPosition, textLabel.transform.localRotation);
        }
    }
}