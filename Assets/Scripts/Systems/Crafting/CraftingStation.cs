using UnityEngine;
using SurvivalGame.Data.Crafting;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Systems.Crafting
{
    [RequireComponent(typeof(Collider))]
    public class CraftingStation : MonoBehaviour, IInteractable
    {
        [Header("Station Settings")]
        [SerializeField] private string _stationName = "Crafting Station";
        [SerializeField] private CraftingStationType _stationType = CraftingStationType.Workbench;
        [SerializeField] private int _maxConcurrentCrafts = 3;
        [SerializeField] private bool _requiresFuel = false;

        [Header("Visuals")]
        [SerializeField] private GameObject _inactiveVisual;
        [SerializeField] private GameObject _activeVisual;
        [SerializeField] private Animator _animator;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _craftingSound;

        private Collider _collider;
        private bool _isActive = false;
        private int _currentCraftCount = 0;

        public string StationName => _stationName;
        public CraftingStationType StationType => _stationType;
        public int MaxConcurrentCrafts => _maxConcurrentCrafts;
        public bool RequiresFuel => _requiresFuel;
        public bool IsActive => _isActive;
        public bool CanCraft => _currentCraftCount < _maxConcurrentCrafts;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }
        }

        private void Start()
        {
            UpdateVisuals();
            CraftingManager.Instance?.RegisterCraftingStation(this);
        }

        private void OnDestroy()
        {
            CraftingManager.Instance?.UnregisterCraftingStation(this);
        }

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            OpenCraftingUI();
        }

        public string GetInteractionText()
        {
            return $"Use {_stationName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        #endregion

        private void OpenCraftingUI()
        {
            EventManager.TriggerEvent("OnOpenCraftingUI", this);
        }

        public void StartCrafting()
        {
            _currentCraftCount++;
            if (!_isActive)
            {
                Activate();
            }

            PlayCraftingSound();
            if (_animator != null)
            {
                _animator.SetTrigger("Craft");
            }
        }

        public void EndCrafting()
        {
            _currentCraftCount--;
            if (_currentCraftCount <= 0)
            {
                _currentCraftCount = 0;
                Deactivate();
            }
        }

        private void Activate()
        {
            _isActive = true;
            UpdateVisuals();
            if (_animator != null)
            {
                _animator.SetBool("IsActive", true);
            }
        }

        private void Deactivate()
        {
            _isActive = false;
            UpdateVisuals();
            if (_animator != null)
            {
                _animator.SetBool("IsActive", false);
            }
        }

        private void UpdateVisuals()
        {
            if (_inactiveVisual != null)
            {
                _inactiveVisual.SetActive(!_isActive);
            }

            if (_activeVisual != null)
            {
                _activeVisual.SetActive(_isActive);
            }
        }

        private void PlayCraftingSound()
        {
            if (_audioSource != null && _craftingSound != null)
            {
                if (!_audioSource.isPlaying)
                {
                    _audioSource.PlayOneShot(_craftingSound);
                }
            }
        }

        public CraftingStationSaveData GetSaveData()
        {
            return new CraftingStationSaveData
            {
                StationType = (int)_stationType,
                Position = transform.position,
                Rotation = transform.rotation,
                IsActive = _isActive
            };
        }
    }

    [System.Serializable]
    public class CraftingStationSaveData
    {
        public int StationType;
        public Vector3 Position;
        public Quaternion Rotation;
        public bool IsActive;
    }
}
