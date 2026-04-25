using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Data.Enemies;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;
using SurvivalGame.World.Environment;

namespace SurvivalGame.Characters.Enemies
{
    public enum EnemyState
    {
        Idle,
        Patrol,
        Wander,
        Alert,
        Chase,
        Attack,
        Retreat,
        Dead
    }

    [RequireComponent(typeof(CharacterController), typeof(Collider))]
    public class Enemy : MonoBehaviour, IDamagable
    {
        [Header("Enemy Data")]
        [SerializeField] private EnemyData _enemyData;
        [SerializeField] private string _enemyID;

        [Header("References")]
        [SerializeField] private CharacterController _controller;
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _target;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        private float _currentHealth;
        private EnemyState _currentState = EnemyState.Idle;
        private float _stateTimer = 0f;
        private float _attackTimer = 0f;

        private Vector3 _moveDirection;
        private Vector3 _targetPosition;
        private Vector3 _homePosition;
        private Vector3 _patrolTarget;

        private float _currentMoveSpeed;
        private float _rotationVelocity;
        private float _verticalVelocity;

        private bool _isInitialized = false;
        private Transform _player;

        public EnemyData EnemyData => _enemyData;
        public string EnemyID => _enemyID;
        public EnemyState CurrentState => _currentState;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _enemyData?.MaxHealth ?? 0f;
        public bool IsAlive => _currentHealth > 0f;
        public Transform Target => _target;
        public float DistanceToTarget => _target != null ? Vector3.Distance(transform.position, _target.position) : float.MaxValue;

        public event System.Action<Enemy> OnEnemyDeath;
        public event System.Action<Enemy> OnEnemyDamaged;

