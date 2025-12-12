using UnityEngine;

public class Handle : MonoBehaviour
{
    [SerializeField] Vector3 _direction;
    Vector3 Direction => _direction;
}
