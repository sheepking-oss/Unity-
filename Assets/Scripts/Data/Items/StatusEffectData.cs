using UnityEngine;

namespace SurvivalGame.Data.Items
{
    [CreateAssetMenu(fileName = "NewStatusEffect", menuName = "SurvivalGame/Items/Status Effect")]
    public class StatusEffectData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _effectName = "New Effect";
        [SerializeField] private string _effectID = "effect_0001";
        [TextArea]
        [SerializeField] private string _description;

        [Header("Effect Properties")]
        [SerializeField] private StatusEffectType _effectType = StatusEffectType.Buff;
        [SerializeField] private float _duration = 10f;
        [SerializeField] private bool _isStackable = false;
        [SerializeField] private int _maxStacks = 1;

        [Header("Tick Settings")]
        [SerializeField] private bool _isPeriodic = false;
        [SerializeField] private float _tickInterval = 1f;

        [Header("Modifiers")]
        [SerializeField] private StatModifier[] _statModifiers;

        [Header("Visuals")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _tintColor = Color.white;
        [SerializeField] private GameObject _particleEffect;

        #region Properties

        public string EffectName => _effectName;
        public string EffectID => _effectID;
        public string Description => _description;

        public StatusEffectType EffectType => _effectType;
        public float Duration => _duration;
        public bool IsStackable => _isStackable;
        public int MaxStacks => _maxStacks;

        public bool IsPeriodic => _isPeriodic;
        public float TickInterval => _tickInterval;

        public StatModifier[] StatModifiers => _statModifiers;

        public Sprite Icon => _icon;
        public Color TintColor => _tintColor;
        public GameObject ParticleEffect => _particleEffect;

        #endregion
    }

    public enum StatusEffectType
    {
        Buff,
        Debuff,
        Neutral
    }

    [System.Serializable]
    public class StatModifier
    {
        public StatType StatType;
        public float Value;
        public ModifierType ModifierType;
    }

    public enum StatType
    {
        Health,
        MaxHealth,
        Hunger,
        MaxHunger,
        Stamina,
        MaxStamina,
        Temperature,
        MoveSpeed,
        AttackDamage,
        Defense,
        GatheringSpeed
    }

    public enum ModifierType
    {
        Flat,
        PercentAdd,
        PercentMultiply
    }
}
