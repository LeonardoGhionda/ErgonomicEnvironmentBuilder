using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SimpleAnimations : MonoBehaviour
{
    // Animations
    bool _groundEnter = false;
    bool _groundExit = false;

    Collider _collider;

    [SerializeField] float _speed = 1.0f;

    private void Start()
    {
        _collider = GetComponent<Collider>();
    }

    public void GroundEnter()
    {
        gameObject.SetActive(true);
        _groundEnter = true;
    }
    public void GroundExit() => _groundExit = true;


    private void FixedUpdate()
    {
        if (_groundEnter)
        {
            if (transform.position.y < _collider.bounds.size.y / 2)
            {
                Vector3 newPos =
                    transform.position + 
                    _speed * Time.deltaTime * Vector3.up;

                transform.position = newPos;
            }
            else
            {
                //stop animation
                _groundEnter = false;
            }
        }

        if (_groundExit)
        {
            if (transform.position.y > -_collider.bounds.size.y / 2)
            {
                Vector3 newPos =
                    transform.position +
                    _speed * Time.deltaTime * Vector3.down;

                transform.position = newPos;
            }
            else
            {
                gameObject.SetActive(false);
                //stop animation
                _groundExit = false;
            }
        }
    }
}
