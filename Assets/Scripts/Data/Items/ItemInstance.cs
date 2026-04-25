using System;
using UnityEngine;
using SurvivalGame.Core.Interfaces;

namespace SurvivalGame.Data.Items
{
    [Serializable]
    public class ItemInstance : IInventoryItem
    {
        [SerializeField] private ItemData _itemData;
        [SerializeField] private int _quantity;

        public ItemData ItemData => _itemData;
        public int Quantity
        {
            get => _quantity;
            set
            {
                if (_itemData == null) return;
                _quantity = Mathf.Clamp(value, 0, _itemData.MaxStackSize);
            }
        }

        public int MaxStackSize => _itemData?.MaxStackSize ?? 0;
        public int CurrentStackSize
        {
            get => _quantity;
            set => Quantity = value;
        }
        public bool IsStackable => _itemData?.IsStackable ?? false;

        public bool IsEmpty => _itemData == null || _quantity <= 0;

        public ItemInstance()
        {
            _itemData = null;
            _quantity = 0;
        }

        public ItemInstance(ItemData itemData, int quantity = 1)
        {
            _itemData = itemData;
            _quantity = Mathf.Clamp(quantity, 0, itemData?.MaxStackSize ?? 99);
        }

        public bool CanStackWith(IInventoryItem other)
        {
            if (other == null || !IsStackable) return false;
            if (other is ItemInstance otherInstance)
            {
                return _itemData?.CanStackWith(otherInstance._itemData) ?? false;
            }
            return false;
        }

        public ItemInstance Split(int amount)
        {
            if (amount <= 0 || amount >= _quantity) return null;

            _quantity -= amount;
            return new ItemInstance(_itemData, amount);
        }

        public void Merge(ItemInstance other)
        {
            if (!CanStackWith(other)) return;

            int spaceAvailable = MaxStackSize - _quantity;
            int amountToAdd = Mathf.Min(other._quantity, spaceAvailable);

            _quantity += amountToAdd;
            other._quantity -= amountToAdd;
        }

        public ItemInstance Clone()
        {
            return new ItemInstance(_itemData, _quantity);
        }

        public static ItemInstance CreateEmpty()
        {
            return new ItemInstance();
        }
    }
}
