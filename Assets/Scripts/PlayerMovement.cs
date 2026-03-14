using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    [Header("Speed")]
    [SerializeField] private float walkSpeed = 5f;
    [SerializeField] private float runSpeed = 8f;
    [SerializeField] private float crouchSpeed = 2f;

    [Header("Acceleration")]
    [SerializeField] private float acceleration = 2000f;
    [SerializeField] private float deceleration = 3000f;

    [Header("Jump and Fall")] 
    [SerializeField] private float jumpForce = 7f;
    [SerializeField] private float gravity = -12f;
    [SerializeField] private float initialFallVelocity = -2f;

    [Header("Movement Sounds")]
    [SerializeField] private AudioSource movementAudioSource;
    [SerializeField, Range(0f, 1f)] private float movementSoundVolume = 1f;
    [SerializeField, Min(0.05f)] private float movementSoundInterval = 0.45f;
    [SerializeField] private AudioClip[] movementSounds;
    
    [Header("References")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference jumpAction;
    [SerializeField] private InputActionReference sprintAction;
    
    private CharacterController _characterController;
    private Vector2 _moveInput;
    private Vector3 _currentVelocity;
    private bool _isGrounded;
    private bool _isSprinting;
    private float _verticalVelocity;
    private float _movementSoundTimer;
    private int _lastMovementSoundIndex = -1;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        if (movementAudioSource == null)
            movementAudioSource = GetComponent<AudioSource>();
    }

    private void OnEnable()
    {
        moveAction.action.performed += StoreMovementInput;
        moveAction.action.canceled += StoreMovementInput;
        jumpAction.action.performed += Jump;
        sprintAction.action.performed += _ => _isSprinting = true;
        sprintAction.action.canceled += _ => _isSprinting = false;
    }

    private void OnDisable()
    {
        moveAction.action.performed -= StoreMovementInput;
        moveAction.action.canceled -= StoreMovementInput;
        jumpAction.action.performed -= Jump;
        sprintAction.action.performed -= _ => _isSprinting = true;
        sprintAction.action.canceled -= _ => _isSprinting = false;
    }

    private void Update()
    {
        _isGrounded = _characterController.isGrounded;
        HandleGravity();
        HandleMovement();
        HandleMovementSounds();
    }

    private void StoreMovementInput(InputAction.CallbackContext ctx)
    {
        _moveInput = ctx.ReadValue<Vector2>(); 
    }

    private void Jump(InputAction.CallbackContext ctx)
    {
        if (_isGrounded)
        {
            _verticalVelocity = jumpForce;
        }
    }

    private void HandleGravity()
    {
        if (_isGrounded && _verticalVelocity < 0)
        {
            _verticalVelocity = initialFallVelocity;
        }
        
        _verticalVelocity += gravity * Time.deltaTime;
    }
    
    private void HandleMovement()
    {
        var targetDirection = cameraTransform.TransformDirection(new Vector3(_moveInput.x, 0, _moveInput.y));
        targetDirection.y = 0;
        if (targetDirection.magnitude > 1f)
            targetDirection.Normalize();

        var currentSpeed = _isSprinting ? runSpeed : walkSpeed;
        var targetVelocity = targetDirection * currentSpeed;

        var rate = targetDirection.magnitude > 0f ? acceleration : deceleration;
        _currentVelocity = Vector3.MoveTowards(_currentVelocity, targetVelocity, rate * Time.deltaTime);

        var finalMove = _currentVelocity;
        finalMove.y = _verticalVelocity;
        
        var collisions = _characterController.Move(finalMove * Time.deltaTime);
        if ((collisions & CollisionFlags.Above) != 0)
        {
            _verticalVelocity = initialFallVelocity;
        }
    }

    private void HandleMovementSounds()
    {
        if (movementAudioSource == null || movementSounds == null || movementSounds.Length == 0)
            return;

        var horizontalSpeed = new Vector3(_currentVelocity.x, 0f, _currentVelocity.z).magnitude;
        var isMovingOnGround = _isGrounded && horizontalSpeed > 0.1f;

        if (!isMovingOnGround)
        {
            _movementSoundTimer = 0f;
            return;
        }

        _movementSoundTimer -= Time.deltaTime;
        if (_movementSoundTimer > 0f)
            return;

        var clipIndex = Random.Range(0, movementSounds.Length);
        if (movementSounds.Length > 1 && clipIndex == _lastMovementSoundIndex)
            clipIndex = (clipIndex + 1) % movementSounds.Length;

        var volumeScale = Mathf.Clamp01(movementSoundVolume);
        movementAudioSource.PlayOneShot(movementSounds[clipIndex], volumeScale);
        _lastMovementSoundIndex = clipIndex;
        _movementSoundTimer = movementSoundInterval;
    }
    
    public void TransformVelocityThroughPortal(Matrix4x4 portalMatrix)
    {
        // Reconstruct the full 3D velocity
        Vector3 fullVelocity = _currentVelocity;
        fullVelocity.y = _verticalVelocity;

        // Rotate it through the portal matrix (direction only, no translation)
        Vector3 transformedVelocity = portalMatrix.MultiplyVector(fullVelocity);

        // Split back into horizontal and vertical components
        _currentVelocity = new Vector3(transformedVelocity.x, 0f, transformedVelocity.z);
        _verticalVelocity = transformedVelocity.y;
    }

    public void LaunchUp(float force)
    {
        _verticalVelocity = force;
    }
}