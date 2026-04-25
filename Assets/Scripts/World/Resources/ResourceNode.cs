using UnityEngine;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Data.Resources;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;
using System.Collections.Generic;

namespace SurvivalGame.World.Resources
{
    [RequireComponent(typeof(Collider))]
    public class ResourceNode : MonoBehaviour, IDamagable, IInteractable
    {
        [Header("Resource Data")]
        [SerializeField] private ResourceNodeData _nodeData;

        [Header("Node Settings")]
        [SerializeField] private string _saveID;
        [SerializeField] private bool _isHarvested = false;
        [SerializeField] private float _currentHealth;

        [Header("Visuals")]
        [SerializeField] private GameObject _normalVisual;
        [SerializeField] private GameObject _damagedVisual;
        [SerializeField] private GameObject _harvestedVisual;
        [SerializeField] private ParticleSystem _harvestEffect;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;

        private Collider _collider;
        private float _respawnTimer;
        private Vector3 _originalPosition;

        public ResourceNodeData NodeData => _nodeData;
        public string SaveID => _saveID;
        public float CurrentHealth => _currentHealth;
        public bool IsHarvested => _isHarvested;
        public bool IsAlive => !_isHarvested;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            _originalPosition = transform.position;

            if (string.IsNullOrEmpty(_saveID))
            {
                _saveID = $"resource_{GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        private void Start()
        {
            if (_nodeData != null)
            {
                _currentHealth = _nodeData.MaxHealth;
            }

            UpdateVisuals();
        }

        private void Update()
        {
            if (_isHarvested && _nodeData != null && _nodeData.IsRenewable)
            {
                _respawnTimer -= Time.deltaTime;
                if (_respawnTimer <= 0f)
                {
                    Respawn();
                }
            }
        }

        #region IDamagable Implementation

        public void TakeDamage(float damage)
        {
            if (_isHarvested) return;

            _currentHealth -= damage;

            PlayHitSound();
            SpawnHarvestParticles();
            UpdateVisuals();

            if (_currentHealth <= 0f)
            {
                Harvest();
            }
        }

        public void Heal(float amount)
        {
            if (_nodeData == null) return;
            _currentHealth = Mathf.Min(_currentHealth + amount, _nodeData.MaxHealth);
            UpdateVisuals();
        }

        bool IDamagable.IsAlive => !_isHarvested;

        #endregion

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            TakeDamage(10f);
        }

        public string GetInteractionText()
        {
            if (_nodeData == null) return "Resource Node";
            return $"Harvest {_nodeData.NodeName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            return !_isHarvested && _nodeData != null;
        }

        #endregion

        public void SetNodeData(ResourceNodeData data)
        {
            _nodeData = data;
            _currentHealth = data.MaxHealth;
            UpdateVisuals();
        }

        private void Harvest()
        {
            _isHarvested = true;
            _currentHealth = 0f;

            if (_collider != null)
            {
                _collider.enabled = false;
            }

            SpawnDrops();
            UpdateVisuals();

            if (_nodeData != null && _nodeData.IsRenewable)
            {
                _respawnTimer = _nodeData.RespawnTime;
            }

            EventManager.TriggerEvent("OnResourceHarvested", this);
            Debug.Log($"Resource node harvested: {_nodeData?.NodeName}");
        }

        private void SpawnDrops()
        {
            if (_nodeData == null) return;

            List<ItemInstance> drops = new List<ItemInstance>();

            SpawnDropList(_nodeData.Drops, drops);

            if (_nodeData.HasRareDrops && Random.value <= _nodeData.RareDropChance)
            {
                SpawnDropList(_nodeData.RareDrops, drops);
            }

            foreach (ItemInstance drop in drops)
            {
                SpawnDropInWorld(drop);
            }

            PlayDestroySound();
        }

        private void SpawnDropList(ResourceDrop[] dropList, List<ItemInstance> output)
        {
            if (dropList == null) return;

            foreach (ResourceDrop drop in dropList)
            {
                if (drop.Item == null) continue;
                if (Random.value > drop.DropChance) continue;

                int quantity = Random.Range(drop.MinQuantity, drop.MaxQuantity + 1);
                output.Add(new ItemInstance(drop.Item, quantity));
            }
        }

