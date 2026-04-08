using System;
using UnityEngine;
using UnityEngine.UI;

public class OptionView : MonoBehaviour
{
    [SerializeField] private Slider CamSensibilitySlider;
    private CameraController _camController;


    // PlayerPrefs keys
    private const string CAM_SENSIBILITY_KEY = "CamSensibility";

    private void Start()
    {
        _camController = DependencyProvider.DTCamera.GetComponent<CameraController>();

        // camera look sensibility
        if (PlayerPrefs.HasKey(CAM_SENSIBILITY_KEY))
        {
            _camController.LookSpeed = PlayerPrefs.GetFloat(CAM_SENSIBILITY_KEY);
        }
        CamSensibilitySlider.value = _camController.LookSpeed;
        CamSensibilitySlider.onValueChanged.AddListener(OnCamSensibilityChanged);
    }

    private void OnCamSensibilityChanged(float arg0)
    {
        _camController.LookSpeed = arg0;
    }

    private void OnDisable()
    {
        // Save user settings
        PlayerPrefs.SetFloat(CAM_SENSIBILITY_KEY, _camController.LookSpeed);
    }
}