        private void Awake()
        {
            if (_controller == null)
                _controller = GetComponent<CharacterController>();

            if (string.IsNullOrEmpty(_enemyID))
            {
                _enemyID = $"enemy_{GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            }

            _player = GameObject.FindGameObjectWithTag("Player")?.transform;
        }

        private void Start()
        {
            if (_enemyData != null && !_isInitialized)
            {
                Initialize(_enemyData);
            }

            _homePosition = transform.position;
        }

        public void Initialize(EnemyData data)
        {
            _enemyData = data;
            _currentHealth = data.MaxHealth;
            _currentMoveSpeed = data.MoveSpeed;
            _isInitialized = true;

            ChangeState(EnemyState.Idle);
        }

        private void Update()
        {
            if (_enemyData == null || !_isInitialized) return;
            if (_currentState == EnemyState.Dead) return;

            HandleDayNightBehavior();
            UpdateStateMachine();
            UpdateMovement();
            UpdateAnimator();
        }

        private void HandleDayNightBehavior()
        {
            if (_enemyData == null) return;

            if (_enemyData.IsNocturnal)
            {
                if (TimeManager.Instance != null && TimeManager.Instance.IsDay)
                {
                    if (_target != null)
                    {
                        _target = null;
                    }
                }
            }

            if (_enemyData.AfraidOfLight)
            {
                if (IsInLight())
                {
                    if (_target != null)
                    {
                        RetreatFromTarget();
                    }
                }
            }
        }

        private bool IsInLight()
        {
            return TimeManager.Instance != null && TimeManager.Instance.IsDay && TimeManager.Instance.SunHeightPercent > 0.3f;
        }

        private void UpdateStateMachine()
        {
            _stateTimer += Time.deltaTime;

            switch (_currentState)
            {
                case EnemyState.Idle:
                    HandleIdleState();
                    break;
                case EnemyState.Patrol:
                    HandlePatrolState();
                    break;
                case EnemyState.Wander:
                    HandleWanderState();
                    break;
                case EnemyState.Alert:
                    HandleAlertState();
                    break;
                case EnemyState.Chase:
                    HandleChaseState();
                    break;
                case EnemyState.Attack:
                    HandleAttackState();
                    break;
                case EnemyState.Retreat:
                    HandleRetreatState();
                    break;
            }
        }

        private void HandleIdleState()
        {
            _moveDirection = Vector3.zero;

            if (CheckForTarget())
            {
                ChangeState(EnemyState.Alert);
                return;
            }

            if (_stateTimer >= _enemyData.PatrolWaitTime)
            {
                if (_enemyData.CanPatrol && _enemyData.BehaviorType == EnemyBehaviorType.Patrol)
                {
                    SetNewPatrolTarget();
                    ChangeState(EnemyState.Patrol);
                }
                else
                {
                    SetWanderTarget();
                    ChangeState(EnemyState.Wander);
                }
            }
        }

        private void HandlePatrolState()
        {
            if (CheckForTarget())
            {
                ChangeState(EnemyState.Alert);
                return;
            }

            MoveToTarget(_patrolTarget);

            float distanceToTarget = Vector3.Distance(transform.position, _patrolTarget);
            if (distanceToTarget <= 1f)
            {
                ChangeState(EnemyState.Idle);
            }
        }

        private void HandleWanderState()
        {
            if (CheckForTarget())
            {
                ChangeState(EnemyState.Alert);
                return;
            }

            MoveToTarget(_targetPosition);

            float distanceToTarget = Vector3.Distance(transform.position, _targetPosition);
            if (distanceToTarget <= 1f || _stateTimer >= 5f)
            {
                ChangeState(EnemyState.Idle);
            }
        }

        private void HandleAlertState()
        {
            _moveDirection = Vector3.zero;

            if (_target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            LookAtTarget();

            if (_stateTimer >= 0.5f)
            {
                if (DistanceToTarget <= _enemyData.AttackRange)
                {
                    ChangeState(EnemyState.Attack);
                }
                else
                {
                    ChangeState(EnemyState.Chase);
                }
            }
        }

        private void HandleChaseState()
        {
            if (_target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            if (DistanceToTarget > _enemyData.ForgetRange)
            {
                _target = null;
                ChangeState(EnemyState.Idle);
                return;
            }

            MoveToTarget(_target.position);

            if (DistanceToTarget <= _enemyData.AttackRange)
            {
                ChangeState(EnemyState.Attack);
            }
        }

        private void HandleAttackState()
        {
            if (_target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            _moveDirection = Vector3.zero;
            LookAtTarget();

            if (DistanceToTarget > _enemyData.AttackRange)
            {
                ChangeState(EnemyState.Chase);
                return;
            }

            _attackTimer -= Time.deltaTime;
            if (_attackTimer <= 0f)
            {
                PerformAttack();
                _attackTimer = _enemyData.AttackCooldown;
            }
        }

        private void HandleRetreatState()
        {
            if (_target == null)
            {
                ChangeState(EnemyState.Idle);
                return;
            }

            Vector3 retreatDirection = transform.position - _target.position;
            retreatDirection.y = 0f;
            retreatDirection.Normalize();

            _moveDirection = retreatDirection;
            _currentMoveSpeed = _enemyData.MoveSpeed * 1.5f;

            if (DistanceToTarget > _enemyData.ForgetRange || _stateTimer >= 3f)
            {
                _target = null;
                _currentMoveSpeed = _enemyData.MoveSpeed;
                ChangeState(EnemyState.Idle);
            }
        }

        private void ChangeState(EnemyState newState)
        {
            _currentState = newState;
            _stateTimer = 0f;

            switch (newState)
            {
                case EnemyState.Idle:
                    _currentMoveSpeed = _enemyData.MoveSpeed * 0.5f;
                    break;
                case EnemyState.Patrol:
                case EnemyState.Wander:
                    _currentMoveSpeed = _enemyData.MoveSpeed * 0.7f;
                    break;
                case EnemyState.Chase:
                    _currentMoveSpeed = _enemyData.MoveSpeed * 1.2f;
                    PlayIdleSound();
                    break;
                case EnemyState.Attack:
                    _currentMoveSpeed = 0f;
                    _attackTimer = 0f;
                    break;
            }
        }

        private bool CheckForTarget()
        {
            if (_player == null)
                _player = GameObject.FindGameObjectWithTag("Player")?.transform;

            if (_player == null) return false;

            float distance = Vector3.Distance(transform.position, _player.position);

            if (distance <= _enemyData.DetectionRange)
            {
                if (HasLineOfSight(_player))
                {
                    _target = _player;
                    return true;
                }
            }

            return false;
        }

        private bool HasLineOfSight(Transform target)
        {
            Vector3 direction = target.position - transform.position;
            float distance = direction.magnitude;

            if (Physics.Raycast(transform.position + Vector3.up, direction.normalized, distance, LayerMask.GetMask("Default", "Environment")))
            {
                return false;
            }

            return true;
        }

        private void MoveToTarget(Vector3 targetPos)
        {
            Vector3 direction = targetPos - transform.position;
            direction.y = 0f;
            direction.Normalize();

            _moveDirection = direction;

            if (direction != Vector3.zero)
            {
                float targetRotation = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.SmoothDamp(
                    transform.rotation,
                    Quaternion.Euler(0f, targetRotation, 0f),
                    ref _rotationVelocity,
                    0.1f);
            }
        }

        private void LookAtTarget()
        {
            if (_target == null) return;

            Vector3 direction = _target.position - transform.position;
            direction.y = 0f;
            direction.Normalize();

            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.SmoothDamp(
                    transform.rotation,
                    Quaternion.LookRotation(direction),
                    ref _rotationVelocity,
                    0.1f);
            }
        }

        private void SetNewPatrolTarget()
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(2f, _enemyData.PatrolRadius);

            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance
            );

            _patrolTarget = _homePosition + offset;
        }

        private void SetWanderTarget()
        {
            float angle = Random.Range(0f, 360f);
            float distance = Random.Range(1f, 5f);

            Vector3 offset = new Vector3(
                Mathf.Sin(angle * Mathf.Deg2Rad) * distance,
                0f,
                Mathf.Cos(angle * Mathf.Deg2Rad) * distance
            );

            _targetPosition = transform.position + offset;
        }

        private void PerformAttack()
        {
            if (_target == null) return;

            IDamagable damagable = _target.GetComponent<IDamagable>();
            if (damagable != null)
            {
                if (IsTargetInAttackAngle())
                {
                    damagable.TakeDamage(_enemyData.Damage);
                    PlayAttackSound();

                    if (_animator != null)
                    {
                        _animator.SetTrigger("Attack");
                    }
                }
            }
        }

        private bool IsTargetInAttackAngle()
        {
            if (_target == null) return false;

            Vector3 directionToTarget = (_target.position - transform.position).normalized;
            float angle = Vector3.Angle(transform.forward, directionToTarget);

            return angle <= _enemyData.AttackAngle * 0.5f;
        }

        private void RetreatFromTarget()
        {
            ChangeState(EnemyState.Retreat);
        }

        private void UpdateMovement()
        {
            if (_controller == null) return;

            if (_controller.isGrounded)
            {
                _verticalVelocity = -2f;
            }
            else
            {
                _verticalVelocity += _enemyData.Gravity * Time.deltaTime;
            }

            Vector3 movement = _moveDirection * _currentMoveSpeed + Vector3.up * _verticalVelocity;
            _controller.Move(movement * Time.deltaTime);
        }

        private void UpdateAnimator()
        {
            if (_animator == null) return;

            float moveMagnitude = _moveDirection.magnitude;
            _animator.SetFloat("MoveSpeed", moveMagnitude);
            _animator.SetBool("IsChasing", _currentState == EnemyState.Chase);
            _animator.SetBool("IsDead", _currentState == EnemyState.Dead);
        }

        #region IDamagable Implementation

        public void TakeDamage(float damage)
        {
            if (_currentState == EnemyState.Dead) return;

            _currentHealth -= damage;
            PlayHurtSound();
            OnEnemyDamaged?.Invoke(this);
            EventManager.TriggerEvent("OnEnemyDamaged", this);

            if (_animator != null)
            {
                _animator.SetTrigger("Damaged");
            }

            if (_target == null)
            {
                _target = _player;
            }

            if (_currentHealth <= 0f)
            {
                Die();
            }
            else if (_currentState != EnemyState.Chase && _currentState != EnemyState.Attack)
            {
                ChangeState(EnemyState.Alert);
            }
        }

        public void Heal(float amount)
        {
            if (_enemyData == null) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _enemyData.MaxHealth);
        }

