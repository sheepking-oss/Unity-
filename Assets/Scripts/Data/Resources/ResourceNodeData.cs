using UnityEngine;

namespace SurvivalGame.Data.Resources
{
    [CreateAssetMenu(fileName = "NewResourceNode", menuName = "SurvivalGame/Resources/Resource Node Data")]
    public class ResourceNodeData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _nodeName = "Resource Node";
        [SerializeField] private string _nodeID = "resource_0001";

        [Header("Node Type")]
        [SerializeField] private ResourceNodeType _nodeType = ResourceNodeType.Tree;
        [SerializeField] private bool _isRenewable = true;
        [SerializeField] private float _respawnTime = 300f;

        [Header("Stats")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _miningResistance = 1f;
        [SerializeField] private ToolType _requiredToolType = ToolType.None;
        [SerializeField] private float _minimumToolPower = 0f;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private GameObject _damagedPrefab;
        [SerializeField] private Sprite _icon;

        [Header("Drops")]
        [SerializeField] private ResourceDrop[] _drops;
        [SerializeField] private bool _hasRareDrops = false;
        [SerializeField] private ResourceDrop[] _rareDrops;
        [SerializeField] private float _rareDropChance = 0.1f;

        [Header("Sound")]
        [SerializeField] private AudioClip _hitSound;
        [SerializeField] private AudioClip _destroySound;

        #region Properties

        public string NodeName => _nodeName;
        public string NodeID => _nodeID;

        public ResourceNodeType NodeType => _nodeType;
        public bool IsRenewable => _isRenewable;
        public float RespawnTime => _respawnTime;

        public float MaxHealth => _maxHealth;
        public float MiningResistance => _miningResistance;
        public ToolType RequiredToolType => _requiredToolType;
        public float MinimumToolPower => _minimumToolPower;

        public GameObject Prefab => _prefab;
        public GameObject DamagedPrefab => _damagedPrefab;
        public Sprite Icon => _icon;

        public ResourceDrop[] Drops => _drops;
        public bool HasRareDrops => _hasRareDrops;
        public ResourceDrop[] RareDrops => _rareDrops;
        public float RareDropChance => _rareDropChance;

        public AudioClip HitSound => _hitSound;
        public AudioClip DestroySound => _destroySound;

        #endregion

        public bool CanBeHarvestedWith(ToolType toolType, float toolPower)
        {
            if (_requiredToolType == ToolType.None)
                return toolPower >= _minimumToolPower;

            return toolType == _requiredToolType && toolPower >= _minimumToolPower;
        }
    }

    [System.Serializable]
    public class ResourceDrop
    {
        public Items.ItemData Item;
        public int MinQuantity = 1;
        public int MaxQuantity = 1;
        public float DropChance = 1f;
    }

    public enum ResourceNodeType
    {
        Tree,
        Rock,
        Ore,
        Bush,
        Plant,
        Animal,
        Chest,
        Barrel,
        Crate
    }
}
