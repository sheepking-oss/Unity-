using UnityEngine;
using SurvivalGame.Core.Interfaces;

namespace SurvivalGame.Data.Items
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "SurvivalGame/Items/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _itemName = "New Item";
        [SerializeField] private string _itemID = "item_0001";
        [TextArea(3, 5)]
        [SerializeField] private string _description = "Item description";

        [Header("Item Properties")]
        [SerializeField] private ItemType _itemType = ItemType.Miscellaneous;
        [SerializeField] private ItemRarity _rarity = ItemRarity.Common;
        [SerializeField] private int _maxStackSize = 99;
        [SerializeField] private bool _isStackable = true;
        [SerializeField] private float _weight = 0.1f;

        [Header("Visuals")]
        [SerializeField] private Sprite _icon;
        [SerializeField] private GameObject _worldPrefab;
        [SerializeField] private Color _tintColor = Color.white;

        [Header("Equip Settings")]
        [SerializeField] private EquipmentSlot _equipmentSlot = EquipmentSlot.None;
        [SerializeField] private GameObject _equipPrefab;

        [Header("Usage Settings")]
        [SerializeField] private bool _canBeUsed = false;
        [SerializeField] private bool _canBeDropped = true;
        [SerializeField] private bool _canBeSold = true;
        [SerializeField] private int _buyPrice = 1;
        [SerializeField] private int _sellPrice = 1;

        #region Properties

        public string ItemName => _itemName;
        public string ItemID => _itemID;
        public string Description => _description;

        public ItemType ItemType => _itemType;
        public ItemRarity Rarity => _rarity;
        public int MaxStackSize => _maxStackSize;
        public bool IsStackable => _isStackable;
        public float Weight => _weight;

        public Sprite Icon => _icon;
        public GameObject WorldPrefab => _worldPrefab;
        public Color TintColor => _tintColor;

        public EquipmentSlot EquipmentSlot => _equipmentSlot;
        public GameObject EquipPrefab => _equipPrefab;

        public bool CanBeUsed => _canBeUsed;
        public bool CanBeDropped => _canBeDropped;
        public bool CanBeSold => _canBeSold;
        public int BuyPrice => _buyPrice;
        public int SellPrice => _sellPrice;

        #endregion

        public virtual ItemInstance CreateInstance(int quantity = 1)
        {
            return new ItemInstance(this, Mathf.Min(quantity, _maxStackSize));
        }

        public virtual bool CanStackWith(ItemData other)
        {
            if (other == null) return false;
            return _isStackable && _itemID == other._itemID;
        }
    }
}
