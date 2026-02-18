using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NewRoomUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Scrollbar zoomSlider;
    [SerializeField] private TMP_Text zoomText;
    [SerializeField] private RectTransform roomLayoutRect;
    [SerializeField] private Button confirmButton;
    [SerializeField] private TMP_Text roomNameError;
    [SerializeField] private LoadingCircle loadingCircle;
    [SerializeField] private MovableButton _background;

    // Events for the State to listen to
    public event Action OnConfirmClicked;
    public event Action<float> OnZoomChanged;

    private void Start()
    {
        confirmButton.onClick.AddListener(() => OnConfirmClicked?.Invoke());

        zoomSlider.onValueChanged.AddListener((val) =>
        {
            UpdateZoomVisuals(val); // Update text immediately
            OnZoomChanged?.Invoke(val); // Notify state if needed
        });

        // Init state
        UpdateZoomVisuals(zoomSlider.value);
        roomNameError.text = "";
    }

    // --- VISUAL METHODS (Called by State) ---

    public void UpdateZoomVisuals(float value)
    {
        int zoom = Mathf.RoundToInt(value * 100.0f);
        if (zoom == 100) zoom = 99;
        zoomText.text = $"Zoom: {zoom:00}%";
        roomLayoutRect.localScale = Vector3.one * (1 + value);
    }



    public void ShowError(string message)
    {
        roomNameError.text = message;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);

    public void UpdateLoadingCircle(float percent) => loadingCircle.SetLoadProgress(percent);

    private void OnDisable()
    {
        UpdateLoadingCircle(0);
    }

    public void MoveBackground(Vector2 mousePos) => _background.Move(mousePos);
}