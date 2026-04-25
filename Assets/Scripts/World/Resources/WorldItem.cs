using UnityEngine;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Core.Events;

namespace SurvivalGame.World.Resources
{
    [RequireComponent(typeof(Collider), typeof(Rigidbody))]
    public class WorldItem : MonoBehaviour, IInteractable
    {
        [Header("Item Data")]
        [SerializeField] private ItemInstance _itemInstance;

        [Header("Settings")]
        [SerializeField] private float _pickupDelay = 0.5f;
        [SerializeField] private float _autoPickupRange = 1f;
        [SerializeField] private float _despawnTime = 120f;
        [SerializeField] private bool _canAutoPickup = true;

        [Header("Visuals")]
        [SerializeField] private Renderer _renderer;
        [SerializeField] private float _rotationSpeed = 90f;
        [SerializeField] private float _bobAmount = 0.2f;
        [SerializeField] private float _bobSpeed = 2f;

        private Rigidbody _rb;
        private Collider _collider;
        private float _spawnTime;
        private float _pickupTimer;
        private Vector3 _originalPosition;

        public ItemInstance ItemInstance => _itemInstance;
        public bool CanBePickedUp => Time.time - _spawnTime >= _pickupDelay;

        private void Awake()
        {
            _rb = GetComponent<Rigidbody>();
            _collider = GetComponent<Collider>();

            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (_rb != null)
            {
                _rb.isKinematic = false;
                _rb.useGravity = true;
            }
        }

        private void Start()
        {
            _spawnTime = Time.time;
            _originalPosition = transform.position;

            if (_renderer == null)
            {
                _renderer = GetComponent<Renderer>();
            }
        }

        private void Update()
        {
            HandleDespawn();
            HandleVisualEffects();
        }

        private void OnTriggerEnter(Collider other)
        {
            if (!_canAutoPickup) return;
            if (!CanBePickedUp) return;

            if (other.CompareTag("Player"))
            {
                TryPickup(other.gameObject);
            }
        }

        public void Initialize(ItemInstance itemInstance)
        {
            _itemInstance = itemInstance;
            gameObject.name = $"WorldItem_{itemInstance.ItemData?.ItemID ?? "Unknown"}";

            UpdateVisualsFromItem();
        }

        private void UpdateVisualsFromItem()
        {
            if (_itemInstance == null || _itemInstance.ItemData == null) return;

            if (_renderer != null && _itemInstance.ItemData.TintColor != Color.white)
            {
                foreach (Material mat in _renderer.materials)
                {
                    mat.color = _itemInstance.ItemData.TintColor;
                }
            }
        }

        private void HandleDespawn()
        {
            if (_despawnTime <= 0f) return;

            if (Time.time - _spawnTime >= _despawnTime)
            {
                Destroy(gameObject);
            }
        }

        private void HandleVisualEffects()
        {
            if (_itemInstance == null) return;

            float time = Time.time;

            transform.Rotate(Vector3.up, _rotationSpeed * Time.deltaTime, Space.World);

            if (_rb != null && _rb.velocity.magnitude < 0.1f)
            {
                float bobOffset = Mathf.Sin(time * _bobSpeed) * _bobAmount;
                transform.position = new Vector3(
                    transform.position.x,
                    _originalPosition.y + bobOffset,
                    transform.position.z
                );
            }
        }

        public bool TryPickup(GameObject picker)
        {
            if (_itemInstance == null) return false;

            EventManager.TriggerEvent("OnWorldItemPickedUp", this);

            Destroy(gameObject);
            return true;
        }

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            TryPickup(interactor);
        }

        public string GetInteractionText()
        {
            if (_itemInstance == null || _itemInstance.ItemData == null)
                return "Pick up item";

            string text = $"Pick up {_itemInstance.ItemData.ItemName}";
            if (_itemInstance.Quantity > 1)
            {
                text += $" x{_itemInstance.Quantity}";
            }
            return text;
        }

        public bool CanInteract(GameObject interactor)
        {
            return _itemInstance != null && CanBePickedUp;
        }

        #endregion
    }
}
