using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Crafting;
using SurvivalGame.Data.Items;
using SurvivalGame.Inventory;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Core.Events;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.Systems.Crafting
{
    [RequireComponent(typeof(Collider))]
    public class Furnace : MonoBehaviour, IInteractable
    {
        [Header("Furnace Settings")]
        [SerializeField] private string _furnaceName = "Furnace";
        [SerializeField] private int _fuelSlots = 1;
        [SerializeField] private int _inputSlots = 1;
        [SerializeField] private int _outputSlots = 1;
        [SerializeField] private float _maxTemperature = 1000f;
        [SerializeField] private float _heatingRate = 50f;
        [SerializeField] private float _coolingRate = 10f;
        [SerializeField] private float _cookTemperature = 300f;

        [Header("Visuals")]
        [SerializeField] private GameObject _inactiveVisual;
        [SerializeField] private GameObject _activeVisual;
        [SerializeField] private ParticleSystem _fireParticle;
        [SerializeField] private Animator _animator;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _fireSound;
        [SerializeField] private AudioClip _igniteSound;
        [SerializeField] private AudioClip _extinguishSound;

        private InventoryData _fuelInventory;
        private InventoryData _inputInventory;
        private InventoryData _outputInventory;
        private Collider _collider;

        private float _currentTemperature = 0f;
        private float _currentFuelTime = 0f;
        private float _maxFuelTime = 0f;
        private float _cookProgress = 0f;

        private bool _isBurning = false;
        private bool _isCooking = false;
        private RecipeData _currentCookingRecipe;

        public string FurnaceName => _furnaceName;
        public float CurrentTemperature => _currentTemperature;
        public float CurrentFuelTime => _currentFuelTime;
        public float MaxFuelTime => _maxFuelTime;
        public bool IsBurning => _isBurning;
        public bool IsCooking => _isCooking;
        public float CookProgress => _cookProgress;

        public InventoryData FuelInventory => _fuelInventory;
        public InventoryData InputInventory => _inputInventory;
        public InventoryData OutputInventory => _outputInventory;

        private void Awake()
        {
            _collider = GetComponent<Collider>();
            if (_collider != null)
            {
                _collider.isTrigger = true;
            }

            InitializeInventories();
        }

        private void Start()
        {
            UpdateVisuals();
        }

        private void InitializeInventories()
        {
            _fuelInventory = new InventoryData("furnace_fuel", _fuelSlots);
            _inputInventory = new InventoryData("furnace_input", _inputSlots);
            _outputInventory = new InventoryData("furnace_output", _outputSlots);
        }

        private void Update()
        {
            if (_isBurning)
            {
                ConsumeFuel();
                UpdateTemperature();
                TryStartCooking();
                UpdateCooking();
            }
            else
            {
                CoolDown();
                TryIgnite();
            }
        }

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            OpenFurnaceUI();
        }

        public string GetInteractionText()
        {
            return _isBurning ? $"Open {_furnaceName} (Burning)" : $"Open {_furnaceName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            return true;
        }

        #endregion

        private void OpenFurnaceUI()
        {
            EventManager.TriggerEvent("OnOpenFurnaceUI", this);
        }

        private void TryIgnite()
        {
            if (!HasFuel()) return;

            ItemInstance fuelItem = GetFirstFuelItem();
            if (fuelItem == null) return;

            float fuelValue = GetFuelValue(fuelItem.ItemData);
            if (fuelValue > 0)
            {
                fuelItem.CurrentStackSize--;
                if (fuelItem.IsEmpty)
                {
                    _fuelInventory.RemoveItem(fuelItem.ItemData, 1);
                }

                _maxFuelTime = fuelValue;
                _currentFuelTime = fuelValue;
                _isBurning = true;

                PlayIgniteSound();
                UpdateVisuals();
                if (_animator != null)
                {
                    _animator.SetBool("IsBurning", true);
                }
            }
        }

        private void ConsumeFuel()
        {
            if (_currentFuelTime <= 0f)
            {
                Extinguish();
                return;
            }

            _currentFuelTime -= Time.deltaTime;
        }

        private void Extinguish()
        {
            _isBurning = false;
            _currentFuelTime = 0f;
            _maxFuelTime = 0f;

            PlayExtinguishSound();
            UpdateVisuals();
            if (_animator != null)
            {
                _animator.SetBool("IsBurning", false);
            }

            _isCooking = false;
            _cookProgress = 0f;
        }

        private void UpdateTemperature()
        {
            if (_isBurning && _currentTemperature < _maxTemperature)
            {
                _currentTemperature = Mathf.Min(_currentTemperature + _heatingRate * Time.deltaTime, _maxTemperature);
            }
        }

        private void CoolDown()
        {
            if (_currentTemperature > 0f)
            {
                _currentTemperature = Mathf.Max(_currentTemperature - _coolingRate * Time.deltaTime, 0f);
            }
        }

        private void TryStartCooking()
        {
            if (_isCooking) return;
            if (_currentTemperature < _cookTemperature) return;

            ItemInstance inputItem = GetFirstInputItem();
            if (inputItem == null || inputItem.IsEmpty) return;

            RecipeData recipe = FindSmeltingRecipe(inputItem.ItemData);
            if (recipe == null) return;

            if (!CanOutputItem(recipe.ResultItem, recipe.ResultQuantity)) return;

            _currentCookingRecipe = recipe;
            _isCooking = true;
            _cookProgress = 0f;
        }

        private void UpdateCooking()
        {
            if (!_isCooking) return;
            if (_currentCookingRecipe == null)
            {
                _isCooking = false;
                return;
            }

            _cookProgress += Time.deltaTime;

            if (_cookProgress >= _currentCookingRecipe.CookTime)
            {
                CompleteCooking();
            }
        }

        private void CompleteCooking()
        {
            if (_currentCookingRecipe == null) return;

            _inputInventory.RemoveItem(_currentCookingRecipe.Ingredients[0].Item, 1);

            _outputInventory.AddItem(_currentCookingRecipe.ResultItem, _currentCookingRecipe.ResultQuantity);

            _cookProgress = 0f;
            _isCooking = false;
            _currentCookingRecipe = null;

            EventManager.TriggerEvent("OnSmeltingCompleted", this);
        }

        private bool HasFuel()
        {
            foreach (InventorySlot slot in _fuelInventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    if (GetFuelValue(slot.Item.ItemData) > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        private ItemInstance GetFirstFuelItem()
        {
            foreach (InventorySlot slot in _fuelInventory.Slots)
            {
                if (!slot.IsEmpty && GetFuelValue(slot.Item.ItemData) > 0)
                {
                    return slot.Item;
                }
            }
            return null;
        }

        private ItemInstance GetFirstInputItem()
        {
            foreach (InventorySlot slot in _inputInventory.Slots)
            {
                if (!slot.IsEmpty)
                {
                    return slot.Item;
                }
            }
            return null;
        }

        private float GetFuelValue(ItemData item)
        {
            if (item == null) return 0f;

            switch (item.ItemType)
            {
                case ItemType.Resource:
                    if (item.ItemID.Contains("wood") || item.ItemID.Contains("log"))
                        return 30f;
                    if (item.ItemID.Contains("coal") || item.ItemID.Contains("charcoal"))
                        return 80f;
                    break;
            }

            return 0f;
        }

        private RecipeData FindSmeltingRecipe(ItemData inputItem)
        {
            if (inputItem == null) return null;

            DataManager dataManager = DataManager.Instance;
            if (dataManager == null) return null;

            List<RecipeData> recipes = dataManager.GetRecipesByType(RecipeType.Smelting);
            foreach (RecipeData recipe in recipes)
            {
                if (recipe.Ingredients.Count > 0 && recipe.Ingredients[0].Item == inputItem)
                {
                    return recipe;
                }
            }

            return null;
        }

        private bool CanOutputItem(ItemData item, int quantity)
        {
            int totalSpace = 0;
            foreach (InventorySlot slot in _outputInventory.Slots)
            {
                if (slot.IsEmpty)
                {
                    totalSpace += item.MaxStackSize;
                }
                else if (slot.Item.ItemData == item)
                {
                    totalSpace += slot.SpaceLeft;
                }
            }

            return totalSpace >= quantity;
        }

        private void UpdateVisuals()
        {
            if (_inactiveVisual != null)
            {
                _inactiveVisual.SetActive(!_isBurning);
            }

            if (_activeVisual != null)
            {
                _activeVisual.SetActive(_isBurning);
            }

            if (_fireParticle != null)
            {
                if (_isBurning && !_fireParticle.isPlaying)
                {
                    _fireParticle.Play();
                }
                else if (!_isBurning && _fireParticle.isPlaying)
                {
                    _fireParticle.Stop();
                }
            }
        }

        private void PlayIgniteSound()
        {
            if (_audioSource != null && _igniteSound != null)
            {
                _audioSource.PlayOneShot(_igniteSound);
            }

            if (_audioSource != null && _fireSound != null)
            {
                _audioSource.clip = _fireSound;
                _audioSource.loop = true;
                _audioSource.Play();
            }
        }

        private void PlayExtinguishSound()
        {
            if (_audioSource != null)
            {
                _audioSource.Stop();
                _audioSource.loop = false;

                if (_extinguishSound != null)
                {
                    _audioSource.PlayOneShot(_extinguishSound);
                }
            }
        }

        public FurnaceSaveData GetSaveData()
        {
            return new FurnaceSaveData
            {
                Position = transform.position,
                Rotation = transform.rotation,
                CurrentTemperature = _currentTemperature,
                CurrentFuelTime = _currentFuelTime,
                MaxFuelTime = _maxFuelTime,
                IsBurning = _isBurning,
                IsCooking = _isCooking,
                CookProgress = _cookProgress,
                FuelInventory = _fuelInventory?.GetSaveData(),
                InputInventory = _inputInventory?.GetSaveData(),
                OutputInventory = _outputInventory?.GetSaveData()
            };
        }

        public void LoadFromSaveData(FurnaceSaveData saveData)
        {
            if (saveData == null) return;

            transform.position = saveData.Position;
            transform.rotation = saveData.Rotation;
            _currentTemperature = saveData.CurrentTemperature;
            _currentFuelTime = saveData.CurrentFuelTime;
            _maxFuelTime = saveData.MaxFuelTime;
            _isBurning = saveData.IsBurning;
            _isCooking = saveData.IsCooking;
            _cookProgress = saveData.CookProgress;

            DataManager dataManager = DataManager.Instance;
            if (saveData.FuelInventory != null)
            {
                _fuelInventory?.LoadFromSaveData(saveData.FuelInventory, dataManager);
            }
            if (saveData.InputInventory != null)
            {
                _inputInventory?.LoadFromSaveData(saveData.InputInventory, dataManager);
            }
            if (saveData.OutputInventory != null)
            {
                _outputInventory?.LoadFromSaveData(saveData.OutputInventory, dataManager);
            }

            UpdateVisuals();
        }
    }

    [System.Serializable]
    public class FurnaceSaveData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public float CurrentTemperature;
        public float CurrentFuelTime;
        public float MaxFuelTime;
        public bool IsBurning;
        public bool IsCooking;
        public float CookProgress;
        public InventorySaveData FuelInventory;
        public InventorySaveData InputInventory;
        public InventorySaveData OutputInventory;
    }
}
