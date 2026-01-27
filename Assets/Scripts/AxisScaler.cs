using UnityEngine;

public class AxisScaler : MonoBehaviour
{

    [SerializeField] float px;
    [SerializeField] float py;
    [SerializeField] float pz;

    [SerializeField] bool negativeFace = false;

    float _oldPX;
    float _oldPY;
    float _oldPZ;

    float _sign = -1f;

    float _min = 0.01f;


    BoxCollider _boxCollider;

    private void Start()
    {
        if (!TryGetComponent(out _boxCollider)) Debug.LogError("Box Collider is necessary");

        px = transform.localScale.x;
        py = transform.localScale.y;
        pz = transform.localScale.z;

        _oldPX = px;
        _oldPY = py;
        _oldPZ = pz;
    }

    void Update()
    {
        if (negativeFace) _sign = 1f;
        else _sign = -1f;

        if (px != _oldPX)
        {
            if (px < _min) px = _min;

            Vector3 worldFaceCenter = transform.TransformPoint(_boxCollider.center + new Vector3(_boxCollider.size.x * _sign * 0.5f, 0, 0));
            ScalerUtility.SetScaleAround(transform, worldFaceCenter, new(px, py, pz));
            _oldPX = px;
        }
        else if (py != _oldPY)
        {
            if(py < _min) py = _min;

            Vector3 worldFaceCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, _boxCollider.size.x * _sign * 0.5f, 0));
            ScalerUtility.SetScaleAround(transform, worldFaceCenter, new(px, py, pz));
            _oldPY = py;
        }
        else if (py != _oldPZ)
        {
            if(pz < _min) pz = _min;

            Vector3 worldFaceCenter = transform.TransformPoint(_boxCollider.center + new Vector3(0, 0, _boxCollider.size.x * _sign * 0.5f));
            ScalerUtility.SetScaleAround(transform, worldFaceCenter, new(px, py, pz));
            _oldPZ = pz;
        }
    }
}
