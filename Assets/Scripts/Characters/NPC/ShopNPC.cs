using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Interfaces;
using SurvivalGame.Data.Trading;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Characters.NPC
{
    [RequireComponent(typeof(Collider))]
    public class ShopNPC : MonoBehaviour, IInteractable
    {
        [Header("Shop Data")]
        [SerializeField] private ShopData _shopData;
        [SerializeField] private string _npcID;

        [Header("Visuals")]
        [SerializeField] private Animator _animator;
        [SerializeField] private Transform _lookTarget;

        [Header("Audio")]
        [SerializeField] private AudioSource _audioSource;
        [SerializeField] private AudioClip _greetingSound;
        [SerializeField] private AudioClip _tradeSound;

        private Collider _collider;
        private Dictionary<string, int> _currentStock = new Dictionary<string, int>();
        private int _playerCurrency = 0;

        public ShopData ShopData => _shopData;
        public string NPCID => _npcID;
        public bool IsTrading { get; private set; }
        public int PlayerCurrency => _playerCurrency;

        private void Awake()
        {
            _collider = GetComponent<Collider>();

            if (string.IsNullOrEmpty(_npcID))
            {
                _npcID = $"npc_{GetInstanceID()}_{System.Guid.NewGuid().ToString().Substring(0, 8)}";
            }
        }

        private void Start()
        {
            InitializeStock();
        }

        private void InitializeStock()
        {
            if (_shopData == null) return;

            _currentStock.Clear();

            foreach (ShopItem shopItem in _shopData.BuyItems)
            {
                if (shopItem.Item == null) continue;

                string itemID = shopItem.Item.ItemID;
                if (!shopItem.UnlimitedStock)
                {
                    _currentStock[itemID] = shopItem.MaxStock;
                }
            }
        }

        #region IInteractable Implementation

        public void Interact(GameObject interactor)
        {
            StartTrading();
        }

        public string GetInteractionText()
        {
            if (_shopData == null) return "Shop";
            return $"Talk to {_shopData.ShopName}";
        }

        public bool CanInteract(GameObject interactor)
        {
            return _shopData != null;
        }

        #endregion

        public void StartTrading()
        {
            if (_shopData == null) return;

            IsTrading = true;
            PlayGreetingSound();

            if (_animator != null)
            {
                _animator.SetTrigger("Greet");
            }

            EventManager.TriggerEvent("OnOpenShop", this);
            EventManager.TriggerEvent(GameEvents.OnGamePaused);
        }

        public void EndTrading()
        {
            IsTrading = false;

            if (_animator != null)
            {
                _animator.SetTrigger("Farewell");
            }

            EventManager.TriggerEvent("OnCloseShop", this);
            EventManager.TriggerEvent(GameEvents.OnGameResumed);
        }

        public bool CanBuyItem(ShopItem shopItem, int quantity = 1)
        {
            if (shopItem == null) return false;
            if (shopItem.Item == null) return false;
            if (!_shopData.CanBuy) return false;

            int totalPrice = shopItem.Price * quantity;
            if (_playerCurrency < totalPrice)
                return false;

            if (!shopItem.UnlimitedStock)
            {
                string itemID = shopItem.Item.ItemID;
                if (!_currentStock.ContainsKey(itemID) || _currentStock[itemID] < quantity)
                    return false;
            }

            return true;
        }

        public bool TryBuyItem(ShopItem shopItem, int quantity = 1)
        {
            if (!CanBuyItem(shopItem, quantity))
                return false;

            int totalPrice = shopItem.Price * quantity;
            _playerCurrency -= totalPrice;

            if (!shopItem.UnlimitedStock)
            {
                string itemID = shopItem.Item.ItemID;
                _currentStock[itemID] -= quantity;
            }

            PlayTradeSound();
            EventManager.TriggerEvent("OnItemBought", shopItem.Item);

            return true;
        }

        public bool CanSellItem(ItemData item, int quantity = 1)
        {
            if (item == null) return false;
            if (!_shopData.CanSell) return false;

            return _shopData.CanSellItem(item);
        }

        public bool TrySellItem(ItemData item, int quantity = 1)
        {
            if (!CanSellItem(item, quantity))
                return false;

            int sellPrice = _shopData.GetSellPrice(item);
            int totalValue = sellPrice * quantity;
            _playerCurrency += totalValue;

            PlayTradeSound();
            EventManager.TriggerEvent("OnItemSold", item);

            return true;
        }

        public int GetCurrentStock(ItemData item)
        {
            if (item == null) return 0;

            foreach (ShopItem shopItem in _shopData.BuyItems)
            {
                if (shopItem.Item == item)
                {
                    if (shopItem.UnlimitedStock)
                        return int.MaxValue;

                    string itemID = item.ItemID;
                    if (_currentStock.ContainsKey(itemID))
                        return _currentStock[itemID];

                    return 0;
                }
            }

            return 0;
        }

        public void Restock()
        {
            if (_shopData == null) return;
            if (!_shopData.Restock) return;

            foreach (ShopItem shopItem in _shopData.BuyItems)
            {
                if (shopItem.Item == null || shopItem.UnlimitedStock) continue;

                string itemID = shopItem.Item.ItemID;
                _currentStock[itemID] = shopItem.MaxStock;
            }

            EventManager.TriggerEvent("OnShopRestocked", this);
        }

        public void SetPlayerCurrency(int amount)
        {
            _playerCurrency = Mathf.Max(0, amount);
        }

        public void AddCurrency(int amount)
        {
            _playerCurrency += amount;
        }

        public bool SpendCurrency(int amount)
        {
            if (_playerCurrency < amount)
                return false;

            _playerCurrency -= amount;
            return true;
        }

        private void PlayGreetingSound()
        {
            if (_audioSource != null && _greetingSound != null)
            {
                _audioSource.PlayOneShot(_greetingSound);
            }
        }

        private void PlayTradeSound()
        {
            if (_audioSource != null && _tradeSound != null)
            {
                _audioSource.PlayOneShot(_tradeSound);
            }
        }

        public ShopSaveData GetSaveData()
        {
            return new ShopSaveData
            {
                NPCID = _npcID,
                ShopDataID = _shopData?.ShopID,
                PlayerCurrency = _playerCurrency,
                CurrentStock = new Dictionary<string, int>(_currentStock)
            };
        }

        public void LoadFromSaveData(ShopSaveData saveData)
        {
            if (saveData == null) return;

            _npcID = saveData.NPCID;
            _playerCurrency = saveData.PlayerCurrency;

            if (saveData.CurrentStock != null)
            {
                _currentStock = new Dictionary<string, int>(saveData.CurrentStock);
            }
        }
    }

    [System.Serializable]
    public class ShopSaveData
    {
        public string NPCID;
        public string ShopDataID;
        public int PlayerCurrency;
        public Dictionary<string, int> CurrentStock;
    }
}
