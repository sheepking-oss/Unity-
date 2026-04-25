using UnityEngine;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Data.Buildings;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;

namespace SurvivalGame.World.Buildings
{
    [RequireComponent(typeof(Collider))]
    public class Building : MonoBehaviour, IDamagable, IInteractable
    {
        [Header("Building Data")]
        [SerializeField] private BuildingData _buildingData;
        [SerializeField] private string _buildingID;

        [Header("Health")]
        [SerializeField] private float _currentHealth;

        [Header("Visuals")]
        [SerializeField] private Renderer _mainRenderer;
        [SerializeField] private GameObject _damagedVisual;
        [SerializeField] private ParticleSystem _destroyEffect;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _destroySound;

        private Collider _collider;
        private bool _isInitialized = false;

        public BuildingData BuildingData => _buildingData;
        public string BuildingID => _buildingID;
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _buildingData?.MaxHealth ?? 0f;
        public float HealthPercent => MaxHealth > 0 ? _currentHealth / MaxHealth : 0f;
        public bool IsAlive => _currentHealth > 0f;

        public event System.Action<Building> OnBuildingDestroyed;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
        }

        private void Start()
        {
            if (string.IsNullOrEmpty(_buildingID))
            {
                GenerateNewBuildingID();
            }

            if (_buildingData != null && !_isInitialized)
            {
                Initialize(_buildingData);
            }
        }

        private void GenerateNewBuildingID()
        {
            _buildingID = $"building_{GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
        }

        public void Initialize(BuildingData data)
        {
            _buildingData = data;
            _currentHealth = data.MaxHealth;
            _isInitialized = true;

            UpdateVisuals();
        }

        public void InitializeWithID(BuildingData data, string buildingID)
        {
            _buildingID = buildingID;
            _buildingData = data;
            _currentHealth = data.MaxHealth;
            _isInitialized = true;

            UpdateVisuals();
        }

        #region IDamagable Implementation

        public void TakeDamage(float damage)
        {
            if (_buildingData == null) return;
            if (_currentHealth <= 0f) return;

            _currentHealth -= damage;

            PlayHitSound();
            UpdateVisuals();
            EventManager.TriggerEvent("OnBuildingDamaged", this);

            if (_currentHealth <= 0f)
            {
                DestroyBuilding();
            }
        }

        public void Heal(float amount)
        {
            if (_buildingData == null) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _buildingData.MaxHealth);
            UpdateVisuals();
        }

        bool IDamagable.IsAlive => _currentHealth > 0f;

        #endregion

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            if (_buildingData == null) return;

            if (_buildingData.IsContainer)
            {
                EventManager.TriggerEvent("OnOpenBuildingContainer", this);
            }
            else if (_buildingData.IsCraftingStation)
            {
                EventManager.TriggerEvent("OnOpenCraftingStation", this);
            }
        }

        public string GetInteractionText()
        {
            if (_buildingData == null) return "Building";

            if (_buildingData.IsContainer)
                return $"Open {_buildingData.BuildingName}";

            if (_buildingData.IsCraftingStation)
                return $"Use {_buildingData.BuildingName}";

            return _buildingData.BuildingName;
        }

        public bool CanInteract(GameObject interactor)
        {
            if (_buildingData == null) return false;
            return _buildingData.IsContainer || _buildingData.IsCraftingStation;
        }

        #endregion

        private void DestroyBuilding()
        {
            _currentHealth = 0f;

            PlayDestroySound();
            SpawnDestroyEffect();

            if (_buildingData != null && _buildingData.BuildCost != null)
            {
                foreach (var cost in _buildingData.BuildCost)
                {
                    if (cost.Item != null && Random.value < 0.5f)
                    {
                        SpawnDropInWorld(cost.Item, Mathf.Max(1, cost.Quantity / 2));
                    }
                }
            }

            OnBuildingDestroyed?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnBuildingDestroyed, this);

            Destroy(gameObject);
        }

        private void SpawnDropInWorld(ItemData item, int quantity)
        {
            if (item == null || item.WorldPrefab == null) return;

            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f + Random.insideUnitSphere;
            GameObject dropObject = Instantiate(item.WorldPrefab, spawnPosition, Quaternion.identity);

            Resources.WorldItem worldItem = dropObject.AddComponent<Resources.WorldItem>();
            worldItem.Initialize(new ItemInstance(item, quantity));
        }

        private void UpdateVisuals()
        {
            if (_buildingData == null) return;

            float healthPercent = HealthPercent;

            if (_damagedVisual != null)
            {
                if (healthPercent < 0.5f)
                {
                    _damagedVisual.SetActive(true);
                }
                else
                {
                    _damagedVisual.SetActive(false);
                }
            }

            if (_mainRenderer != null)
            {
                Color tintColor = Color.Lerp(Color.red, Color.white, healthPercent);
                foreach (Material mat in _mainRenderer.materials)
                {
                    mat.SetColor("_Tint", tintColor);
                }
            }
        }

        private void PlayHitSound()
        {
            if (_audioSource != null && _hitSound != null)
            {
                _audioSource.PlayOneShot(_hitSound);
            }
        }

        private void PlayDestroySound()
        {
            if (_audioSource != null && _destroySound != null)
            {
                AudioSource.PlayClipAtPoint(_destroySound, transform.position);
            }
        }

        private void SpawnDestroyEffect()
        {
            if (_destroyEffect != null)
            {
                Instantiate(_destroyEffect, transform.position, transform.rotation);
            }
        }

        public BuildingSaveData GetSaveData()
        {
            return new BuildingSaveData
            {
                BuildingID = _buildingID,
                BuildingDataID = _buildingData?.BuildingID,
                Position = transform.position,
                Rotation = transform.rotation,
                Scale = transform.localScale,
                CurrentHealth = _currentHealth
            };
        }

        public void LoadFromSaveData(BuildingSaveData saveData)
        {
            if (saveData == null) return;

            _buildingID = saveData.BuildingID;
            transform.position = saveData.Position;
            transform.rotation = saveData.Rotation;
            transform.localScale = saveData.Scale;
            _currentHealth = saveData.CurrentHealth;

            UpdateVisuals();
        }
    }

    [System.Serializable]
    public class BuildingSaveData
    {
        public string BuildingID;
        public string BuildingDataID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public float CurrentHealth;
    }
}
