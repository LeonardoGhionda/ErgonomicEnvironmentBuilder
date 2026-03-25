using TMPro;
using UnityEngine;

public class HM_BellyCalibration : HM_Base
{
    enum Fase
    {
        Init,
        Calibrate,
    }

    Fase _fase;
    BodyPointsManager _bpm;

    string _text;
    float _fontSize;
    TextMeshProUGUI _tmp;

    [SerializeField] private GameObject _lookPointPrefab;
    [SerializeField] private Camera _playerCamera;
    [SerializeField] private float _lookPointDistance = 10f;

    private GameObject _spawnedLookPoint;
    private HandMenuManager _handMenu;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _handMenu = Managers.Get<HandMenuManager>();
        _bpm = FindAnyObjectByType<BodyPointsManager>();
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        _text = _tmp.text;
        _fontSize = _tmp.fontSize;

        _fase = Fase.Init;
    }

    public override void OnClick()
    {
        if (_fase == Fase.Init)
        {
            _tmp.text = "Stand Up, look the yellow point in front of you and confirm";
            _tmp.enableAutoSizing = true;

            Vector3 camPos = _playerCamera.transform.position;
            Vector3 camForward = _playerCamera.transform.forward;
            Vector3 flatForward = new Vector3(camForward.x, 0f, camForward.z).normalized;
            Vector3 spawnPosition = camPos + (flatForward * _lookPointDistance);

            _spawnedLookPoint = Instantiate(_lookPointPrefab, spawnPosition, Quaternion.identity);

            _fase = Fase.Calibrate;

            _handMenu.Lock = true;
        }
        else if (_fase == Fase.Calibrate)
        {
            Destroy(_spawnedLookPoint);

            _tmp.text = _text;
            _tmp.enableAutoSizing = false;
            _tmp.fontSize = _fontSize;

            _bpm.Calibrate();
            _fase = Fase.Init;
            _handMenu.Lock = false;
            _handMenu.Show(false);
        }


    }


}

