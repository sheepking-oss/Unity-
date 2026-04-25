using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.Inventory
{
    public class InventoryManager : Core.Managers.ManagerBase
    {
        [Header("Inventory Settings")]
        [SerializeField] private int _playerInventorySize = 36;
        [SerializeField] private int _hotbarSize = 9;
        [SerializeField] private int _equipmentSlots = 6;

        [Header("References")]
        [SerializeField] private DataManager _dataManager;

        private InventoryData _playerInventory;
        private InventoryData _hotbarInventory;
        private InventoryData _equipmentInventory;

        private int _selectedHotbarSlot = 0;

        public static InventoryManager Instance => GetInstance<InventoryManager>();

        public InventoryData PlayerInventory => _playerInventory;
        public InventoryData HotbarInventory => _hotbarInventory;
        public InventoryData EquipmentInventory => _equipmentInventory;

        public int SelectedHotbarSlot
        {
            get => _selectedHotbarSlot;
            private set
            {
                if (_selectedHotbarSlot != value)
                {
                    int oldSlot = _selectedHotbarSlot;
                    _selectedHotbarSlot = value;
                    OnHotbarSlotChanged(oldSlot, _selectedHotbarSlot);
                }
            }
        }

        public ItemInstance SelectedHotbarItem => _hotbarInventory?.GetSlot(_selectedHotbarSlot)?.Item;

        public override void Initialize()
        {
            base.Initialize();

            if (_dataManager == null)
                _dataManager = DataManager.Instance;

            CreateInventories();
        }

        private void CreateInventories()
        {
            _playerInventory = new InventoryData("player_inventory", _playerInventorySize);
            _playerInventory.OnInventoryChanged += () => EventManager.TriggerEvent(GameEvents.OnInventoryChanged);

            _hotbarInventory = new InventoryData("hotbar", _hotbarSize);
            _hotbarInventory.OnInventoryChanged += () => EventManager.TriggerEvent(GameEvents.OnInventoryChanged);

            _equipmentInventory = new InventoryData("equipment", _equipmentSlots);
            _equipmentInventory.OnInventoryChanged += () => EventManager.TriggerEvent(GameEvents.OnInventoryChanged);
        }

        public bool AddItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0)
                return false;

            ItemInstance item = new ItemInstance(itemData, quantity);
            return AddItem(item);
        }

        public bool AddItem(ItemInstance item)
        {
            if (item == null || item.IsEmpty)
                return false;

            int originalQuantity = item.CurrentStackSize;

            if (_hotbarInventory != null)
            {
                for (int i = 0; i < _hotbarInventory.SlotCount; i++)
                {
                    InventorySlot slot = _hotbarInventory.GetSlot(i);
                    if (slot != null && !slot.IsEmpty)
                    {
                        slot.AddItem(item);
                        if (item.IsEmpty) break;
                    }
                }
            }

            if (!item.IsEmpty && _playerInventory != null)
            {
                _playerInventory.AddItem(item);
            }

            return item.IsEmpty;
        }

        public bool RemoveItem(ItemData itemData, int quantity = 1)
        {
            if (itemData == null || quantity <= 0)
                return false;

            if (!HasItem(itemData, quantity))
                return false;

            int remaining = quantity;

            remaining = RemoveFromInventory(_hotbarInventory, itemData, remaining);

            if (remaining > 0)
            {
                remaining = RemoveFromInventory(_playerInventory, itemData, remaining);
            }

            return remaining <= 0;
        }

        private int RemoveFromInventory(InventoryData inventory, ItemData itemData, int amount)
        {
            if (inventory == null || amount <= 0)
                return amount;

            int remaining = amount;

            for (int i = inventory.SlotCount - 1; i >= 0; i--)
            {
                InventorySlot slot = inventory.GetSlot(i);
                if (slot == null || slot.IsEmpty) continue;

                if (slot.Item.ItemData == itemData)
                {
                    int toRemove = Mathf.Min(remaining, slot.CurrentStackSize);
                    slot.RemoveItem(toRemove);
                    remaining -= toRemove;

                    if (remaining <= 0)
                        break;
                }
            }

            return remaining;
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

            if (_hotbarInventory != null)
            {
                foreach (InventorySlot slot in _hotbarInventory.Slots)
                {
                    if (!slot.IsEmpty && slot.Item.ItemData == itemData)
                        count += slot.CurrentStackSize;
                }
            }

            if (_playerInventory != null)
            {
                foreach (InventorySlot slot in _playerInventory.Slots)
                {
                    if (!slot.IsEmpty && slot.Item.ItemData == itemData)
                        count += slot.CurrentStackSize;
                }
            }

            return count;
        }

        public void SelectHotbarSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= _hotbarSize)
                return;

            SelectedHotbarSlot = slotIndex;
        }

        private void OnHotbarSlotChanged(int oldSlot, int newSlot)
        {
            ItemInstance oldItem = _hotbarInventory.GetSlot(oldSlot)?.Item;
            ItemInstance newItem = _hotbarInventory.GetSlot(newSlot)?.Item;

            if (oldItem != null)
            {
                EventManager.TriggerEvent(GameEvents.OnItemUnequipped, oldItem);
            }

            if (newItem != null)
            {
                EventManager.TriggerEvent(GameEvents.OnItemEquipped, newItem);
            }

            EventManager.TriggerEvent("OnHotbarSlotChanged", newSlot);
        }

        public void MoveItemFromInventoryToHotbar(int inventoryIndex, int hotbarIndex)
        {
            if (_playerInventory == null || _hotbarInventory == null)
                return;

            InventorySlot invSlot = _playerInventory.GetSlot(inventoryIndex);
            InventorySlot hotSlot = _hotbarInventory.GetSlot(hotbarIndex);

            if (invSlot == null || hotSlot == null)
                return;

            invSlot.SwapWith(hotSlot);
        }

        public void MoveItemFromHotbarToInventory(int hotbarIndex, int inventoryIndex)
        {
            MoveItemFromInventoryToHotbar(inventoryIndex, hotbarIndex);
        }

        public bool CanAddItem(ItemData itemData, int quantity)
        {
            if (itemData == null || quantity <= 0)
                return false;

            int totalSpace = 0;

            int stackSize = itemData.MaxStackSize;

            if (_hotbarInventory != null)
            {
                foreach (InventorySlot slot in _hotbarInventory.Slots)
                {
                    if (slot.IsEmpty)
                    {
                        totalSpace += stackSize;
                    }
                    else if (slot.Item.ItemData == itemData && slot.Item.CanStackWith(new ItemInstance(itemData, 1)))
                    {
                        totalSpace += slot.SpaceLeft;
                    }
                }
            }

            if (_playerInventory != null)
            {
                foreach (InventorySlot slot in _playerInventory.Slots)
                {
                    if (slot.IsEmpty)
                    {
                        totalSpace += stackSize;
                    }
                    else if (slot.Item.ItemData == itemData && slot.Item.CanStackWith(new ItemInstance(itemData, 1)))
                    {
                        totalSpace += slot.SpaceLeft;
                    }
                }
            }

            return totalSpace >= quantity;
        }

        public InventorySaveData GetPlayerInventorySaveData()
        {
            return _playerInventory?.GetSaveData();
        }

        public InventorySaveData GetHotbarSaveData()
        {
            return _hotbarInventory?.GetSaveData();
        }

        public InventorySaveData GetEquipmentSaveData()
        {
            return _equipmentInventory?.GetSaveData();
        }

        public void LoadPlayerInventory(InventorySaveData saveData)
        {
            _playerInventory?.LoadFromSaveData(saveData, _dataManager);
        }

        public void LoadHotbar(InventorySaveData saveData)
        {
            _hotbarInventory?.LoadFromSaveData(saveData, _dataManager);
        }

        public void LoadEquipment(InventorySaveData saveData)
        {
            _equipmentInventory?.LoadFromSaveData(saveData, _dataManager);
        }
    }
}
