using UnityEngine;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Utilities;

namespace SurvivalGame.Player.Controllers
{
    public class ThirdPersonCamera : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform _target;
        [SerializeField] private Vector3 _offset = new Vector3(0f, 2f, -5f);

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationSpeed = 2f;
        [SerializeField] private float _minPitch = -30f;
        [SerializeField] private float _maxPitch = 60f;
        [SerializeField] private bool _invertY = false;

        [Header("Collision Settings")]
        [SerializeField] private bool _enableCollision = true;
        [SerializeField] private LayerMask _collisionLayers;
        [SerializeField] private float _collisionRadius = 0.3f;
        [SerializeField] private float _minDistance = 1f;

        [Header("Smooth Settings")]
        [SerializeField] private float _positionSmoothTime = 0.1f;
        [SerializeField] private float _rotationSmoothTime = 0.05f;

        [Header("Zoom Settings")]
        [SerializeField] private bool _enableZoom = true;
        [SerializeField] private float _zoomSpeed = 10f;
        [SerializeField] private float _minZoom = 2f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _defaultZoom = 5f;

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private float _currentYaw;
        private float _currentPitch;
        private float _currentZoom;
        private Vector3 _currentPosition;
        private Vector3 _positionVelocity;
        private float _yawVelocity;
        private float _pitchVelocity;
        private float _zoomVelocity;

        private Transform _transform;

        public Transform Target => _target;
        public float CurrentYaw => _currentYaw;
        public float CurrentPitch => _currentPitch;

        private void Awake()
        {
            _transform = transform;
            _inputManager = InputManager.Instance;
            _gameStateManager = GameStateManager.Instance;

            _currentZoom = _defaultZoom;
        }

        private void Start()
        {
            if (_target == null)
            {
                GameObject player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    _target = player.transform;
                }
            }

            if (_target != null)
            {
                _currentYaw = _target.eulerAngles.y;
                _currentPitch = 0f;
            }
        }

        private void LateUpdate()
        {
            if (_target == null) return;

            HandleRotation();
            HandleZoom();

            Vector3 targetPosition = CalculateTargetPosition();

            if (_enableCollision)
            {
                targetPosition = AdjustForCollision(targetPosition);
            }

            SmoothMove(targetPosition);
            SmoothLookAt(_target.position + Vector3.up * _offset.y);
        }

        private void HandleRotation()
        {
            if (_gameStateManager != null && _gameStateManager.IsInUI())
                return;

            float mouseX = _inputManager?.MouseX ?? 0f;
            float mouseY = _inputManager?.MouseY ?? 0f;

            if (_invertY)
                mouseY = -mouseY;

            _currentYaw += mouseX * _rotationSpeed;
            _currentPitch -= mouseY * _rotationSpeed;

            _currentPitch = MathUtilities.ClampAngle(_currentPitch, _minPitch, _maxPitch);
        }

        private void HandleZoom()
        {
            if (!_enableZoom) return;

            float scrollInput = Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scrollInput) > 0.01f)
            {
                _currentZoom -= scrollInput * _zoomSpeed;
                _currentZoom = Mathf.Clamp(_currentZoom, _minZoom, _maxZoom);
            }
        }

        private Vector3 CalculateTargetPosition()
        {
            Quaternion rotation = Quaternion.Euler(_currentPitch, _currentYaw, 0f);
            Vector3 offset = new Vector3(_offset.x, _offset.y, -_currentZoom);
            return _target.position + rotation * offset;
        }

        private Vector3 AdjustForCollision(Vector3 targetPosition)
        {
            Vector3 targetCenter = _target.position + Vector3.up * _offset.y;
            Vector3 direction = targetPosition - targetCenter;
            float distance = direction.magnitude;

            if (Physics.SphereCast(
                targetCenter,
                _collisionRadius,
                direction.normalized,
                out RaycastHit hit,
                distance,
                _collisionLayers,
                QueryTriggerInteraction.Ignore))
            {
                float adjustedDistance = hit.distance - _collisionRadius;
                adjustedDistance = Mathf.Max(adjustedDistance, _minDistance);
                return targetCenter + direction.normalized * adjustedDistance;
            }

            return targetPosition;
        }

        private void SmoothMove(Vector3 targetPosition)
        {
            _currentPosition = Vector3.SmoothDamp(
                _transform.position,
                targetPosition,
                ref _positionVelocity,
                _positionSmoothTime);

            _transform.position = _currentPosition;
        }

        private void SmoothLookAt(Vector3 lookTarget)
        {
            Vector3 direction = lookTarget - _transform.position;
            if (direction.sqrMagnitude < 0.01f) return;

            Quaternion targetRotation = Quaternion.LookRotation(direction);

            _transform.rotation = Quaternion.Lerp(
                _transform.rotation,
                targetRotation,
                1f - Mathf.Exp(-_rotationSmoothTime * Time.deltaTime));
        }

        public void SetTarget(Transform newTarget)
        {
            _target = newTarget;
            if (newTarget != null)
            {
                _currentYaw = newTarget.eulerAngles.y;
            }
        }

        public void ResetRotation()
        {
            if (_target != null)
            {
                _currentYaw = _target.eulerAngles.y;
                _currentPitch = 0f;
            }
        }

        public void SetZoom(float zoom)
        {
            _currentZoom = Mathf.Clamp(zoom, _minZoom, _maxZoom);
        }

        public void ResetZoom()
        {
            _currentZoom = _defaultZoom;
        }
    }
}
