using UnityEngine;
using Cinemachine;

[RequireComponent(typeof(CharacterController))]
public class CharacterControllerMovement : MonoBehaviour
{
    public bool IsOnGround => _controller.isGrounded;

    [SerializeField] private float _speed = 10;
    [SerializeField] private float _rotationSpeed = 10;
    [Header("Jump")]
    [SerializeField] private bool _useDoubleJump;
    [SerializeField] private float _jumpEndEarlyGravityModifier = 1.2f;
    [SerializeField] private float _jumpForce = 20;
    [SerializeField] private float _cayoteTime = 0.2f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [Header("Apex modifier")]
    [SerializeField] private float _apexThreshold = 0.5f;
    [SerializeField] private float _apexBonus = 2;
    [Header("Gravity")]
    [SerializeField] private float _gravityForce = 50;
    [SerializeField] private float _maxFallVelocity = 20;
    [Header("Ground check")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Vector3 _boxOffset = new Vector3(0, -0.7f, 0);
    [SerializeField] private Vector3 _boxHalfExtends = new Vector3(0.35f, 0.31f, 0.35f);

    private bool _isCanJump;

    public bool IsCanJump
    {
        get { return (CayoteTimeTimer > 0 && JumpBufferTimer > 0) || _isCanJump && JumpBufferTimer > 0; }
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

    private bool _isJumpEndedEarly;

    private float _horizontalVelocity;
    private float _verticalVelocity;

    private float _lastHorizontalInputValue = 1;
    private float _horizontalInput;

    private CharacterController _controller;

    public CinemachineVirtualCamera _cinemachineVirtualCamera;
    public float shakeIntensity = 1f;
    public float shakeDuration = 0.5f;
    private float shakeTimer;
    private CinemachineBasicMultiChannelPerlin noise;


    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        noise = _cinemachineVirtualCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();

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
        ApexBoostUpdate();

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

            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;
                noise.m_AmplitudeGain = Mathf.Lerp(shakeIntensity, 0.2f, 1f - (shakeTimer / shakeDuration));
            }


        }

        //if (_verticalVelocity < 0)
        //{
        //    _isJumpEndedEarly = false;
        //}

        if (Input.GetKeyDown(KeyCode.Space))
        {
            JumpBufferTimer = _jumpBufferTime;
        }
        else
        {
            JumpBufferTimer -= Time.deltaTime;
        }

        if (IsCanJump || IsCanDoubleJump)
        {
            if (IsCanJump == false)
                IsCanDoubleJump = false;

            JumpBufferTimer = 0;

            _verticalVelocity = _jumpForce;
        }

        if (Input.GetKey(KeyCode.Space))
        {
            _isJumpEndedEarly = false;
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            _isJumpEndedEarly = true;

            if (IsCanDoubleJump == true)
                CayoteTimeTimer = 0;
        }
    }

    private void GravityUpdate()
    {
        if (IsOnGround == false)
        {
            float modifiedGravityForce = _isJumpEndedEarly && _verticalVelocity > 0 ? _gravityForce * _jumpEndEarlyGravityModifier : _gravityForce;

            float velocity = Mathf.Max(_verticalVelocity - modifiedGravityForce * Time.fixedDeltaTime, -_maxFallVelocity);

            _verticalVelocity = velocity;

            ShakeCamera();
        }
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

    private void ApexBoostUpdate()
    {
        if (IsOnGround == false && _horizontalInput != 0)
        {
            float apexFactor = Mathf.InverseLerp(_apexThreshold, 0, Mathf.Abs(_verticalVelocity));
            float apexBoost = Mathf.Sign(_horizontalInput) * _apexBonus * apexFactor;
            _horizontalVelocity += apexBoost * Time.fixedDeltaTime;
        }
    }

    public void ShakeCamera()
    {
        shakeTimer = shakeDuration;

        noise.m_AmplitudeGain = Mathf.Lerp(0f, shakeIntensity, 1f - (shakeTimer / shakeDuration));
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
