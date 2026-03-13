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

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
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

    public void LaunchUp(float force)
    {
        _verticalVelocity = force;
    }
}