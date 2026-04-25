using UnityEngine;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Player.Controllers
{
    public class PlayerController : MonoBehaviour, IDamagable
    {
        [Header("Component References")]
        [SerializeField] private PlayerMovement _playerMovement;
        [SerializeField] private PlayerInteraction _playerInteraction;
        [SerializeField] private Animator _animator;
        [SerializeField] private CharacterController _controller;

        [Header("Player Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _maxHunger = 100f;
        [SerializeField] private float _maxStamina = 100f;
        [SerializeField] private float _maxTemperature = 100f;

        [Header("Regeneration Settings")]
        [SerializeField] private float _healthRegenRate = 1f;
        [SerializeField] private float _staminaRegenRate = 5f;
        [SerializeField] private float _staminaDrainRate = 10f;

        [Header("Hunger Settings")]
        [SerializeField] private float _hungerDrainRate = 0.5f;
        [SerializeField] private float _starvationDamageRate = 1f;

        private float _currentHealth;
        private float _currentHunger;
        private float _currentStamina;
        private float _currentTemperature;

        private bool _isAlive = true;
        private bool _isInvincible = false;

        #region Properties

        public PlayerMovement Movement => _playerMovement;
        public PlayerInteraction Interaction => _playerInteraction;
        public Animator Animator => _animator;
        public CharacterController Controller => _controller;

        public float MaxHealth => _maxHealth;
        public float MaxHunger => _maxHunger;
        public float MaxStamina => _maxStamina;
        public float MaxTemperature => _maxTemperature;

        public float Health
        {
            get => _currentHealth;
            private set
            {
                float oldValue = _currentHealth;
                _currentHealth = Mathf.Clamp(value, 0f, _maxHealth);
                if (Mathf.Abs(oldValue - _currentHealth) > 0.01f)
                {
                    EventManager.TriggerEvent(GameEvents.OnPlayerHealthChanged, _currentHealth);
                }
            }
        }

        public float Hunger
        {
            get => _currentHunger;
            private set
            {
                float oldValue = _currentHunger;
                _currentHunger = Mathf.Clamp(value, 0f, _maxHunger);
                if (Mathf.Abs(oldValue - _currentHunger) > 0.01f)
                {
                    EventManager.TriggerEvent(GameEvents.OnPlayerHungerChanged, _currentHunger);
                }
            }
        }

        public float Stamina
        {
            get => _currentStamina;
            private set
            {
                float oldValue = _currentStamina;
                _currentStamina = Mathf.Clamp(value, 0f, _maxStamina);
                if (Mathf.Abs(oldValue - _currentStamina) > 0.01f)
                {
                    EventManager.TriggerEvent(GameEvents.OnPlayerStaminaChanged, _currentStamina);
                }
            }
        }

        public float Temperature
        {
            get => _currentTemperature;
            private set => _currentTemperature = Mathf.Clamp(value, 0f, _maxTemperature);
        }

        public bool IsAlive => _isAlive;
        public bool IsInvincible
        {
            get => _isInvincible;
            set => _isInvincible = value;
        }

        #endregion

        private void Awake()
        {
            if (_playerMovement == null)
                _playerMovement = GetComponent<PlayerMovement>();
            if (_playerInteraction == null)
                _playerInteraction = GetComponent<PlayerInteraction>();
            if (_animator == null)
                _animator = GetComponent<Animator>();
            if (_controller == null)
                _controller = GetComponent<CharacterController>();
        }

        private void Start()
        {
            InitializeStats();
        }

        private void Update()
        {
            if (!_isAlive) return;

            UpdateHunger();
            UpdateStamina();
            UpdateHealthRegen();
        }

        private void InitializeStats()
        {
            Health = _maxHealth;
            Hunger = _maxHunger;
            Stamina = _maxStamina;
            Temperature = _maxTemperature * 0.5f;
        }

        private void UpdateHunger()
        {
            Hunger -= _hungerDrainRate * Time.deltaTime;

            if (Hunger <= 0f)
            {
                TakeDamage(_starvationDamageRate * Time.deltaTime);
            }
        }

        private void UpdateStamina()
        {
            bool isSprinting = _playerMovement != null && _playerMovement.IsSprinting;

            if (isSprinting && _playerMovement.IsMoving)
            {
                Stamina -= _staminaDrainRate * Time.deltaTime;

                if (Stamina <= 0f)
                {
                    Stamina = 0f;
                }
            }
            else
            {
                Stamina += _staminaRegenRate * Time.deltaTime;
            }
        }

        private void UpdateHealthRegen()
        {
            if (Health < _maxHealth && Hunger > 20f)
            {
                Health += _healthRegenRate * Time.deltaTime;
            }
        }

        #region IDamagable Implementation

        public void TakeDamage(float damage)
        {
            if (_isInvincible || !_isAlive) return;

            Health -= damage;
            _animator?.SetTrigger("Damaged");

            EventManager.TriggerEvent("OnPlayerDamaged", damage);

            if (Health <= 0f)
            {
                Die();
            }
        }

        public void Heal(float amount)
        {
            if (!_isAlive) return;

            Health += amount;
            EventManager.TriggerEvent("OnPlayerHealed", amount);
        }

        bool IDamagable.IsAlive => _isAlive;

        #endregion

        public void RestoreHunger(float amount)
        {
            Hunger += amount;
        }

        public void RestoreStamina(float amount)
        {
            Stamina += amount;
        }

        public void ConsumeStamina(float amount)
        {
            Stamina -= amount;
        }

        public bool HasEnoughStamina(float amount)
        {
            return Stamina >= amount;
        }

        private void Die()
        {
            _isAlive = false;
            Health = 0f;

            _animator?.SetBool("IsDead", true);
            EventManager.TriggerEvent("OnPlayerDeath");

            Debug.Log("Player has died!");
        }

        public void Revive()
        {
            _isAlive = true;
            InitializeStats();

            _animator?.SetBool("IsDead", false);
            EventManager.TriggerEvent("OnPlayerRevived");
        }

        public void SetMaxHealth(float maxHealth)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);
            Health = Mathf.Min(Health, _maxHealth);
        }

        public void SetMaxHunger(float maxHunger)
        {
            _maxHunger = Mathf.Max(1f, maxHunger);
            Hunger = Mathf.Min(Hunger, _maxHunger);
        }

        public void SetMaxStamina(float maxStamina)
        {
            _maxStamina = Mathf.Max(1f, maxStamina);
            Stamina = Mathf.Min(Stamina, _maxStamina);
        }
    }
}
