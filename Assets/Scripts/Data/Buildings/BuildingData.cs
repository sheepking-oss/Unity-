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
        [SerializeField] private BuildingCategory[] _validFoundationTypes;
        [SerializeField] private bool _canBeRotated = true;
        [SerializeField] private float _rotationStep = 90f;
        [SerializeField] private bool _snapToGrid = true;
        [SerializeField] private float _gridSize = 1f;
        [SerializeField] private bool _alignToAdjacentBuildings = true;

        [Header("Placement Validation Settings")]
        [SerializeField] private PlacementValidationRule _validationRules = PlacementValidationRule.Default;

        [Header("Slope Terrain Settings")]
        [SerializeField] private bool _allowOnSlope = false;
        [SerializeField] private float _maxAllowedSlopeAngle = 15f;
        [SerializeField] private bool _useCustomSlopeLimit = false;

        [Header("Ground Height Detection")]
        [SerializeField] private int _groundSampleCountX = 5;
        [SerializeField] private int _groundSampleCountZ = 5;
        [SerializeField] private float _maxAllowedHeightVariance = 0.15f;
        [SerializeField] private bool _useCustomHeightVariance = false;
        [SerializeField] private float _groundDetectionHeight = 5f;
        [SerializeField] private float _extraDepthCheck = 2f;

        [Header("Collision Detection")]
        [SerializeField] private float _collisionMargin = 0.01f;
        [SerializeField] private bool _checkOverlapWithOtherBuildings = true;
        [SerializeField] private bool _checkOverlapWithWalls = true;
        [SerializeField] private bool _checkOverlapWithPlayer = true;

        [Header("Building Spacing")]
        [SerializeField] private float _minimumSpacingToOtherBuildings = 0.05f;
        [SerializeField] private float _minimumSpacingToWalls = 0.1f;
        [SerializeField] private bool _useCustomSpacing = false;

        [Header("Support Requirements")]
        [SerializeField] private float _requiredSupportRatio = 1f;
        [SerializeField] private bool _allowPartialSupport = false;

        [Header("Continuous Placement")]
        [SerializeField] private bool _allowContinuousPlacement = true;
        [SerializeField] private bool _autoSnapToGridLine = true;

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
        public BuildingCategory[] ValidFoundationTypes => _validFoundationTypes;
        public bool CanBeRotated => _canBeRotated;
        public float RotationStep => _rotationStep;
        public bool SnapToGrid => _snapToGrid;
        public float GridSize => _gridSize;
        public bool AlignToAdjacentBuildings => _alignToAdjacentBuildings;

        public PlacementValidationRule ValidationRules => _validationRules;

        public bool AllowOnSlope => _allowOnSlope;
        public float MaxAllowedSlopeAngle => _maxAllowedSlopeAngle;
        public bool UseCustomSlopeLimit => _useCustomSlopeLimit;

        public int GroundSampleCountX => _groundSampleCountX;
        public int GroundSampleCountZ => _groundSampleCountZ;
        public float MaxAllowedHeightVariance => _maxAllowedHeightVariance;
        public bool UseCustomHeightVariance => _useCustomHeightVariance;
        public float GroundDetectionHeight => _groundDetectionHeight;
        public float ExtraDepthCheck => _extraDepthCheck;

        public float CollisionMargin => _collisionMargin;
        public bool CheckOverlapWithOtherBuildings => _checkOverlapWithOtherBuildings;
        public bool CheckOverlapWithWalls => _checkOverlapWithWalls;
        public bool CheckOverlapWithPlayer => _checkOverlapWithPlayer;

        public float MinimumSpacingToOtherBuildings => _minimumSpacingToOtherBuildings;
        public float MinimumSpacingToWalls => _minimumSpacingToWalls;
        public bool UseCustomSpacing => _useCustomSpacing;

        public float RequiredSupportRatio => _requiredSupportRatio;
        public bool AllowPartialSupport => _allowPartialSupport;

        public bool AllowContinuousPlacement => _allowContinuousPlacement;
        public bool AutoSnapToGridLine => _autoSnapToGridLine;

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

        public bool IsValidFoundationType(BuildingCategory category)
        {
            if (_validFoundationTypes == null || _validFoundationTypes.Length == 0)
                return category == BuildingCategory.Foundation;

            foreach (BuildingCategory validCategory in _validFoundationTypes)
            {
                if (validCategory == category)
                    return true;
            }

            return false;
        }
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

    public enum PlacementValidationRule
    {
        Default = 0,
        Strict = 1,
        Relaxed = 2,
        IgnoreAll = 3
    }
}
