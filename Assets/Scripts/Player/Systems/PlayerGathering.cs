using UnityEngine;
using SurvivalGame.Player.Controllers;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Data.Items;
using SurvivalGame.Data.Resources;
using SurvivalGame.Core.Events;
using SurvivalGame.Core.Interfaces;

namespace SurvivalGame.Player.Systems
{
    public class PlayerGathering : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private PlayerInteraction _playerInteraction;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _gatherPoint;

        [Header("Gathering Settings")]
        [SerializeField] private float _gatherRange = 3f;
        [SerializeField] private float _baseGatherTime = 1f;
        [SerializeField] private float _staminaCostPerGather = 5f;
        [SerializeField] private LayerMask _gatherLayers;

        [Header("Tool Settings")]
        [SerializeField] private bool _useEquippedTool = true;
        [SerializeField] private ToolItemData _defaultTool;

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private bool _isGathering = false;
        private float _gatherTimer = 0f;
        private IDamagable _currentTarget;
        private GameObject _currentTargetObject;

        private ToolItemData _equippedTool;

        public bool IsGathering => _isGathering;
        public float GatherProgress => _isGathering ? _gatherTimer / GetCurrentGatherTime() : 0f;
        public IDamagable CurrentTarget => _currentTarget;

        private void Awake()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_playerInteraction == null)
                _playerInteraction = GetComponent<PlayerInteraction>();
            if (_animator == null)
                _animator = GetComponent<Animator>();

            _inputManager = InputManager.Instance;
            _gameStateManager = GameStateManager.Instance;

            if (_gatherPoint == null)
            {
                _gatherPoint = transform;
            }
        }

        private void Update()
        {
            if (!CanGather()) return;

            if (_isGathering)
            {
                UpdateGathering();
            }
            else
            {
                CheckForGatherInput();
            }
        }

        private bool CanGather()
        {
            if (_gameStateManager != null && _gameStateManager.CurrentState != GameState.Playing)
                return false;

            if (_playerController != null && !_playerController.IsAlive)
                return false;

            return true;
        }

        private void CheckForGatherInput()
        {
            if (_inputManager == null) return;

            if (_inputManager.AttackPressed)
            {
                TryStartGathering();
            }
        }

        private void TryStartGathering()
        {
            if (_playerController != null && !_playerController.HasEnoughStamina(_staminaCostPerGather))
            {
                Debug.Log("Not enough stamina to gather!");
                return;
            }

            IDamagable target = FindGatherTarget();
            if (target == null) return;

            ResourceNode resourceNode = (target as MonoBehaviour)?.GetComponent<ResourceNode>();
            if (resourceNode != null && resourceNode.NodeData != null)
            {
                ToolType requiredTool = resourceNode.NodeData.RequiredToolType;
                float requiredPower = resourceNode.NodeData.MinimumToolPower;

                float currentToolPower = GetCurrentToolPower(requiredTool);
                if (currentToolPower < requiredPower)
                {
                    Debug.Log($"Need better {requiredTool} to harvest this!");
                    return;
                }
            }

            StartGathering(target);
        }

        private IDamagable FindGatherTarget()
        {
            Vector3 origin = _gatherPoint != null ? _gatherPoint.position : transform.position;
            Vector3 forward = transform.forward;

            if (Physics.SphereCast(
                origin,
                0.5f,
                forward,
                out RaycastHit hit,
                _gatherRange,
                _gatherLayers,
                QueryTriggerInteraction.Collide))
            {
                IDamagable damagable = hit.collider.GetComponent<IDamagable>();
                if (damagable != null && damagable.IsAlive)
                {
                    return damagable;
                }
            }

            Collider[] colliders = Physics.OverlapSphere(origin, _gatherRange, _gatherLayers, QueryTriggerInteraction.Collide);
            IDamagable nearestTarget = null;
            float nearestDistance = float.MaxValue;

            foreach (Collider collider in colliders)
            {
                IDamagable damagable = collider.GetComponent<IDamagable>();
                if (damagable == null || !damagable.IsAlive) continue;

                float distance = Vector3.Distance(origin, collider.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearestTarget = damagable;
                }
            }

            return nearestTarget;
        }

        private void StartGathering(IDamagable target)
        {
            _isGathering = true;
            _gatherTimer = 0f;
            _currentTarget = target;
            _currentTargetObject = (target as MonoBehaviour)?.gameObject;

            _animator?.SetTrigger("StartGathering");

            EventManager.TriggerEvent("OnGatheringStarted", target);
            Debug.Log("Started gathering...");
        }

        private void UpdateGathering()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
            {
                CancelGathering();
                return;
            }

            Vector3 targetPosition = _currentTargetObject != null ?
                _currentTargetObject.transform.position : transform.position;

            float distanceToTarget = Vector3.Distance(transform.position, targetPosition);
            if (distanceToTarget > _gatherRange * 1.5f)
            {
                CancelGathering();
                return;
            }

            _gatherTimer += Time.deltaTime;
            float gatherTime = GetCurrentGatherTime();

            _animator?.SetFloat("GatherSpeed", 1f / gatherTime);

            if (_gatherTimer >= gatherTime)
            {
                CompleteGathering();
            }
        }

        private float GetCurrentGatherTime()
        {
            float gatherTime = _baseGatherTime;

            if (_useEquippedTool && _equippedTool != null)
            {
                gatherTime /= _equippedTool.Efficiency;
            }

            return gatherTime;
        }

        private float GetCurrentToolPower(ToolType requiredToolType)
        {
            if (_useEquippedTool && _equippedTool != null)
            {
                if (_equippedTool.ToolType == requiredToolType)
                {
                    return _equippedTool.MiningPower;
                }
            }

            if (_defaultTool != null && _defaultTool.ToolType == requiredToolType)
            {
                return _defaultTool.MiningPower;
            }

            return 0f;
        }

        private void CompleteGathering()
        {
            if (_playerController != null)
            {
                _playerController.ConsumeStamina(_staminaCostPerGather);
            }

            float damage = CalculateGatherDamage();
            _currentTarget?.TakeDamage(damage);

            _animator?.SetTrigger("GatherComplete");

            EventManager.TriggerEvent("OnGatheringComplete", _currentTarget);

            _isGathering = false;
            _gatherTimer = 0f;
        }

        private float CalculateGatherDamage()
        {
            float baseDamage = 10f;

            if (_useEquippedTool && _equippedTool != null)
            {
                baseDamage = _equippedTool.MiningPower * 10f;
            }

            return baseDamage;
        }

        private void CancelGathering()
        {
            _isGathering = false;
            _gatherTimer = 0f;
            _currentTarget = null;
            _currentTargetObject = null;

            _animator?.SetTrigger("CancelGathering");

            EventManager.TriggerEvent("OnGatheringCancelled");
        }

        public void EquipTool(ToolItemData tool)
        {
            _equippedTool = tool;
            EventManager.TriggerEvent("OnToolEquipped", tool);
        }

        public void UnequipTool()
        {
            _equippedTool = null;
            EventManager.TriggerEvent("OnToolUnequipped");
        }

        public ToolItemData GetEquippedTool()
        {
            return _equippedTool;
        }

        private void OnDrawGizmosSelected()
        {
            Vector3 origin = _gatherPoint != null ? _gatherPoint.position : transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(origin, _gatherRange);
        }
    }
}
