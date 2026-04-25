using UnityEngine;

namespace SurvivalGame.Data.Enemies
{
    [CreateAssetMenu(fileName = "NewEnemy", menuName = "SurvivalGame/Enemies/Enemy Data")]
    public class EnemyData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _enemyName = "New Enemy";
        [SerializeField] private string _enemyID = "enemy_0001";
        [TextArea]
        [SerializeField] private string _description;

        [Header("Enemy Type")]
        [SerializeField] private EnemyType _enemyType = EnemyType.Hostile;
        [SerializeField] private EnemyBehaviorType _behaviorType = EnemyBehaviorType.Aggressive;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _moveSpeed = 3f;
        [SerializeField] private float _rotationSpeed = 180f;
        [SerializeField] private float _gravity = 20f;

        [Header("Combat")]
        [SerializeField] private float _damage = 10f;
        [SerializeField] private float _attackRange = 2f;
        [SerializeField] private float _attackCooldown = 1.5f;
        [SerializeField] private float _detectionRange = 10f;
        [SerializeField] private float _chaseRange = 15f;
        [SerializeField] private float _forgetRange = 20f;
        [SerializeField] private float _attackAngle = 60f;

        [Header("Patrol")]
        [SerializeField] private bool _canPatrol = true;
        [SerializeField] private float _patrolRadius = 10f;
        [SerializeField] private float _patrolWaitTime = 3f;

        [Header("Loot")]
        [SerializeField] private LootTableEntry[] _lootTable;
        [SerializeField] private int _experienceReward = 10;

        [Header("Behavior")]
        [SerializeField] private bool _isNocturnal = false;
        [SerializeField] private bool _afraidOfLight = false;
        [SerializeField] private float _sightRange = 15f;
        [SerializeField] private float _hearingRange = 8f;
        [SerializeField] private float _investigationTime = 5f;

        [Header("Sound")]
        [SerializeField] private AudioClip _idleSound;
        [SerializeField] private AudioClip _attackSound;
        [SerializeField] private AudioClip _hurtSound;
        [SerializeField] private AudioClip _deathSound;

        #region Properties

        public string EnemyName => _enemyName;
        public string EnemyID => _enemyID;
        public string Description => _description;

        public EnemyType EnemyType => _enemyType;
        public EnemyBehaviorType BehaviorType => _behaviorType;

        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;

        public float MaxHealth => _maxHealth;
        public float MoveSpeed => _moveSpeed;
        public float RotationSpeed => _rotationSpeed;
        public float Gravity => _gravity;

        public float Damage => _damage;
        public float AttackRange => _attackRange;
        public float AttackCooldown => _attackCooldown;
        public float DetectionRange => _detectionRange;
        public float ChaseRange => _chaseRange;
        public float ForgetRange => _forgetRange;
        public float AttackAngle => _attackAngle;

        public bool CanPatrol => _canPatrol;
        public float PatrolRadius => _patrolRadius;
        public float PatrolWaitTime => _patrolWaitTime;

        public LootTableEntry[] LootTable => _lootTable;
        public int ExperienceReward => _experienceReward;

        public bool IsNocturnal => _isNocturnal;
        public bool AfraidOfLight => _afraidOfLight;
        public float SightRange => _sightRange;
        public float HearingRange => _hearingRange;
        public float InvestigationTime => _investigationTime;

        public AudioClip IdleSound => _idleSound;
        public AudioClip AttackSound => _attackSound;
        public AudioClip HurtSound => _hurtSound;
        public AudioClip DeathSound => _deathSound;

        #endregion
    }

    [System.Serializable]
    public class LootTableEntry
    {
        public Items.ItemData Item;
        public int MinQuantity = 1;
        public int MaxQuantity = 1;
        public float DropChance = 1f;
    }

    public enum EnemyType
    {
        Hostile,
        Neutral,
        Passive,
        Boss
    }

    public enum EnemyBehaviorType
    {
        Aggressive,
        Defensive,
        Patrol,
        Wander,
        Stationary,
        Herd
    }
}
