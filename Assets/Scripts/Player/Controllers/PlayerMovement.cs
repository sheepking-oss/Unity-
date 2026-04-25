using UnityEngine;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Player.Controllers
{
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform _cameraTransform;
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _controller;

        [Header("Movement Settings")]
        [SerializeField] private float _walkSpeed = 4f;
        [SerializeField] private float _runSpeed = 8f;
        [SerializeField] private float _sprintSpeed = 12f;
        [SerializeField] private float _crouchSpeed = 2f;
        [SerializeField] private float _rotationSmoothTime = 0.1f;

        [Header("Gravity & Jumping")]
        [SerializeField] private float _gravity = -20f;
        [SerializeField] private float _jumpHeight = 2f;
        [SerializeField] private float _jumpCooldown = 0.5f;

        [Header("Ground Check")]
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private float _groundCheckDistance = 0.2f;
        [SerializeField] private Transform _groundCheckPoint;

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private Vector3 _velocity;
        private float _targetRotation;
        private float _rotationVelocity;
        private float _lastJumpTime;
        private bool _isGrounded;
        private bool _isCrouching;
        private bool _isSprinting;
        private float _moveSpeed;

        public Vector3 Velocity => _velocity;
        public float MoveSpeed => _moveSpeed;
        public bool IsGrounded => _isGrounded;
        public bool IsCrouching => _isCrouching;
        public bool IsSprinting => _isSprinting;
        public bool IsMoving => _inputManager != null && _inputManager.IsMoving;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponent<CharacterController>();

            _inputManager = InputManager.Instance;
            _gameStateManager = GameStateManager.Instance;
        }

        private void Start()
        {
            if (_cameraTransform == null)
                _cameraTransform = Camera.main?.transform;

            _lastJumpTime = -_jumpCooldown;
        }

        private void Update()
        {
            if (!CanMove()) return;

            CheckGrounded();
            HandleMovement();
            HandleRotation();
            HandleGravity();
            HandleJumping();
            ApplyMovement();
            UpdateAnimator();
        }

        private bool CanMove()
        {
            if (_gameStateManager == null) return true;
            return _gameStateManager.CanPlayerMove();
        }

        private void CheckGrounded()
        {
            if (_groundCheckPoint != null)
            {
                _isGrounded = Physics.CheckSphere(_groundCheckPoint.position, _groundCheckDistance, _groundLayers);
            }
            else
            {
                _isGrounded = _controller.isGrounded;
            }

            if (_isGrounded && _velocity.y < 0)
            {
                _velocity.y = -2f;
            }
        }

        private void HandleMovement()
        {
            float horizontal = _inputManager.Horizontal;
            float vertical = _inputManager.Vertical;

            Vector3 direction = new Vector3(horizontal, 0f, vertical).normalized;

            bool wantSprint = _inputManager.SprintHeld && _isGrounded && vertical > 0;
            bool wantCrouch = _inputManager.CrouchHeld && _isGrounded;

            _isSprinting = wantSprint && !wantCrouch;
            _isCrouching = wantCrouch;

            if (_isSprinting)
                _moveSpeed = _sprintSpeed;
            else if (_isCrouching)
                _moveSpeed = _crouchSpeed;
            else if (Input.GetKey(KeyCode.LeftShift) == false && _isGrounded)
                _moveSpeed = _runSpeed;
            else
                _moveSpeed = _walkSpeed;

            if (direction.magnitude >= 0.1f)
            {
                _targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg + _cameraTransform.eulerAngles.y;
            }
        }

        private void HandleRotation()
        {
            if (_cameraTransform == null) return;

            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _targetRotation, ref _rotationVelocity, _rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, rotation, 0f);
        }

        private void HandleGravity()
        {
            _velocity.y += _gravity * Time.deltaTime;
        }

        private void HandleJumping()
        {
            if (_inputManager.JumpPressed && _isGrounded && Time.time - _lastJumpTime >= _jumpCooldown)
            {
                _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _lastJumpTime = Time.time;
                EventManager.TriggerEvent("OnPlayerJump");
            }
        }

        private void ApplyMovement()
        {
            Vector3 targetDirection = Quaternion.Euler(0f, _targetRotation, 0f) * Vector3.forward;
            Vector3 moveDirection = targetDirection.normalized * _moveSpeed;

            _velocity = new Vector3(moveDirection.x, _velocity.y, moveDirection.z);

            if (_controller != null)
            {
                _controller.Move(_velocity * Time.deltaTime);
            }
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            float moveMagnitude = new Vector3(_inputManager.Horizontal, 0f, _inputManager.Vertical).normalized.magnitude;
            float animationSpeed = moveMagnitude;

            if (_isSprinting)
                animationSpeed *= 2f;
            else if (_isCrouching)
                animationSpeed *= 0.5f;

            _animator.SetFloat("MoveSpeed", animationSpeed);
            _animator.SetBool("IsGrounded", _isGrounded);
            _animator.SetBool("IsCrouching", _isCrouching);
            _animator.SetFloat("VerticalVelocity", _velocity.y);
        }

        public void SetMoveSpeedMultiplier(float multiplier)
        {
            _walkSpeed = 4f * multiplier;
            _runSpeed = 8f * multiplier;
            _sprintSpeed = 12f * multiplier;
            _crouchSpeed = 2f * multiplier;
        }

        public void ResetMoveSpeed()
        {
            _walkSpeed = 4f;
            _runSpeed = 8f;
            _sprintSpeed = 12f;
            _crouchSpeed = 2f;
        }

        private void OnDrawGizmosSelected()
        {
            if (_groundCheckPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(_groundCheckPoint.position, _groundCheckDistance);
            }
        }
    }
}
