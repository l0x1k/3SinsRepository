using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerMovement : MonoBehaviour
{
    public bool IsOnGround => _controller.isGrounded;

    [SerializeField] private float _speed;
    [SerializeField] private float _rotationSpeed;
    [Header("Jump")]
    [SerializeField] private bool _useDoubleJump;
    [SerializeField] private float _jumpForce = 5;
    [SerializeField] private float _cayoteTime = 0.2f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [Header("Gravity")]
    [SerializeField] private float _gravityForce;
    [SerializeField] private float _maxFallVelocity;
    [Header("Ground check")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Vector3 _boxOffset = new Vector3(0, -0.7f, 0);
    [SerializeField] private Vector3 _boxHalfExtends = new Vector3(0.35f, 0.31f, 0.35f);

    private bool _isCanJump;

    public bool IsCanJump
    {
        get { return (CayoteTimeTimer > 0 && JumpBufferTimer > 0) || _isCanJump; }
        set { _isCanJump = value; }
    }

    #region Timers
    private float _cayoteTimeTimer;
    public float CayoteTimeTimer
    {
        get { return _cayoteTimeTimer; }
        private set { _cayoteTimeTimer = Mathf.Clamp(value, 0, _cayoteTime); }
    }

    private float _jumpBufferTimer;
    public float JumpBufferTimer
    {
        get { return _jumpBufferTimer; }
        private set { _jumpBufferTimer = Mathf.Clamp(value, 0, _jumpBufferTime); }
    }
    #endregion

    #region DoubleJumpProperty
    private bool _isCanDoubleJump;
    public bool IsCanDoubleJump
    {
        get
        {
            if (_useDoubleJump)
                return _isCanDoubleJump && JumpBufferTimer > 0;
            else
                return false;
        }

        private set { _isCanDoubleJump = value; }
    }
    #endregion 

    private float _horizontalVelocity;
    private float _verticalVelocity;

    private float _lastHorizontalInputValue = 1;
    private float _horizontalInput;

    private CharacterController _controller;

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        Inputs();
        JumpUpdate();
        RotationUpdate();
    }

    private void FixedUpdate()
    {
        MoveUpdate();
        GravityUpdate();

        CayoteTimeTimer -= Time.fixedDeltaTime;
        _controller.Move((_horizontalVelocity * Vector3.right + _verticalVelocity * Vector3.up) * Time.fixedDeltaTime);
    }

    private void Inputs()
    {
        _horizontalInput = Input.GetAxis("Horizontal");
    }

    private bool IsGrounded()
    {
        bool result = Physics.CheckBox(transform.position + _boxOffset, _boxHalfExtends, Quaternion.identity, _groundLayerMask);
        Physics.OverlapBox(transform.position + _boxOffset, _boxHalfExtends, Quaternion.identity, _groundLayerMask);
        return result;
    }

    private void MoveUpdate()
    {
        _horizontalVelocity = _horizontalInput * _speed;
    }

    private void JumpUpdate()
    {
        if (IsOnGround)
        {
            CayoteTimeTimer = _cayoteTime;
            IsCanDoubleJump = true;
        }

        //jump buffer
        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpBufferTimer = _jumpBufferTime;
        }
        else
        {
            JumpBufferTimer -= Time.deltaTime;
        }

        //if we press jump key and if we was on groun or if we can double jump
        if (IsCanJump || IsCanDoubleJump)
        {
            //if we jumping not from ground we set is can double jump to false
            if (IsCanJump == false)
                IsCanDoubleJump = false;

            JumpBufferTimer = 0;

            //Standart jump on set Y axis velocity
            _verticalVelocity = _jumpForce;
        }
        //set to zero cayote timer
        if (Input.GetKeyUp(KeyCode.Space) && _verticalVelocity > 0)
        {
            if (IsCanDoubleJump == true)
                CayoteTimeTimer = 0;
        }
    }

    private void GravityUpdate()
    {
        if (IsOnGround == false)
            _verticalVelocity = Mathf.Max(_verticalVelocity - _gravityForce * Time.fixedDeltaTime, -_maxFallVelocity);
    }

    private void RotationUpdate()
    {
        if (_horizontalInput > 0)
            _lastHorizontalInputValue = 1;
        if (_horizontalInput < 0)
            _lastHorizontalInputValue = -1;

        Quaternion neededRotation = Quaternion.LookRotation(Vector3.forward * _lastHorizontalInputValue, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, neededRotation, Time.deltaTime * _rotationSpeed);
    }

    private void OnDrawGizmos()
    {
        Color GizmoColor;
        GizmoColor = Color.red;
        GizmoColor.a = 125;

        Gizmos.color = GizmoColor;

        Gizmos.DrawCube(transform.position + _boxOffset, _boxHalfExtends * 2);
    }
}