        bool IDamagable.IsAlive => _currentHealth > 0f;

        #endregion

        private void Die()
        {
            _currentState = EnemyState.Dead;
            _moveDirection = Vector3.zero;

            PlayDeathSound();
            SpawnLoot();

            OnEnemyDeath?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnEnemyKilled, this);

            if (_animator != null)
            {
                _animator.SetBool("IsDead", true);
            }

            if (_controller != null)
            {
                _controller.enabled = false;
            }

            Collider collider = GetComponent<Collider>();
            if (collider != null)
            {
                collider.enabled = false;
            }

            Destroy(gameObject, 5f);
        }

        private void SpawnLoot()
        {
            if (_enemyData == null) return;

            foreach (LootTableEntry entry in _enemyData.LootTable)
            {
                if (entry.Item == null) continue;
                if (Random.value > entry.DropChance) continue;

                int quantity = Random.Range(entry.MinQuantity, entry.MaxQuantity + 1);

                Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
                Vector3 randomOffset = new Vector3(
                    Random.Range(-0.5f, 0.5f),
                    0f,
                    Random.Range(-0.5f, 0.5f)
                );

                if (entry.Item.WorldPrefab != null)
                {
                    GameObject dropObject = Instantiate(
                        entry.Item.WorldPrefab,
                        spawnPosition + randomOffset,
                        Quaternion.identity
                    );

                    World.Resources.WorldItem worldItem = dropObject.AddComponent<World.Resources.WorldItem>();
                    worldItem.Initialize(new ItemInstance(entry.Item, quantity));
                }
            }
        }

