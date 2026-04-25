using UnityEngine;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Player.Controllers
{
    public class PlayerInteraction : MonoBehaviour
    {
        [Header("Interaction Settings")]
        [SerializeField] private float _interactionRange = 3f;
        [SerializeField] private float _interactionRadius = 0.5f;
        [SerializeField] private LayerMask _interactionLayers;
        [SerializeField] private Transform _interactionOrigin;

        [Header("Highlight Settings")]
        [SerializeField] private Color _highlightColor = Color.yellow;
        [SerializeField] private float _highlightDuration = 0.1f;

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private IInteractable _currentInteractable;
        private GameObject _currentInteractableObject;
        private float _lastInteractionTime;

        public IInteractable CurrentInteractable => _currentInteractable;
        public bool HasInteractable => _currentInteractable != null;

        private void Awake()
        {
            _inputManager = InputManager.Instance;
            _gameStateManager = GameStateManager.Instance;

            if (_interactionOrigin == null)
            {
                _interactionOrigin = transform;
            }
        }

        private void Update()
        {
            if (!CanInteract()) return;

            FindInteractable();

            if (_inputManager.InteractPressed && _currentInteractable != null)
            {
                TryInteract();
            }
        }

        private bool CanInteract()
        {
            if (_gameStateManager == null) return true;
            return _gameStateManager.CurrentState == GameState.Playing;
        }

        private void FindInteractable()
        {
            IInteractable nearestInteractable = null;
            GameObject nearestObject = null;
            float nearestDistance = float.MaxValue;

            Vector3 origin = _interactionOrigin != null ? _interactionOrigin.position : transform.position;
            Vector3 forward = _interactionOrigin != null ? _interactionOrigin.forward : transform.forward;

            if (Physics.SphereCast(
                origin,
                _interactionRadius,
                forward,
                out RaycastHit hit,
                _interactionRange,
                _interactionLayers,
                QueryTriggerInteraction.Collide))
            {
                IInteractable interactable = hit.collider.GetComponent<IInteractable>();
                if (interactable != null && interactable.CanInteract(gameObject))
                {
                    nearestInteractable = interactable;
                    nearestObject = hit.collider.gameObject;
                    nearestDistance = hit.distance;
                }
            }

            Collider[] colliders = Physics.OverlapSphere(origin, _interactionRange, _interactionLayers, QueryTriggerInteraction.Collide);
            foreach (Collider collider in colliders)
            {
                IInteractable interactable = collider.GetComponent<IInteractable>();
                if (interactable == null) continue;
                if (!interactable.CanInteract(gameObject)) continue;

                float distance = Vector3.Distance(origin, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestInteractable = interactable;
                    nearestObject = collider.gameObject;
                    nearestDistance = distance;
                }
            }

            if (nearestInteractable != _currentInteractable)
            {
                if (_currentInteractable != null)
                {
                    OnInteractableLost();
                }

                _currentInteractable = nearestInteractable;
                _currentInteractableObject = nearestObject;

                if (_currentInteractable != null)
                {
                    OnInteractableFound();
                }
            }
        }

        private void OnInteractableFound()
        {
            Debug.Log($"Found interactable: {_currentInteractable.GetInteractionText()}");
            EventManager.TriggerEvent("OnInteractableFound", _currentInteractable);
        }

        private void OnInteractableLost()
        {
            EventManager.TriggerEvent("OnInteractableLost", _currentInteractable);
        }

        private void TryInteract()
        {
            if (_currentInteractable == null) return;
            if (!_currentInteractable.CanInteract(gameObject)) return;

            _currentInteractable.Interact(gameObject);
            _lastInteractionTime = Time.time;

            EventManager.TriggerEvent("OnPlayerInteracted", _currentInteractable);
        }

        public string GetCurrentInteractionText()
        {
            return _currentInteractable?.GetInteractionText() ?? string.Empty;
        }

        public void SetInteractionRange(float range)
        {
            _interactionRange = Mathf.Max(0.1f, range);
        }

        public float GetInteractionRange()
        {
            return _interactionRange;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = _interactionOrigin != null ? _interactionOrigin.position : transform.position;
            Vector3 forward = _interactionOrigin != null ? _interactionOrigin.forward : transform.forward;

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(origin, _interactionRadius);
            Gizmos.DrawLine(origin, origin + forward * _interactionRange);
            Gizmos.DrawWireSphere(origin + forward * _interactionRange, _interactionRadius);

            Gizmos.color = new Color(0f, 1f, 1f, 0.1f);
            Gizmos.DrawSphere(origin, _interactionRange);
        }
    }
}
