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
    [SerializeField] private Image goBackLoadUi; // The fill image for long press

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
        goBackLoadUi.gameObject.SetActive(false);
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

    public void SetLoadProgress(float percent)
    {
        if (percent <= 0)
        {
            goBackLoadUi.gameObject.SetActive(false);
        }
        else
        {
            goBackLoadUi.gameObject.SetActive(true);
            goBackLoadUi.fillAmount = percent;
        }
    }

    public void ShowError(string message)
    {
        roomNameError.text = message;
    }

    public void Show() => gameObject.SetActive(true);
    public void Hide() => gameObject.SetActive(false);
}