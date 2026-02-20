using System.Linq;
using UnityEngine;

public class SelectionFollower : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float minHeight = 0.5f;
    [SerializeField] private float maxHeight = 1.5f;
    [Tooltip("Offset away from the collider surface towards the player")]
    [SerializeField] private float surfaceOffset = 0.2f;

    private VRSelectionManager _selectionManager;
    private Transform _selectedTransform;
    private BoxCollider _selectedCollider;


    private void SetActive(bool active)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(active);
        }
    }

    void Start()
    {
        _selectionManager = FindAnyObjectByType<VRSelectionManager>();
        _selectionManager.OnSelectionChanged += ReachSelected;
        SetActive(false);
    }

    private void ReachSelected(VRSelectionManager.SelectionChangedArgs args)
    {
        if (args.selection == null)
        {
            _selectedTransform = null;
            _selectedCollider = null;
            SetActive(true);
            return;
        }

        SetActive(true);
        // Store references to update every frame
        _selectedTransform = args.selection.transform;
        _selectedCollider = _selectedTransform.GetComponent<BoxCollider>();

        UpdatePositionAndRotation();
    }

    private void Update()
    {
        if (_selectedTransform != null)
        {
            UpdatePositionAndRotation();
        }
    }

    private void UpdatePositionAndRotation()
    {
        Transform camTransform = Camera.main.transform;
        Vector3 center;

        // Use collider center if available, otherwise transform center
        if (_selectedCollider != null)
        {
            center = _selectedCollider.bounds.center;
        }
        else
        {
            center = _selectedTransform.position;
        }

        // Calculate direction to player on a flat plane
        Vector3 dirToPlayer = camTransform.position - center;
        dirToPlayer.y = 0;
        dirToPlayer.Normalize();

        Vector3 targetPos;

        if (_selectedCollider != null)
        {
            // Calculate the extent (half-size) of the box in the direction of the player
            // This finds the "largest point" or edge of the box toward the player
            Vector3 extents = _selectedCollider.bounds.extents;

            // We find how far the box extends along the line to the player
            float distanceToEdge = Mathf.Abs(dirToPlayer.x * extents.x) +
                                   Mathf.Abs(dirToPlayer.y * extents.y) +
                                   Mathf.Abs(dirToPlayer.z * extents.z);

            // Position at edge + small extra offset
            targetPos = center + (dirToPlayer * (distanceToEdge + surfaceOffset));
        }
        else
        {
            // Fallback if no collider
            targetPos = center + (dirToPlayer * surfaceOffset);
        }

        // Apply fixed height
        targetPos.y = Mathf.Clamp(_selectedTransform.position.y + _selectedCollider.center.y, minHeight, maxHeight);
        transform.position = targetPos;

        // Always face player
        Vector3 lookDir = camTransform.position - transform.position;
        lookDir.y = 0;
        if (lookDir != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(-lookDir);
        }
    }

    private void OnDestroy()
    {
        if (_selectionManager != null)
        {
            _selectionManager.OnSelectionChanged -= ReachSelected;
        }
    }
}