        private void SpawnDropInWorld(ItemInstance itemInstance)
        {
            if (itemInstance.ItemData == null) return;
            if (itemInstance.ItemData.WorldPrefab == null) return;

            Vector3 spawnPosition = transform.position + Vector3.up * 0.5f;
            Vector3 randomOffset = new Vector3(
                Random.Range(-0.5f, 0.5f),
                0f,
                Random.Range(-0.5f, 0.5f)
            );

            GameObject dropObject = Instantiate(
                itemInstance.ItemData.WorldPrefab,
                spawnPosition + randomOffset,
                Quaternion.identity
            );

            WorldItem worldItem = dropObject.AddComponent<WorldItem>();
            worldItem.Initialize(itemInstance);

            Rigidbody rb = dropObject.GetComponent<Rigidbody>();
            if (rb != null)
            {
                Vector3 randomForce = new Vector3(
                    Random.Range(-1f, 1f),
                    Random.Range(2f, 4f),
                    Random.Range(-1f, 1f)
                );
                rb.AddForce(randomForce, ForceMode.Impulse);
            }
        }

        private void Respawn()
        {
            if (_nodeData == null) return;

            _isHarvested = false;
            _currentHealth = _nodeData.MaxHealth;
            _respawnTimer = 0f;

            if (_collider != null)
            {
                _collider.enabled = true;
            }

            UpdateVisuals();
            EventManager.TriggerEvent("OnResourceRespawned", this);
            Debug.Log($"Resource node respawned: {_nodeData.NodeName}");
        }

        private void UpdateVisuals()
        {
            if (_nodeData == null) return;

            float healthPercent = _currentHealth / _nodeData.MaxHealth;

            if (_isHarvested)
            {
                if (_normalVisual != null) _normalVisual.SetActive(false);
                if (_damagedVisual != null) _damagedVisual.SetActive(false);
                if (_harvestedVisual != null) _harvestedVisual.SetActive(true);
            }
            else if (healthPercent < 0.5f && _damagedVisual != null)
            {
                if (_normalVisual != null) _normalVisual.SetActive(false);
                _damagedVisual.SetActive(true);
                if (_harvestedVisual != null) _harvestedVisual.SetActive(false);
            }
            else
            {
                if (_normalVisual != null) _normalVisual.SetActive(true);
                if (_damagedVisual != null) _damagedVisual.SetActive(false);
                if (_harvestedVisual != null) _harvestedVisual.SetActive(false);
            }
        }

        private void PlayHitSound()
        {
            if (_audioSource != null && _nodeData != null && _nodeData.HitSound != null)
            {
                _audioSource.PlayOneShot(_nodeData.HitSound);
            }
        }

        private void PlayDestroySound()
        {
            if (_audioSource != null && _nodeData != null && _nodeData.DestroySound != null)
            {
                _audioSource.PlayOneShot(_nodeData.DestroySound);
            }
        }

        private void SpawnHarvestParticles()
        {
            if (_harvestEffect != null)
            {
                _harvestEffect.Play();
            }
        }

        public ResourceNodeSaveData GetSaveData()
        {
            return new ResourceNodeSaveData
            {
                SaveID = _saveID,
                Position = transform.position,
                Rotation = transform.rotation,
                CurrentHealth = _currentHealth,
                IsHarvested = _isHarvested,
                RespawnTimer = _respawnTimer
            };
        }

        public void LoadFromSaveData(ResourceNodeSaveData data)
        {
            if (data == null) return;

            _saveID = data.SaveID;
            transform.position = data.Position;
            transform.rotation = data.Rotation;
            _currentHealth = data.CurrentHealth;
            _isHarvested = data.IsHarvested;
            _respawnTimer = data.RespawnTimer;

            if (_collider != null)
            {
                _collider.enabled = !_isHarvested;
            }

            UpdateVisuals();
        }
    }

    [System.Serializable]
    public class ResourceNodeSaveData
    {
        public string SaveID;
        public Vector3 Position;
        public Quaternion Rotation;
        public float CurrentHealth;
        public bool IsHarvested;
        public float RespawnTimer;
    }
}
