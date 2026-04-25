using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Buildings;
using SurvivalGame.Player.Controllers;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Inventory;
using SurvivalGame.Core.Events;
using SurvivalGame.World.Buildings;
using SurvivalGame.World.Managers;

namespace SurvivalGame.Player.Systems
{
    [System.Serializable]
    public class PlacementHistoryRecord
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public string BuildingID;
        public float Timestamp;
        public int PlacementIndex;

        public PlacementHistoryRecord(Vector3 position, Quaternion rotation, string buildingID, int index)
        {
            Position = position;
            Rotation = rotation;
            BuildingID = buildingID;
            Timestamp = Time.time;
            PlacementIndex = index;
        }

        public Vector3 GetGridAlignedPosition(float gridSize)
        {
            return new Vector3(
                Mathf.Round(Position.x / gridSize) * gridSize,
                Mathf.Round(Position.y * 1000f) / 1000f,
                Mathf.Round(Position.z / gridSize) * gridSize
            );
        }
    }

    public class PlayerBuilding : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private ThirdPersonCamera _camera;
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private BuildingManager _buildingManager;

        [Header("Placement Settings")]
        [SerializeField] private BuildingPlacementSettings _placementSettings;

        [Header("Building Settings")]
        [SerializeField] private float _maxBuildDistance = 10f;
        [SerializeField] private float _yOffset = 0.01f;
        [SerializeField] private LayerMask _groundLayers;
        [SerializeField] private LayerMask _obstacleLayers;

        [Header("Preview Settings")]
        [SerializeField] private Material _validPlacementMaterial;
        [SerializeField] private Material _invalidPlacementMaterial;
        [SerializeField] private float _previewYOffset = 0.05f;

        [Header("Rotation Settings")]
        [SerializeField] private float _rotationStep = 90f;

        [Header("Continuous Placement Settings")]
        [SerializeField] private int _maxHistoryRecords = 50;
        [SerializeField] private float _duplicatePositionThreshold = 0.01f;
        [SerializeField] private bool _enableEdgeSnap = true;
        [SerializeField] private float _edgeSnapThreshold = 0.3f;

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private BuildingData _selectedBuilding;
        private GameObject _previewObject;
        private Renderer[] _previewRenderers;
        private bool _isInBuildMode = false;
        private float _currentRotation = 0f;
        private Vector3 _currentPosition;
        private Quaternion _currentRotationQuaternion => Quaternion.Euler(0f, _currentRotation, 0f);
        private PlacementValidationResult _lastValidationResult;
        private string _lastValidationMessage;

        private List<Collider> _previewColliders = new List<Collider>();
        private List<GroundSamplePoint> _lastSamplePoints = new List<GroundSamplePoint>();
        private List<PlacementHistoryRecord> _placementHistory = new List<PlacementHistoryRecord>();
        private int _totalPlacementsThisSession = 0;
        private int _consecutivePlacements = 0;
        private Vector3 _lastPlacementPosition;
        private bool _isPlacementStable = true;

        public bool IsInBuildMode => _isInBuildMode;
        public BuildingData SelectedBuilding => _selectedBuilding;
        public bool IsValidPlacement => _lastValidationResult != null && _lastValidationResult.IsValid;
        public string ValidationMessage => _lastValidationMessage;
        public float LastSlopeAngle => _lastValidationResult?.SlopeAngle ?? 0f;
        public float LastHeightVariance => _lastValidationResult?.HeightVariance ?? 0f;

        private void Awake()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_inventoryManager == null)
                _inventoryManager = InventoryManager.Instance;
            if (_buildingManager == null)
                _buildingManager = BuildingManager.Instance;
            if (_placementSettings == null)
                _placementSettings = ScriptableObject.CreateInstance<BuildingPlacementSettings>();

            _inputManager = InputManager.Instance;
            _gameStateManager = GameStateManager.Instance;
        }

        private void Update()
        {
            if (!_isInBuildMode)
            {
                CheckBuildModeInput();
                return;
            }

            UpdateBuildMode();
        }

        private void CheckBuildModeInput()
        {
            if (_inputManager != null && _inputManager.BuildModePressed)
            {
                if (_selectedBuilding != null)
                {
                    EnterBuildMode();
                }
            }
        }

        private void UpdateBuildMode()
        {
            if (_selectedBuilding == null)
            {
                ExitBuildMode();
                return;
            }

            UpdatePreviewPosition();
            UpdatePreviewRotation();

            ValidateCurrentPlacement();
            UpdatePreviewVisuals();

            CheckPlacementInput();
            CheckExitInput();
        }

        private void UpdatePreviewPosition()
        {
            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, _maxBuildDistance, _groundLayers))
            {
                _currentPosition = hit.point;

                if (_selectedBuilding.SnapToGrid && _placementSettings.SnapToGrid)
                {
                    float gridSize = _selectedBuilding.GridSize;

                    bool snapped = false;

                    if (_enableEdgeSnap && _buildingManager != null)
                    {
                        List<Building> nearbyBuildings = _buildingManager.GetBuildingsInRange(
                            _currentPosition,
                            _placementSettings.AdjacentBuildingSearchRadius
                        );

                        Vector3 edgeSnapPosition = SnapToBuildingEdges(_currentPosition, gridSize, nearbyBuildings);
                        if (edgeSnapPosition != _currentPosition)
                        {
                            _currentPosition = edgeSnapPosition;
                            snapped = true;
                        }
                    }

                    if (!snapped && _placementSettings.AlignToAdjacentBuildings && _buildingManager != null)
                    {
                        List<Building> nearbyBuildings = _buildingManager.GetBuildingsInRange(
                            _currentPosition,
                            _placementSettings.AdjacentBuildingSearchRadius
                        );

                        _currentPosition = BuildingPlacementValidator.AlignToAdjacentBuildings(
                            _currentPosition,
                            gridSize,
                            _placementSettings.AdjacentBuildingSearchRadius,
                            nearbyBuildings
                        );
                    }
                    else if (!snapped)
                    {
                        _currentPosition = SnapToGridPrecise(_currentPosition, gridSize);
                    }
                }

                _currentPosition += _selectedBuilding.PlacementOffset;
                _currentPosition.y += _yOffset;
            }
            else
            {
                _currentPosition = ray.origin + ray.direction * _maxBuildDistance;
            }

            if (_previewObject != null)
            {
                _previewObject.transform.position = _currentPosition + Vector3.up * _previewYOffset;
            }
        }

        private Vector3 SnapToBuildingEdges(Vector3 position, float gridSize, List<Building> nearbyBuildings)
        {
            if (nearbyBuildings == null || nearbyBuildings.Count == 0)
                return position;

            Vector3 buildingSize = _selectedBuilding.Size;
            Vector3 halfSize = buildingSize * 0.5f;

            Vector3 bestSnap = position;
            float bestDistance = _edgeSnapThreshold;

            foreach (Building building in nearbyBuildings)
            {
                if (building == null || building.BuildingData == null) continue;

                Vector3 otherPos = building.transform.position + building.BuildingData.PlacementOffset;
                Vector3 otherHalfSize = building.BuildingData.Size * 0.5f;

                float[] xEdges = new float[]
                {
                    otherPos.x - otherHalfSize.x - halfSize.x,
                    otherPos.x + otherHalfSize.x + halfSize.x
                };

                float[] zEdges = new float[]
                {
                    otherPos.z - otherHalfSize.z - halfSize.z,
                    otherPos.z + otherHalfSize.z + halfSize.z
                };

                foreach (float edgeX in xEdges)
                {
                    float xDist = Mathf.Abs(position.x - edgeX);
                    if (xDist < bestDistance)
                    {
                        float zAligned = Mathf.Round(position.z / gridSize) * gridSize;
                        Vector3 snapPos = new Vector3(edgeX, position.y, zAligned);
                        float totalDist = Vector3.Distance(position, snapPos);
                        if (totalDist < bestDistance)
                        {
                            bestDistance = totalDist;
                            bestSnap = snapPos;
                        }
                    }
                }

                foreach (float edgeZ in zEdges)
                {
                    float zDist = Mathf.Abs(position.z - edgeZ);
                    if (zDist < bestDistance)
                    {
                        float xAligned = Mathf.Round(position.x / gridSize) * gridSize;
                        Vector3 snapPos = new Vector3(xAligned, position.y, edgeZ);
                        float totalDist = Vector3.Distance(position, snapPos);
                        if (totalDist < bestDistance)
                        {
                            bestDistance = totalDist;
                            bestSnap = snapPos;
                        }
                    }
                }
            }

            return bestSnap;
        }

        private Vector3 SnapToGridPrecise(Vector3 position, float gridSize)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                position.y,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        private void UpdatePreviewRotation()
        {
            if (_inputManager != null && _inputManager.RotateBuildingPressed)
            {
                if (_selectedBuilding != null && _selectedBuilding.CanBeRotated)
                {
                    _currentRotation += _selectedBuilding.RotationStep;
                    if (_currentRotation >= 360f)
                        _currentRotation -= 360f;
                    else if (_currentRotation < 0f)
                        _currentRotation += 360f;
                }
            }

            if (_previewObject != null)
            {
                _previewObject.transform.rotation = _currentRotationQuaternion;
            }
        }

        private void ValidateCurrentPlacement()
        {
            if (_selectedBuilding == null)
            {
                _lastValidationResult = new PlacementValidationResult();
                _lastValidationResult.AddError(PlacementValidationError.PositionOutOfRange);
                _lastValidationMessage = "No building selected";
                return;
            }

            List<Building> existingBuildings = _buildingManager?.GetBuildingsInRange(_currentPosition, _maxBuildDistance);

            _lastValidationResult = BuildingPlacementValidator.ValidatePlacement(
                _selectedBuilding,
                _currentPosition,
                _currentRotationQuaternion,
                _placementSettings,
                _groundLayers,
                _obstacleLayers,
                transform,
                existingBuildings
            );

            _lastValidationMessage = _lastValidationResult.ErrorMessage;

            if (_placementSettings.LogValidationErrors && !_lastValidationResult.IsValid)
            {
                Debug.LogWarning($"[BuildingPlacement] Validation failed: {_lastValidationMessage}");
            }
        }

        private void UpdatePreviewVisuals()
        {
            if (_previewRenderers == null || _previewRenderers.Length == 0)
                return;

            Material material = IsValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial;

            foreach (Renderer renderer in _previewRenderers)
            {
                if (renderer != null)
                {
                    Material[] materials = renderer.materials;
                    for (int i = 0; i < materials.Length; i++)
                    {
                        materials[i] = material;
                    }
                    renderer.materials = materials;
                }
            }
        }

        private void CheckPlacementInput()
        {
            if (_inputManager != null && _inputManager.AttackPressed)
            {
                if (IsValidPlacement)
                {
                    TryPlaceBuilding();
                }
            }
        }

        private void CheckExitInput()
        {
            if (_inputManager != null && _inputManager.PausePressed)
            {
                ExitBuildMode();
            }
        }

        public void EnterBuildMode()
        {
            if (_selectedBuilding == null) return;

            _isInBuildMode = true;
            _currentRotation = 0f;
            _consecutivePlacements = 0;
            _isPlacementStable = true;
            _lastPlacementPosition = Vector3.zero;
            _placementHistory.Clear();

            _gameStateManager?.ChangeState(GameState.Building);

            CreatePreviewObject();

            EventManager.TriggerEvent("OnEnterBuildMode", _selectedBuilding);

            if (_placementSettings.LogValidationErrors)
            {
                Debug.Log($"[PlayerBuilding] Entered build mode with: {_selectedBuilding.BuildingName}");
            }
        }

        public void ExitBuildMode()
        {
            _isInBuildMode = false;
            DestroyPreviewObject();

            if (_gameStateManager != null && _gameStateManager.CurrentState == GameState.Building)
            {
                _gameStateManager.ReturnToPreviousState();
            }

            EventManager.TriggerEvent("OnExitBuildMode");
        }

        public void SelectBuilding(BuildingData buildingData)
        {
            if (buildingData == null) return;

            _selectedBuilding = buildingData;
            _consecutivePlacements = 0;
            _isPlacementStable = true;
            _placementHistory.Clear();

            if (_isInBuildMode)
            {
                DestroyPreviewObject();
                CreatePreviewObject();
            }

            EventManager.TriggerEvent("OnBuildingSelected", _selectedBuilding);

            if (_placementSettings.LogValidationErrors)
            {
                Debug.Log($"[PlayerBuilding] Selected building: {buildingData.BuildingName}");
            }
        }

        private void CreatePreviewObject()
        {
            if (_selectedBuilding == null || _selectedBuilding.Prefab == null)
                return;

            DestroyPreviewObject();

            _previewObject = Instantiate(_selectedBuilding.Prefab, _currentPosition, _currentRotationQuaternion);
            _previewObject.layer = LayerMask.NameToLayer("Ignore Raycast");

            _previewRenderers = _previewObject.GetComponentsInChildren<Renderer>(true);
            _previewColliders = new List<Collider>(_previewObject.GetComponentsInChildren<Collider>(true));

            foreach (Collider col in _previewColliders)
            {
                col.enabled = false;
            }

            UpdatePreviewVisuals();
        }

        private void DestroyPreviewObject()
        {
            if (_previewObject != null)
            {
                Destroy(_previewObject);
                _previewObject = null;
            }

            _previewRenderers = null;
            _previewColliders.Clear();
        }

        private bool TryPlaceBuilding()
        {
            if (_selectedBuilding == null) return false;

            if (!HasRequiredMaterials())
            {
                Debug.Log("Not enough materials to build!");
                return false;
            }

            if (_lastValidationResult == null || !_lastValidationResult.IsValid)
            {
                Debug.LogWarning($"[PlayerBuilding] Cannot place - validation failed: {_lastValidationMessage}");
                return false;
            }

            if (!CheckPlacementStability())
            {
                Debug.LogWarning("[PlayerBuilding] Placement position unstable, skipping placement");
                return false;
            }

            Vector3 finalPosition = _lastValidationResult.SuggestedPosition;
            if (finalPosition == Vector3.zero)
            {
                finalPosition = _currentPosition;
            }

            finalPosition = new Vector3(
                finalPosition.x,
                _lastValidationResult.GroundHeight + _yOffset,
                finalPosition.z
            );

            ConsumeMaterials();

            Quaternion finalRotation = _currentRotationQuaternion;

            GameObject buildingObject = Instantiate(
                _selectedBuilding.Prefab,
                finalPosition,
                finalRotation
            );

            Building building = buildingObject.GetComponent<Building>();
            if (building != null)
            {
                building.Initialize(_selectedBuilding);
                RegisterPlacedBuilding(building, finalPosition);
            }

            EventManager.TriggerEvent(GameEvents.OnBuildingPlaced, building);

            Debug.Log($"[PlayerBuilding] Placed building: {_selectedBuilding.BuildingName} at {finalPosition:F1}");

            return true;
        }

        private void RegisterPlacedBuilding(Building building, Vector3 position)
        {
            _consecutivePlacements++;
            _totalPlacementsThisSession++;
            _lastPlacementPosition = position;

            AddToPlacementHistory(position, _currentRotationQuaternion, _selectedBuilding.BuildingID);

            if (_consecutivePlacements >= 5)
            {
                if (!VerifyPlacedBuildings())
                {
                    _isPlacementStable = false;
                    Debug.LogWarning($"[PlayerBuilding] Placement instability detected after {_consecutivePlacements} placements!");
                }
                else
                {
                    _consecutivePlacements = 0;
                }
            }
        }

        private void AddToPlacementHistory(Vector3 position, Quaternion rotation, string buildingID)
        {
            PlacementHistoryRecord record = new PlacementHistoryRecord(
                position,
                rotation,
                buildingID,
                _totalPlacementsThisSession
            );

            _placementHistory.Add(record);

            while (_placementHistory.Count > _maxHistoryRecords)
            {
                _placementHistory.RemoveAt(0);
            }
        }

        private bool CheckPlacementStability()
        {
            if (_consecutivePlacements == 0)
            {
                return true;
            }

            if (_selectedBuilding == null)
                return true;

            float gridSize = _selectedBuilding.GridSize;
            Vector3 currentGridPos = SnapToGridPrecise(_currentPosition, gridSize);

            if (IsDuplicatePosition(currentGridPos, gridSize))
            {
                Debug.LogWarning($"[PlayerBuilding] Duplicate position detected: {currentGridPos}");
                return false;
            }

            float minSpacing = gridSize * 0.01f;
            float distanceFromLast = Vector3.Distance(currentGridPos, SnapToGridPrecise(_lastPlacementPosition, gridSize));

            if (distanceFromLast < minSpacing && _consecutivePlacements > 1)
            {
                Debug.LogWarning($"[PlayerBuilding] Same position as previous placement (distance: {distanceFromLast:F3})");
                return false;
            }

            return true;
        }

        private bool IsDuplicatePosition(Vector3 gridPosition, float gridSize)
        {
            float threshold = _duplicatePositionThreshold;

            foreach (PlacementHistoryRecord record in _placementHistory)
            {
                Vector3 recordGridPos = record.GetGridAlignedPosition(gridSize);

                float xDiff = Mathf.Abs(recordGridPos.x - gridPosition.x);
                float zDiff = Mathf.Abs(recordGridPos.z - gridPosition.z);
                float yDiff = Mathf.Abs(recordGridPos.y - gridPosition.y);

                if (xDiff < threshold && zDiff < threshold && yDiff < threshold * 10f)
                {
                    return true;
                }
            }

            return false;
        }

        private bool VerifyPlacedBuildings()
        {
            if (_buildingManager == null)
                return true;

            List<Building> recentBuildings = _buildingManager.GetBuildingsInRange(_currentPosition, 50f);
            HashSet<Vector3> positions = new HashSet<Vector3>();

            foreach (Building building in recentBuildings)
            {
                if (building == null || building.BuildingData == null)
                    continue;

                Vector3 pos = RoundToGrid(building.transform.position, building.BuildingData.GridSize);

                if (positions.Contains(pos))
                {
                    Debug.LogError($"[PlayerBuilding] Duplicate building at position: {pos}");
                    return false;
                }

                positions.Add(pos);

                if (!CheckBuildingSupport(building))
                {
                    Debug.LogError($"[PlayerBuilding] Building {building.BuildingData.BuildingName} has insufficient support at {pos}");
                    return false;
                }
            }

            return true;
        }

        private Vector3 RoundToGrid(Vector3 position, float gridSize)
        {
            return new Vector3(
                Mathf.Round(position.x / gridSize) * gridSize,
                Mathf.Round(position.y * 100f) / 100f,
                Mathf.Round(position.z / gridSize) * gridSize
            );
        }

        private bool CheckBuildingSupport(Building building)
        {
            if (building == null || building.BuildingData == null)
                return false;

            if (building.BuildingData.RequiresFoundation)
            {
                Vector3 checkCenter = building.transform.position + building.BuildingData.PlacementOffset - Vector3.up * 0.2f;
                Vector3 checkExtents = new Vector3(
                    building.BuildingData.Size.x * 0.4f,
                    0.2f,
                    building.BuildingData.Size.z * 0.4f
                );

                Collider[] colliders = Physics.OverlapBox(checkCenter, checkExtents);
                foreach (Collider col in colliders)
                {
                    Building foundBuilding = col.GetComponentInParent<Building>();
                    if (foundBuilding != null && foundBuilding.BuildingData != null)
                    {
                        if (foundBuilding.BuildingData.Category == BuildingCategory.Foundation)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            return true;
        }

        private bool HasRequiredMaterials()
        {
            if (_selectedBuilding == null || _selectedBuilding.BuildCost == null)
                return false;

            if (_inventoryManager == null) return false;

            foreach (var cost in _selectedBuilding.BuildCost)
            {
                if (cost.Item == null) continue;

                if (!_inventoryManager.HasItem(cost.Item, cost.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        private void ConsumeMaterials()
        {
            if (_selectedBuilding == null || _selectedBuilding.BuildCost == null)
                return;

            if (_inventoryManager == null) return;

            foreach (var cost in _selectedBuilding.BuildCost)
            {
                if (cost.Item == null) continue;

                _inventoryManager.RemoveItem(cost.Item, cost.Quantity);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!_placementSettings.ShowDebugGizmos) return;

            if (_selectedBuilding != null && _isInBuildMode)
            {
                Gizmos.color = IsValidPlacement ? Color.green : Color.red;
                Gizmos.DrawWireCube(_currentPosition + _selectedBuilding.PlacementOffset, _selectedBuilding.Size);

                if (_lastValidationResult != null)
                {
                    Gizmos.color = Color.blue;
                    Gizmos.DrawWireSphere(_lastValidationResult.SuggestedPosition, 0.3f);
                }
            }

            if (_lastSamplePoints != null)
            {
                foreach (var point in _lastSamplePoints)
                {
                    Gizmos.color = point.HitGround ? Color.green : Color.red;
                    Gizmos.DrawSphere(point.WorldPosition, 0.1f);

                    if (point.HitGround)
                    {
                        Gizmos.DrawLine(point.WorldPosition, point.HitInfo.point);
                    }
                }
            }
        }
    }
}
