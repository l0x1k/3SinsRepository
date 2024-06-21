using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class RigidBodyMovement : MonoBehaviour
{
    //also property
    public bool IsOnGround => IsGrounded();

    #region Fields

    [SerializeField] private float _speed = 5;
    [SerializeField] private float _rotationSpeed = 10;
    [Header("Jump")]
    [SerializeField] private bool _useDoubleJump;
    [SerializeField] private float _jumpForce = 5;
    [SerializeField] private float _cayoteTime = 0.2f;
    [SerializeField] private float _jumpBufferTime = 0.2f;
    [SerializeField] private ParticleSystem _jumpParticleSystem;
    [Header("Slope check")]
    [SerializeField] private float _maxSlopeAngle = 60;
    [SerializeField] private Vector3 _slopeCheckerOffset;
    [SerializeField] private float _slopeCheckerLength = 0.51f;
    [Header("Ground check")]
    [SerializeField] private LayerMask _groundLayerMask;
    [SerializeField] private Vector3 _boxOffset = new Vector3(0, -0.7f, 0);
    [SerializeField] private Vector3 _boxHalfExtends = new Vector3(0.35f, 0.31f, 0.35f);

    private int _lastHorizontalInputValue = 1;

    private float _horizontalInput;

    private Rigidbody _rb;
    #endregion

    #region Properties
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
                return _isCanDoubleJump;
            else
                return false;
        }

        private set { _isCanDoubleJump = value; }
    }
    #endregion 
    #endregion

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        CayoteTimeTimer -= Time.deltaTime;

        RotationUpdate();
        InputUpdate();
        JumpUpdate();
    }

    private void FixedUpdate()
    {
        MoveUpdate();
    }

    //GettingInputs
    private void InputUpdate()
    {
        _horizontalInput = Input.GetAxisRaw("Horizontal");
    }

    #region Checkers
    private bool IsGrounded()
    {
        //Checking ground using checkbox
        //We doing that because checkbox working better then raycast with slope surfaces
        bool result = Physics.CheckBox(transform.position + _boxOffset, _boxHalfExtends, Quaternion.identity, _groundLayerMask);
        return result;
    }

    //Checking if in front of player slope with angle greater then moving angle limit
    private bool IsCanMoveOnSlopeForward()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position + _slopeCheckerOffset, Vector3.right * Mathf.Ceil(_horizontalInput), out hit, _slopeCheckerLength);

        if (Vector3.Angle(hit.normal, Vector3.up) > _maxSlopeAngle)
            return false;
        else
            return true;
    }
    #endregion

    #region Calculations
    //!!!PAY ATTENTION!!!: this gets normal of ground in 100 units under player
    private Vector3 GetGroundNormal()
    {
        RaycastHit hit;
        Physics.Raycast(transform.position + _slopeCheckerOffset, Vector3.down, out hit, 100, _groundLayerMask);
        return hit.normal;
    }

    private Vector3 CalculateProjectOnSlope(Vector3 direction, Vector3 normal)
    {
        //this formula that you can deduce mathematically to get vector project on slope
        return direction - Vector3.Dot(direction, normal) * normal;
    } 
    #endregion

    #region Updates
    private void MoveUpdate()
    {
        //it's condition need if wee want to limit max slope angle
        if (IsCanMoveOnSlopeForward())
        {
            Vector3 direction = Vector3.right * _horizontalInput;
            Vector3 directionAlongSurface;

            //if we on ground we calculating directionAlongSurface depending on ground normal, but if we in air we calculating depending on flat surface normal (Vector3.up)
            //We doing that because method GetGroundNormal gets normal of ground in 100 units under player
            if (IsOnGround)
            {
                directionAlongSurface = CalculateProjectOnSlope(direction, GetGroundNormal());
            }
            else
            {
                directionAlongSurface = CalculateProjectOnSlope(direction, Vector3.up);
            }

            Vector3 velocity = directionAlongSurface * _speed * Time.fixedDeltaTime;
            _rb.MovePosition(transform.position + velocity);
        }

        //doing this to prevent bug
        _rb.velocity = Vector3.up * _rb.velocity.y;
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
        if (JumpBufferTimer > 0 && (_cayoteTimeTimer > 0 || IsCanDoubleJump))
        {
            //if we jumping not from ground we set is can double jump to true
            if (JumpBufferTimer <= 0 || _cayoteTimeTimer <= 0)
                IsCanDoubleJump = false;

            //Standart jump on set Y axis velocity
            _rb.velocity = new Vector3(_rb.velocity.x, _jumpForce);
            JumpBufferTimer = 0;

            //Spawn particle on Jump
            Instantiate(_jumpParticleSystem, transform);
        }
        //set to zero cayote timer
        if (Input.GetKeyUp(KeyCode.Space) && _rb.velocity.y > 0)
        {
            if (IsCanDoubleJump == true)
                CayoteTimeTimer = 0;
        }
    }

    //rotating player to position, where position == transform.position + last moving direction
    private void RotationUpdate()
    {
        int intInput = (int)Mathf.Ceil(_horizontalInput);
        if (intInput != 0)
            _lastHorizontalInputValue = intInput;

        Quaternion neededRotation = Quaternion.LookRotation(Vector3.forward * _lastHorizontalInputValue, Vector3.up);

        transform.rotation = Quaternion.Lerp(transform.rotation, neededRotation, Time.deltaTime * _rotationSpeed);
    } 
    #endregion

    //just drawing some gizmos (red cube and red ray in scene and in game if we enabel gizmos)
    private void OnDrawGizmos()
    {
        Color GizmoColor;
        GizmoColor = Color.red;
        GizmoColor.a = 125;

        Gizmos.color = GizmoColor;

        Gizmos.DrawCube(transform.position + _boxOffset, _boxHalfExtends * 2);
        Gizmos.DrawRay(transform.position + _slopeCheckerOffset, Vector3.right * (int)_horizontalInput);
        Gizmos.DrawRay(transform.position + _slopeCheckerOffset, Vector3.right * (int)_horizontalInput);
    }
}