using UnityEngine;

namespace SurvivalGame.Data.Items
{
    [CreateAssetMenu(fileName = "NewConsumable", menuName = "SurvivalGame/Items/Consumable Item")]
    public class ConsumableItemData : ItemData
    {
        [Header("Consumable Effects")]
        [SerializeField] private float _healthRestore = 0f;
        [SerializeField] private float _hungerRestore = 0f;
        [SerializeField] private float _staminaRestore = 0f;
        [SerializeField] private float _temperatureRestore = 0f;

        [Header("Status Effects")]
        [SerializeField] private StatusEffectData[] _statusEffects;

        [Header("Usage Settings")]
        [SerializeField] private bool _destroyOnUse = true;
        [SerializeField] private float _useTime = 1f;

        #region Properties

        public float HealthRestore => _healthRestore;
        public float HungerRestore => _hungerRestore;
        public float StaminaRestore => _staminaRestore;
        public float TemperatureRestore => _temperatureRestore;
        public StatusEffectData[] StatusEffects => _statusEffects;
        public bool DestroyOnUse => _destroyOnUse;
        public float UseTime => _useTime;

        #endregion
    }
}
