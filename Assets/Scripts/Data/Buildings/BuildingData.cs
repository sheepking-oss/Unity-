using UnityEngine;

namespace SurvivalGame.Data.Buildings
{
    [CreateAssetMenu(fileName = "NewBuilding", menuName = "SurvivalGame/Buildings/Building Data")]
    public class BuildingData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _buildingName = "New Building";
        [SerializeField] private string _buildingID = "building_0001";
        [TextArea(3, 5)]
        [SerializeField] private string _description = "Building description";

        [Header("Building Type")]
        [SerializeField] private BuildingType _buildingType = BuildingType.Structure;
        [SerializeField] private BuildingCategory _category = BuildingCategory.Wall;

        [Header("Visuals")]
        [SerializeField] private GameObject _prefab;
        [SerializeField] private Sprite _icon;
        [SerializeField] private Color _previewColor = new Color(0.5f, 0.9f, 1f, 0.8f);
        [SerializeField] private Color _invalidPlacementColor = new Color(1f, 0.3f, 0.3f, 0.8f);

        [Header("Placement")]
        [SerializeField] private Vector3 _placementOffset = Vector3.zero;
        [SerializeField] private Vector3 _size = Vector3.one;
        [SerializeField] private bool _requiresFoundation = false;
        [SerializeField] private bool _canBeRotated = true;
        [SerializeField] private float _rotationStep = 90f;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _gridSize = 1f;

        [Header("Properties")]
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _fireResistance = 0f;
        [SerializeField] private bool _isSolid = true;
        [SerializeField] private LayerMask _collisionLayers;

        [Header("Functionality")]
        [SerializeField] private bool _hasInventory = false;
        [SerializeField] private int _inventorySize = 0;
        [SerializeField] private bool _isCraftingStation = false;
        [SerializeField] private string _requiredStationType;
        [SerializeField] private bool _isContainer = false;
        [SerializeField] private bool _canBeLocked = false;

        [Header("Cost")]
        [SerializeField] private BuildingCost[] _buildCost;
        [SerializeField] private float _buildTime = 1f;

        #region Properties

        public string BuildingName => _buildingName;
        public string BuildingID => _buildingID;
        public string Description => _description;

        public BuildingType BuildingType => _buildingType;
        public BuildingCategory Category => _category;

        public GameObject Prefab => _prefab;
        public Sprite Icon => _icon;
        public Color PreviewColor => _previewColor;
        public Color InvalidPlacementColor => _invalidPlacementColor;

        public Vector3 PlacementOffset => _placementOffset;
        public Vector3 Size => _size;
        public bool RequiresFoundation => _requiresFoundation;
        public bool CanBeRotated => _canBeRotated;
        public float RotationStep => _rotationStep;
        public bool SnapToGrid => _snapToGrid;
        public float GridSize => _gridSize;

        public float MaxHealth => _maxHealth;
        public float FireResistance => _fireResistance;
        public bool IsSolid => _isSolid;
        public LayerMask CollisionLayers => _collisionLayers;

        public bool HasInventory => _hasInventory;
        public int InventorySize => _inventorySize;
        public bool IsCraftingStation => _isCraftingStation;
        public string RequiredStationType => _requiredStationType;
        public bool IsContainer => _isContainer;
        public bool CanBeLocked => _canBeLocked;

        public BuildingCost[] BuildCost => _buildCost;
        public float BuildTime => _buildTime;

        #endregion
    }

    [System.Serializable]
    public class BuildingCost
    {
        public Data.Items.ItemData Item;
        public int Quantity;
    }

    public enum BuildingType
    {
        Structure,
        Furniture,
        CraftingStation,
        Container,
        Decoration,
        Utility
    }

    public enum BuildingCategory
    {
        Foundation,
        Wall,
        Floor,
        Roof,
        Door,
        Window,
        Stairs,
        Fence,
        Workbench,
        Furnace,
        Storage,
        Bed,
        Torch,
        Other
    }
}
