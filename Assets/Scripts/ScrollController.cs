using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(ScrollRect))]
public class ScrollController : MonoBehaviour
{
    private ScrollRect _scrollRect;

    [Header("Settings")]
    [SerializeField] private float _scrollStep = 0.2f; // How much to scroll per click (0.0 to 1.0)
    [SerializeField] private float _smoothSpeed = 10f;

    private float _targetPosition = 1f; // Start at top

    private void Awake()
    {
        _scrollRect = GetComponent<ScrollRect>();
    }

    private void OnEnable()
    {
        // Reset to top when menu opens
        _targetPosition = 1f;
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Update()
    {
        // Smoothly interpolate current position to target position
        if (Mathf.Abs(_scrollRect.verticalNormalizedPosition - _targetPosition) > 0.001f)
        {
            _scrollRect.verticalNormalizedPosition = Mathf.Lerp(
                _scrollRect.verticalNormalizedPosition,
                _targetPosition,
                Time.deltaTime * _smoothSpeed
            );
        }
    }

    // Connect this to your "Scroll Up" Button
    public void ScrollUp()
    {
        _targetPosition += _scrollStep;
        _targetPosition = Mathf.Clamp01(_targetPosition); // Keep between 0 and 1
    }

    // Connect this to your "Scroll Down" Button
    public void ScrollDown()
    {
        _targetPosition -= _scrollStep;
        _targetPosition = Mathf.Clamp01(_targetPosition);
    }
}