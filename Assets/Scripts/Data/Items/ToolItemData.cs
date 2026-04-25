using UnityEngine;

namespace SurvivalGame.Data.Items
{
    [CreateAssetMenu(fileName = "NewTool", menuName = "SurvivalGame/Items/Tool Item")]
    public class ToolItemData : ItemData
    {
        [Header("Tool Properties")]
        [SerializeField] private ToolType _toolType = ToolType.None;
        [SerializeField] private float _durability = 100f;
        [SerializeField] private float _maxDurability = 100f;
        [SerializeField] private float _efficiency = 1f;
        [SerializeField] private float _miningPower = 1f;
        [SerializeField] private float _harvestBonus = 1f;

        [Header("Tool Actions")]
        [SerializeField] private string[] _validActions;

        #region Properties

        public ToolType ToolType => _toolType;
        public float Durability => _durability;
        public float MaxDurability => _maxDurability;
        public float Efficiency => _efficiency;
        public float MiningPower => _miningPower;
        public float HarvestBonus => _harvestBonus;
        public string[] ValidActions => _validActions;

        #endregion

        public bool CanPerformAction(string actionName)
        {
            if (_validActions == null || _validActions.Length == 0)
                return false;

            foreach (string action in _validActions)
            {
                if (action.Equals(actionName, System.StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }
    }

    public enum ToolType
    {
        None,
        Axe,
        Pickaxe,
        Shovel,
        Hoe,
        Hammer,
        FishingRod
    }
}
