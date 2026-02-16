using System;
using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class HM_BellyMeasure : HM_Base
{
    enum Fase
    {
        Init,
        Calibrate,
        MeasureStart,
        MeasureUpdate,
    }

    Fase _fase;
    BodyPointsManager _bpm;
    MeasureManager _mm;

    string _text;
    float _fontSize;
    TextMeshProUGUI _tmp;

    protected override void OnInitialized()
    {
        base.OnInitialized();
        _bpm = FindAnyObjectByType<BodyPointsManager>();
        _mm = FindAnyObjectByType<MeasureManager>();
        _tmp = GetComponentInChildren<TextMeshProUGUI>();
        _text = _tmp.text;
        _fontSize = _tmp.fontSize;

        _fase = Fase.Init;
    }

    public override void OnClick()
    {
        if (_fase == Fase.Init)
        {
            _tmp.text = "1. Stand Up\n2.Place controller on your belly\n3. Look forward and confirm";
            _tmp.enableAutoSizing = true;

            _bpm.InitBellyButtonCalibration(_deps.handMenu.HandTransform);
            _fase = Fase.Calibrate;

            _deps.handMenu.Lock = true;
        }
        else if (_fase == Fase.Calibrate)
        {
            _tmp.text = _text;
            _tmp.enableAutoSizing = false;
            _tmp.fontSize = _fontSize;

            _bpm.Calibrate();
            _fase = Fase.MeasureStart;
        }

        if (_fase == Fase.MeasureStart)
        {
            _deps.handMenu.Show(false);
            _mm.Init(_deps.player.GetComponentInChildren<Camera>());
            _mm.OnMeasureEnd += MeasureEnd;

            _mm.StartMeasure(_bpm.BellyButton.position, _bpm.BellyButton);
        }
    }

    private void MeasureEnd()
    {
        _mm.OnMeasureEnd -= MeasureEnd;
        _deps.handMenu.Lock = false;
    }


}

