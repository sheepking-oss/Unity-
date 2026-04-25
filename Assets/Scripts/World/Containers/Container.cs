using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Inventory;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.World.Containers
{
    [RequireComponent(typeof(Collider))]
    public class Container : MonoBehaviour, IInteractable
    {
        [Header("Container Settings")]
        [SerializeField] private string _containerName = "Container";
        [SerializeField] private string _containerID;
        [SerializeField] private int _size = 24;
        [SerializeField] private ContainerType _containerType = ContainerType.Storage;
        [SerializeField] private bool _isLocked = false;

        [Header("Lock Settings")]
        [SerializeField] private string _requiredKeyID;
        [SerializeField] private bool _canBePicked = false;
        [SerializeField] private int _lockDifficulty = 1;

        [Header("Visuals")]
        [SerializeField] private GameObject _closedVisual;
        [SerializeField] private GameObject _openVisual;
        [SerializeField] private Animator _animator;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _openSound;
        [SerializeField] private AudioClip _closeSound;
        [SerializeField] private AudioClip _lockedSound;

        private InventoryData _inventory;
        private Collider _collider;
        private bool _isOpen = false;

        public string ContainerName => _containerName;
        public string ContainerID => _containerID;
        public int Size => _size;
        public ContainerType ContainerType => _containerType;
        public bool IsLocked => _isLocked;
        public bool IsOpen => _isOpen;
        public InventoryData Inventory => _inventory;

        public event System.Action<Container> OnContainerOpened;
        public event System.Action<Container> OnContainerClosed;

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            if (string.IsNullOrEmpty(_containerID))
            {
                _containerID = $"container_{GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        private void Start()
        {
            InitializeInventory();
            UpdateVisuals();
        }

        private void InitializeInventory()
        {
            _inventory = new InventoryData(_containerID, _size);
            _inventory.OnInventoryChanged += () =>
            {
                EventManager.TriggerEvent("OnContainerInventoryChanged", this);
            };
        }

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            if (_isLocked)
            {
                TryUnlock(interactor);
                return;
            }

            if (_isOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        public string GetInteractionText()
        {
            if (_isLocked)
                return $"{_containerName} (Locked)";

            return _isOpen ? $"Close {_containerName}" : $"Open {_containerName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        #endregion

        public void Open()
        {
            if (_isLocked || _isOpen) return;

            _isOpen = true;
            UpdateVisuals();
            PlayOpenSound();

            if (_animator != null)
            {
                _animator.SetBool("IsOpen", true);
            }

            OnContainerOpened?.Invoke(this);
            EventManager.TriggerEvent("OnContainerOpened", this);
        }

        public void Close()
        {
            if (!_isOpen) return;

            _isOpen = false;
            UpdateVisuals();
            PlayCloseSound();

            if (_animator != null)
            {
                _animator.SetBool("IsOpen", false);
            }

            OnContainerClosed?.Invoke(this);
            EventManager.TriggerEvent("OnContainerClosed", this);
        }

        private void TryUnlock(GameObject interactor)
        {
            InventoryManager inventoryManager = interactor.GetComponent<InventoryManager>();
            if (inventoryManager == null)
            {
                PlayLockedSound();
                return;
            }

            if (!string.IsNullOrEmpty(_requiredKeyID))
            {
                ItemData keyData = DataManager.Instance?.GetItem(_requiredKeyID);
                if (keyData != null && inventoryManager.HasItem(keyData, 1))
                {
                    Unlock();
                    return;
                }
            }

            if (_canBePicked)
            {
                if (TryPickLock(interactor))
                {
                    return;
                }
            }

            PlayLockedSound();
        }

        private bool TryPickLock(GameObject interactor)
        {
            return false;
        }

        public void Unlock()
        {
            _isLocked = false;
            UpdateVisuals();
            Open();
        }

        public void Lock()
        {
            _isLocked = true;
            Close();
            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (_closedVisual != null)
            {
                _closedVisual.SetActive(!_isOpen);
            }

            if (_openVisual != null)
            {
                _openVisual.SetActive(_isOpen);
            }
        }

        private void PlayOpenSound()
        {
            if (_audioSource != null && _openSound != null)
            {
                _audioSource.PlayOneShot(_openSound);
            }
        }

        private void PlayCloseSound()
        {
            if (_audioSource != null && _closeSound != null)
            {
                _audioSource.PlayOneShot(_closeSound);
            }
        }

        private void PlayLockedSound()
        {
            if (_audioSource != null && _lockedSound != null)
            {
                _audioSource.PlayOneShot(_lockedSound);
            }
        }

        public bool AddItem(ItemData itemData, int quantity = 1)
        {
            return _inventory?.AddItem(itemData, quantity) ?? false;
        }

        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            return _inventory?.RemoveItem(itemData, quantity) ?? false;
        }

        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            return _inventory?.HasItem(itemData, quantity) ?? false;
        }

        public int GetItemCount(ItemData itemData)
        {
            return _inventory?.GetItemCount(itemData) ?? 0;
        }

        public ContainerSaveData GetSaveData()
        {
            return new ContainerSaveData
            {
                ContainerID = _containerID,
                Position = transform.position,
                Rotation = transform.rotation,
                IsLocked = _isLocked,
                IsOpen = _isOpen,
                InventoryData = _inventory?.GetSaveData()
            };
        }

        public void LoadFromSaveData(ContainerSaveData saveData)
        {
            if (saveData == null) return;

            _containerID = saveData.ContainerID;
            transform.position = saveData.Position;
            transform.rotation = saveData.Rotation;
            _isLocked = saveData.IsLocked;
            _isOpen = saveData.IsOpen;

            if (saveData.InventoryData != null && _inventory != null)
            {
                _inventory.LoadFromSaveData(saveData.InventoryData, DataManager.Instance);
            }

            UpdateVisuals();
        }
    }

    public enum ContainerType
    {
        Storage,
        Chest,
        Crate,
        Barrel,
        Cabinet,
        Drawer,
        Trunk,
        Safe,
        Merchant
    }

    [System.Serializable]
    public class ContainerSaveData
    {
        public string ContainerID;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsLocked;
        public bool IsOpen;
        public InventorySaveData InventoryData;
    }
}
