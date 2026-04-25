using UnityEngine;

namespace SurvivalGame.Data.Items
{
    public enum ItemType
    {
        Resource,
        Tool,
        Weapon,
        Consumable,
        Equipment,
        BuildingMaterial,
        Miscellaneous
    }

    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    public enum EquipmentSlot
    {
        None,
        Head,
        Chest,
        Legs,
        Feet,
        Hand,
        OffHand,
        Toolbelt
    }
}
