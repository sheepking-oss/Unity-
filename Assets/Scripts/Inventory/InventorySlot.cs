using System;
using System.Collections.Generic;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Inventory
{
    [Serializable]
    public class InventorySlot
    {
        [SerializeField] private ItemInstance _item;
        [SerializeField] private int _slotIndex;

        public ItemInstance Item => _item;
        public int SlotIndex => _slotIndex;
        public bool IsEmpty => _item == null || _item.IsEmpty;
        public int CurrentStackSize => _item?.CurrentStackSize ?? 0;
        public int MaxStackSize => _item?.MaxStackSize ?? 0;
        public int SpaceLeft => IsEmpty ? 0 : MaxStackSize - CurrentStackSize;

        public event Action<InventorySlot> OnSlotChanged;

        public InventorySlot(int index)
        {
            _slotIndex = index;
            _item = ItemInstance.CreateEmpty();
        }

        public void SetItem(ItemInstance newItem)
        {
            _item = newItem ?? ItemInstance.CreateEmpty();
            OnSlotChanged?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
        }

        public void Clear()
        {
            _item = ItemInstance.CreateEmpty();
            OnSlotChanged?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
        }

        public int AddItem(ItemInstance itemToAdd)
        {
            if (itemToAdd == null || itemToAdd.IsEmpty)
                return 0;

            if (IsEmpty)
            {
                _item = itemToAdd.Clone();
                itemToAdd.CurrentStackSize = 0;
                OnSlotChanged?.Invoke(this);
                EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
                return _item.CurrentStackSize;
            }

            if (!_item.CanStackWith(itemToAdd))
                return 0;

            int spaceLeft = SpaceLeft;
            int amountToAdd = Mathf.Min(spaceLeft, itemToAdd.CurrentStackSize);

            _item.CurrentStackSize += amountToAdd;
            itemToAdd.CurrentStackSize -= amountToAdd;

            OnSlotChanged?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);

            return amountToAdd;
        }

        public ItemInstance RemoveItem(int amount)
        {
            if (IsEmpty || amount <= 0)
                return null;

            int amountToRemove = Mathf.Min(amount, _item.CurrentStackSize);
            ItemInstance removedItem = _item.Split(amountToRemove);

            OnSlotChanged?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);

            return removedItem;
        }

        public void SwapWith(InventorySlot other)
        {
            if (other == this) return;

            ItemInstance temp = _item;
            _item = other._item;
            other._item = temp;

            OnSlotChanged?.Invoke(this);
            other.OnSlotChanged?.Invoke(other);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
        }

        public void MergeWith(InventorySlot other)
        {
            if (other == this || other.IsEmpty) return;
            if (IsEmpty || !_item.CanStackWith(other._item)) return;

            int spaceLeft = SpaceLeft;
            if (spaceLeft <= 0) return;

            int amountToMerge = Mathf.Min(spaceLeft, other._item.CurrentStackSize);
            _item.CurrentStackSize += amountToMerge;
            other._item.CurrentStackSize -= amountToMerge;

            if (other._item.IsEmpty)
            {
                other.Clear();
            }

            OnSlotChanged?.Invoke(this);
            other.OnSlotChanged?.Invoke(other);
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
        }

        public InventorySlotSaveData GetSaveData()
        {
            return new InventorySlotSaveData
            {
                SlotIndex = _slotIndex,
                ItemID = _item?.ItemData?.ItemID,
                Quantity = _item?.CurrentStackSize ?? 0
            };
        }
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public int SlotIndex;
        public string ItemID;
        public int Quantity;
    }
}
