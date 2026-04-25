using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Items;

namespace SurvivalGame.Data.Trading
{
    [CreateAssetMenu(fileName = "NewShop", menuName = "SurvivalGame/Trading/Shop Data")]
    public class ShopData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _shopName = "Shop";
        [SerializeField] private string _shopID = "shop_0001";
        [TextArea]
        [SerializeField] private string _greetingMessage = "Welcome to my shop!";

        [Header("Shop Type")]
        [SerializeField] private ShopType _shopType = ShopType.General;
        [SerializeField] private bool _canBuy = true;
        [SerializeField] private bool _canSell = true;

        [Header("Buy Items (Player buys from shop)")]
        [SerializeField] private List<ShopItem> _buyItems = new List<ShopItem>();

        [Header("Sell Items (Player sells to shop)")]
        [SerializeField] private List<SellCategory> _sellCategories = new List<SellCategory>();
        [SerializeField] private float _sellPriceMultiplier = 0.5f;

        [Header("Restock Settings")]
        [SerializeField] private bool _restocks = true;
        [SerializeField] private int _restockIntervalDays = 1;
        [SerializeField] private float _restockTimeHour = 8f;

        #region Properties

        public string ShopName => _shopName;
        public string ShopID => _shopID;
        public string GreetingMessage => _greetingMessage;

        public ShopType ShopType => _shopType;
        public bool CanBuy => _canBuy;
        public bool CanSell => _canSell;

        public List<ShopItem> BuyItems => _buyItems;
        public List<SellCategory> SellCategories => _sellCategories;
        public float SellPriceMultiplier => _sellPriceMultiplier;

        public bool Restocks => _restocks;
        public int RestockIntervalDays => _restockIntervalDays;
        public float RestockTimeHour => _restockTimeHour;

        #endregion

        public bool CanSellItem(ItemData item)
        {
            if (item == null) return false;
            if (!_canSell) return false;

            if (_sellCategories.Count == 0)
                return true;

            foreach (SellCategory category in _sellCategories)
            {
                if (category.AcceptedItemTypes.Contains(item.ItemType))
                {
                    return true;
                }
            }

            return false;
        }

        public int GetSellPrice(ItemData item)
        {
            if (item == null) return 0;
            if (!CanSellItem(item)) return 0;

            float basePrice = item.SellPrice;
            return Mathf.Max(1, Mathf.FloorToInt(basePrice * _sellPriceMultiplier));
        }

        public int GetBuyPrice(ItemData item)
        {
            if (item == null) return 0;

            foreach (ShopItem shopItem in _buyItems)
            {
                if (shopItem.Item == item)
                {
                    return shopItem.Price;
                }
            }

            return item.BuyPrice;
        }
    }

    [System.Serializable]
    public class ShopItem
    {
        public ItemData Item;
        public int Price = 10;
        public int Quantity = 1;
        public int MaxStock = 10;
        public int CurrentStock = 10;
        public bool UnlimitedStock = false;
    }

    [System.Serializable]
    public class SellCategory
    {
        public string CategoryName;
        public List<ItemType> AcceptedItemTypes = new List<ItemType>();
        public float PriceMultiplier = 1f;
    }

    public enum ShopType
    {
        General,
        WeaponSmith,
        Armorer,
        Alchemist,
        FoodVendor,
        ResourceMerchant,
        Builder,
        Farmer
    }
}
