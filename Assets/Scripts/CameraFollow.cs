using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private Transform _followObject;
    [SerializeField] private float _followSmoothness;
    [Header("Borders")]
    [SerializeField, Range(0, 1)] private float _leftBorder;
    [SerializeField, Range(0, 1)] private float _rightBorder;
    [SerializeField, Range(0, 1)] private float _topBorder;
    [SerializeField, Range(0, 1)] private float _bottomBorder;

    private bool _moveLeft;
    private bool _moveRight;
    private bool _moveUp;
    private bool _moveDown;


    private void FixedUpdate()
    {
        FollowUpdate();
    }

    private void FollowUpdate()
    {
        Vector3 objectViewportPoint = _camera.WorldToViewportPoint(_followObject.position);

        if (objectViewportPoint.x <= _leftBorder)
            _moveLeft = true;
        else
            _moveLeft = false;

        if (objectViewportPoint.x >= 1 - _rightBorder)
            _moveRight = true;
        else
            _moveRight = false;

        if (objectViewportPoint.y >= 1 - _topBorder)
            _moveUp = true;
        else
            _moveUp = false;

        if (objectViewportPoint.y <= _bottomBorder)
            _moveDown = true;
        else
            _moveDown = false;

        if (_moveLeft)
        {
            float position = transform.position.x + objectViewportPoint.x - _leftBorder;
            float LerpedPosition = Mathf.Lerp(transform.position.x, position, Time.deltaTime * _followSmoothness);

            transform.position = new Vector3(LerpedPosition, transform.position.y, transform.position.z);
        }
        if (_moveUp)
        {
            float position = transform.position.y + objectViewportPoint.y - 1 + _topBorder;
            float LerpedPosition = Mathf.Lerp(transform.position.y, position, Time.deltaTime * _followSmoothness);

            transform.position = new Vector3(transform.position.x , LerpedPosition, transform.position.z);
        }
        if (_moveRight)
        {
            float position = transform.position.x + objectViewportPoint.x - 1 + _rightBorder;
            float LerpedPosition = Mathf.Lerp(transform.position.x, position, Time.deltaTime * _followSmoothness);

            transform.position = new Vector3(LerpedPosition, transform.position.y, transform.position.z);
        }
        if (_moveDown)
        {
            float position = transform.position.y + objectViewportPoint.y - _bottomBorder;
            float LerpedPosition = Mathf.Lerp(transform.position.y, position, Time.deltaTime * _followSmoothness);

            transform.position = new Vector3(transform.position.x, LerpedPosition, transform.position.z);
        }
    }
}