        private void PlayIdleSound()
        {
            if (_audioSource != null && _enemyData != null && _enemyData.IdleSound != null)
            {
                _audioSource.PlayOneShot(_enemyData.IdleSound);
            }
        }

        private void PlayAttackSound()
        {
            if (_audioSource != null && _enemyData != null && _enemyData.AttackSound != null)
            {
                _audioSource.PlayOneShot(_enemyData.AttackSound);
            }
        }

        private void PlayHurtSound()
        {
            if (_audioSource != null && _enemyData != null && _enemyData.HurtSound != null)
            {
                _audioSource.PlayOneShot(_enemyData.HurtSound);
            }
        }

        private void PlayDeathSound()
        {
            if (_audioSource != null && _enemyData != null && _enemyData.DeathSound != null)
            {
                AudioSource.PlayClipAtPoint(_enemyData.DeathSound, transform.position);
            }
        }

        public EnemySaveData GetSaveData()
        {
            return new EnemySaveData
            {
                EnemyID = _enemyID,
                EnemyDataID = _enemyData?.EnemyID,
                Position = transform.position,
                Rotation = transform.rotation,
                CurrentHealth = _currentHealth,
                CurrentState = (int)_currentState
            };
        }

        public void LoadFromSaveData(EnemySaveData saveData)
        {
            if (saveData == null) return;

            _enemyID = saveData.EnemyID;
            transform.position = saveData.Position;
            transform.rotation = saveData.Rotation;
            _currentHealth = saveData.CurrentHealth;
            _currentState = (EnemyState)saveData.CurrentState;
        }
    }

    [System.Serializable]
    public class EnemySaveData
    {
        public string EnemyID;
        public string EnemyDataID;
        public Vector3 Position;
        public Quaternion Rotation;
        public float CurrentHealth;
        public int CurrentState;
    }
}
