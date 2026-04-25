using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.Inventory
{
    [Serializable]
    public class InventoryData
    {
        [SerializeField] private string _inventoryID;
        [SerializeField] private int _size;
        [SerializeField] private List<InventorySlot> _slots = new List<InventorySlot>();

        public string InventoryID => _inventoryID;
        public int Size => _size;
        public int SlotCount => _slots.Count;
        public IReadOnlyList<InventorySlot> Slots => _slots.AsReadOnly();

        public event Action<InventorySlot> OnSlotChanged;
        public event Action OnInventoryChanged;

        public InventoryData(string id, int size)
        {
            _inventoryID = id;
            _size = size;
            InitializeSlots();
        }

        private void InitializeSlots()
        {
            _slots.Clear();
            for (int i = 0; i < _size; i++)
            {
                InventorySlot slot = new InventorySlot(i);
                slot.OnSlotChanged += (s) => OnSlotChanged?.Invoke(s);
                _slots.Add(slot);
            }
        }

        public InventorySlot GetSlot(int index)
        {
            if (index < 0 || index >= _slots.Count)
                return null;
            return _slots[index];
        }

        public bool AddItem(ItemInstance item)
        {
            if (item == null || item.IsEmpty)
                return false;

            int originalQuantity = item.CurrentStackSize;

            foreach (InventorySlot slot in _slots)
            {
                if (item.IsEmpty)
                    break;

                if (slot.IsEmpty)
                    continue;

                slot.AddItem(item);
            }

            if (!item.IsEmpty)
            {
                foreach (InventorySlot slot in _slots)
                {
                    if (item.IsEmpty)
                        break;

                    if (!slot.IsEmpty)
                        continue;

                    slot.AddItem(item);
                }
            }

            if (item.CurrentStackSize < originalQuantity)
            {
                OnInventoryChanged?.Invoke();
                EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
            }

            return item.IsEmpty;
        }

        public bool AddItem(ItemData itemData, int quantity)
        {
            if (itemData == null || quantity <= 0)
                return false;

            ItemInstance item = new ItemInstance(itemData, quantity);
            return AddItem(item);
        }

        public bool RemoveItem(ItemData itemData, int quantity)
        {
            if (itemData == null || quantity <= 0)
                return false;

            if (!HasItem(itemData, quantity))
                return false;

            int remaining = quantity;

            for (int i = _slots.Count - 1; i >= 0; i--)
            {
                InventorySlot slot = _slots[i];
                if (slot.IsEmpty) continue;

                if (slot.Item.ItemData == itemData)
                {
                    int toRemove = Mathf.Min(remaining, slot.CurrentStackSize);
                    slot.RemoveItem(toRemove);
                    remaining -= toRemove;

                    if (remaining <= 0)
                        break;
                }
            }

            OnInventoryChanged?.Invoke();
            EventManager.TriggerEvent(GameEvents.OnInventoryChanged);

            return remaining <= 0;
        }

        public bool HasItem(ItemData itemData, int quantity = 1)
        {
            return GetItemCount(itemData) >= quantity;
        }

        public int GetItemCount(ItemData itemData)
        {
            if (itemData == null)
                return 0;

            int count = 0;
            foreach (InventorySlot slot in _slots)
            {
                if (slot.IsEmpty) continue;
                if (slot.Item.ItemData == itemData)
                {
                    count += slot.CurrentStackSize;
                }
            }
            return count;
        }

        public int FindFirstEmptySlot()
        {
            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty)
                    return i;
            }
            return -1;
        }

        public int FindFirstSlotWithItem(ItemData itemData)
        {
            if (itemData == null)
                return -1;

            for (int i = 0; i < _slots.Count; i++)
            {
                if (_slots[i].IsEmpty) continue;
                if (_slots[i].Item.ItemData == itemData)
                    return i;
            }
            return -1;
        }

        public bool SwapSlots(int index1, int index2)
        {
            if (index1 < 0 || index1 >= _slots.Count) return false;
            if (index2 < 0 || index2 >= _slots.Count) return false;
            if (index1 == index2) return false;

            _slots[index1].SwapWith(_slots[index2]);
            OnInventoryChanged?.Invoke();

            return true;
        }

        public void Clear()
        {
            foreach (InventorySlot slot in _slots)
            {
                slot.Clear();
            }
            OnInventoryChanged?.Invoke();
        }

        public void Resize(int newSize)
        {
            if (newSize < _slots.Count)
            {
                for (int i = newSize; i < _slots.Count; i++)
                {
                    _slots[i].Clear();
                }
                _slots.RemoveRange(newSize, _slots.Count - newSize);
            }
            else
            {
                for (int i = _slots.Count; i < newSize; i++)
                {
                    InventorySlot slot = new InventorySlot(i);
                    slot.OnSlotChanged += (s) => OnSlotChanged?.Invoke(s);
                    _slots.Add(slot);
                }
            }

            _size = newSize;
            OnInventoryChanged?.Invoke();
        }

        public InventorySaveData GetSaveData()
        {
            InventorySaveData saveData = new InventorySaveData
            {
                InventoryID = _inventoryID,
                Size = _size,
                Slots = new List<InventorySlotSaveData>()
            };

            foreach (InventorySlot slot in _slots)
            {
                if (!slot.IsEmpty)
                {
                    saveData.Slots.Add(slot.GetSaveData());
                }
            }

            return saveData;
        }

        public void LoadFromSaveData(InventorySaveData saveData, DataManager dataManager)
        {
            if (saveData == null) return;
            if (dataManager == null) return;

            _inventoryID = saveData.InventoryID;
            if (saveData.Size != _size)
            {
                Resize(saveData.Size);
            }

            Clear();

            foreach (InventorySlotSaveData slotData in saveData.Slots)
            {
                ItemData itemData = dataManager.GetItem(slotData.ItemID);
                if (itemData != null && slotData.Quantity > 0)
                {
                    int slotIndex = slotData.SlotIndex;
                    if (slotIndex >= 0 && slotIndex < _slots.Count)
                    {
                        _slots[slotIndex].SetItem(new ItemInstance(itemData, slotData.Quantity));
                    }
                }
            }

            OnInventoryChanged?.Invoke();
        }
    }

    [Serializable]
    public class InventorySaveData
    {
        public string InventoryID;
        public int Size;
        public List<InventorySlotSaveData> Slots = new List<InventorySlotSaveData>();
    }
}
