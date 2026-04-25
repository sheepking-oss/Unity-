using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Inventory
{
    public class EquipmentManager : MonoBehaviour
    {
        [Header("Equipment Slots")]
        [SerializeField] private EquipmentSlot[] _equipmentSlots;

        [Header("References")]
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private Transform _equipmentRoot;

        private Dictionary<EquipmentSlot, GameObject> _equippedObjects = new Dictionary<EquipmentSlot, GameObject>();
        private Dictionary<EquipmentSlot, ItemInstance> _equippedItems = new Dictionary<EquipmentSlot, ItemInstance>();

        public delegate void EquipmentChangedEventHandler(EquipmentSlot slot, ItemInstance oldItem, ItemInstance newItem);
        public event EquipmentChangedEventHandler OnEquipmentChanged;

        private void Awake()
        {
            if (_inventoryManager == null)
                _inventoryManager = InventoryManager.Instance;

            InitializeEquipmentSlots();
        }

        private void InitializeEquipmentSlots()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot == EquipmentSlot.None) continue;

                _equippedObjects[slot] = null;
                _equippedItems[slot] = null;
            }
        }

        public bool EquipItem(ItemInstance item, EquipmentSlot targetSlot = EquipmentSlot.None)
        {
            if (item == null || item.IsEmpty)
                return false;

            if (item.ItemData == null)
                return false;

            EquipmentSlot itemSlot = item.ItemData.EquipmentSlot;
            if (itemSlot == EquipmentSlot.None)
                return false;

            EquipmentSlot slotToUse = targetSlot != EquipmentSlot.None ? targetSlot : itemSlot;

            if (_equippedItems.TryGetValue(slotToUse, out ItemInstance currentItem))
            {
                if (currentItem != null && !currentItem.IsEmpty)
                {
                    if (_inventoryManager != null)
                    {
                        _inventoryManager.AddItem(currentItem);
                    }
                    UnequipItem(slotToUse);
                }
            }

            _equippedItems[slotToUse] = item;
            SpawnEquippedObject(item, slotToUse);

            OnEquipmentChanged?.Invoke(slotToUse, currentItem, item);
            EventManager.TriggerEvent("OnEquipmentChanged", slotToUse);

            return true;
        }

        public bool UnequipItem(EquipmentSlot slot)
        {
            if (slot == EquipmentSlot.None)
                return false;

            if (!_equippedItems.TryGetValue(slot, out ItemInstance item))
                return false;

            if (item == null || item.IsEmpty)
                return false;

            if (_inventoryManager != null && !_inventoryManager.AddItem(item))
            {
                return false;
            }

            DestroyEquippedObject(slot);
            ItemInstance oldItem = _equippedItems[slot];
            _equippedItems[slot] = null;

            OnEquipmentChanged?.Invoke(slot, oldItem, null);
            EventManager.TriggerEvent("OnEquipmentChanged", slot);

            return true;
        }

        public ItemInstance GetEquippedItem(EquipmentSlot slot)
        {
            if (_equippedItems.TryGetValue(slot, out ItemInstance item))
            {
                return item;
            }
            return null;
        }

        public GameObject GetEquippedObject(EquipmentSlot slot)
        {
            if (_equippedObjects.TryGetValue(slot, out GameObject obj))
            {
                return obj;
            }
            return null;
        }

        public T GetEquippedItem<T>(EquipmentSlot slot) where T : ItemData
        {
            ItemInstance item = GetEquippedItem(slot);
            return item?.ItemData as T;
        }

        private void SpawnEquippedObject(ItemInstance item, EquipmentSlot slot)
        {
            if (item == null || item.ItemData == null)
                return;

            if (item.ItemData.EquipPrefab == null)
                return;

            DestroyEquippedObject(slot);

            Transform parent = GetSlotParent(slot);
            if (parent == null)
                parent = _equipmentRoot ?? transform;

            GameObject equippedObject = Instantiate(item.ItemData.EquipPrefab, parent);
            equippedObject.transform.localPosition = Vector3.zero;
            equippedObject.transform.localRotation = Quaternion.identity;

            _equippedObjects[slot] = equippedObject;
        }

        private void DestroyEquippedObject(EquipmentSlot slot)
        {
            if (_equippedObjects.TryGetValue(slot, out GameObject obj) && obj != null)
            {
                Destroy(obj);
                _equippedObjects[slot] = null;
            }
        }

        private Transform GetSlotParent(EquipmentSlot slot)
        {
            if (_equipmentSlots == null)
                return null;

            foreach (var slotData in _equipmentSlots)
            {
                if (slotData.Slot == slot)
                {
                    return slotData.Parent;
                }
            }

            return null;
        }

        public void UnequipAll()
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                if (slot != EquipmentSlot.None)
                {
                    UnequipItem(slot);
                }
            }
        }

        public EquipmentSaveData GetSaveData()
        {
            EquipmentSaveData saveData = new EquipmentSaveData
            {
                EquippedItems = new Dictionary<int, EquipmentSlotSaveData>()
            };

            foreach (var kvp in _equippedItems)
            {
                if (kvp.Value != null && !kvp.Value.IsEmpty)
                {
                    saveData.EquippedItems[(int)kvp.Key] = new EquipmentSlotSaveData
                    {
                        Slot = (int)kvp.Key,
                        ItemID = kvp.Value.ItemData?.ItemID,
                        Quantity = kvp.Value.CurrentStackSize
                    };
                }
            }

            return saveData;
        }
    }

    [System.Serializable]
    public struct EquipmentSlot
    {
        public EquipmentSlot Slot;
        public Transform Parent;
        public string BoneName;
    }

    [System.Serializable]
    public class EquipmentSaveData
    {
        public Dictionary<int, EquipmentSlotSaveData> EquippedItems;
    }

    [System.Serializable]
    public class EquipmentSlotSaveData
    {
        public int Slot;
        public string ItemID;
        public int Quantity;
    }
}
