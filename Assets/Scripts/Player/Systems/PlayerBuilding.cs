using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Buildings;
using SurvivalGame.Player.Controllers;
using SurvivalGame.Core.Input;
using SurvivalGame.Core.Managers;
using SurvivalGame.Inventory;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Player.Systems
{
    public class PlayerBuilding : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private PlayerController _playerController;
        [SerializeField] private ThirdPersonCamera _camera;
        [SerializeField] private InventoryManager _inventoryManager;

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

        private InputManager _inputManager;
        private GameStateManager _gameStateManager;

        private BuildingData _selectedBuilding;
        private GameObject _previewObject;
        private Renderer[] _previewRenderers;
        private bool _isInBuildMode = false;
        private float _currentRotation = 0f;
        private Vector3 _currentPosition;
        private bool _isValidPlacement = false;

        private List<Collider> _previewColliders = new List<Collider>();

        public bool IsInBuildMode => _isInBuildMode;
        public BuildingData SelectedBuilding => _selectedBuilding;
        public bool IsValidPlacement => _isValidPlacement;

        private void Awake()
        {
            if (_playerController == null)
                _playerController = GetComponent<PlayerController>();
            if (_inventoryManager == null)
                _inventoryManager = InventoryManager.Instance;

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
            CheckPlacementValidity();
            UpdatePreviewVisuals();
            CheckPlacementInput();
            CheckExitInput();
        }

        private void UpdatePreviewPosition()
        {
            if (_camera == null) return;

            Camera mainCamera = Camera.main;
            if (mainCamera == null) return;

            Ray ray = mainCamera.ScreenPointToRay(new Vector3(Screen.width / 2, Screen.height / 2, 0));

            if (Physics.Raycast(ray, out RaycastHit hit, _maxBuildDistance, _groundLayers))
            {
                _currentPosition = hit.point;

                if (_selectedBuilding.SnapToGrid)
                {
                    float gridSize = _selectedBuilding.GridSize;
                    _currentPosition.x = Mathf.Round(_currentPosition.x / gridSize) * gridSize;
                    _currentPosition.z = Mathf.Round(_currentPosition.z / gridSize) * gridSize;
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

        private void UpdatePreviewRotation()
        {
            if (_inputManager != null && _inputManager.RotateBuildingPressed)
            {
                if (_selectedBuilding != null && _selectedBuilding.CanBeRotated)
                {
                    _currentRotation += _selectedBuilding.RotationStep;
                    if (_currentRotation >= 360f)
                        _currentRotation -= 360f;
                }
            }

            if (_previewObject != null)
            {
                _previewObject.transform.rotation = Quaternion.Euler(0f, _currentRotation, 0f);
            }
        }

        private void CheckPlacementValidity()
        {
            if (_selectedBuilding == null)
            {
                _isValidPlacement = false;
                return;
            }

            if (_previewColliders.Count == 0)
            {
                _isValidPlacement = false;
                return;
            }

            _isValidPlacement = true;

            if (_selectedBuilding.RequiresFoundation)
            {
                if (!CheckOnFoundation())
                {
                    _isValidPlacement = false;
                }
            }

            Vector3 center = _currentPosition;
            Vector3 halfExtents = _selectedBuilding.Size * 0.5f;
            Quaternion rotation = Quaternion.Euler(0f, _currentRotation, 0f);

            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, _obstacleLayers);

            foreach (Collider hit in hitColliders)
            {
                if (_previewColliders.Contains(hit))
                    continue;

                if (hit.transform == transform)
                    continue;

                _isValidPlacement = false;
                break;
            }
        }

        private bool CheckOnFoundation()
        {
            Vector3 center = _currentPosition - Vector3.up * 0.1f;
            Vector3 halfExtents = new Vector3(_selectedBuilding.Size.x * 0.4f, 0.1f, _selectedBuilding.Size.z * 0.4f);

            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents);

            foreach (Collider hit in hitColliders)
            {
                Buildings.Building building = hit.GetComponent<Buildings.Building>();
                if (building != null && building.BuildingData != null)
                {
                    if (building.BuildingData.Category == BuildingCategory.Foundation)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void UpdatePreviewVisuals()
        {
            if (_previewRenderers == null || _previewRenderers.Length == 0)
                return;

            Material material = _isValidPlacement ? _validPlacementMaterial : _invalidPlacementMaterial;

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
                if (_isValidPlacement)
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
            _gameStateManager?.ChangeState(GameState.Building);

            CreatePreviewObject();

            EventManager.TriggerEvent("OnEnterBuildMode", _selectedBuilding);
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

            if (_isInBuildMode)
            {
                DestroyPreviewObject();
                CreatePreviewObject();
            }

            EventManager.TriggerEvent("OnBuildingSelected", _selectedBuilding);
        }

        private void CreatePreviewObject()
        {
            if (_selectedBuilding == null || _selectedBuilding.Prefab == null)
                return;

            DestroyPreviewObject();

            _previewObject = Instantiate(_selectedBuilding.Prefab, _currentPosition, Quaternion.Euler(0f, _currentRotation, 0f));
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

            ConsumeMaterials();

            GameObject buildingObject = Instantiate(
                _selectedBuilding.Prefab,
                _currentPosition,
                Quaternion.Euler(0f, _currentRotation, 0f)
            );

            Buildings.Building building = buildingObject.GetComponent<Buildings.Building>();
            if (building != null)
            {
                building.Initialize(_selectedBuilding);
            }

            EventManager.TriggerEvent(GameEvents.OnBuildingPlaced, building);

            Debug.Log($"Placed building: {_selectedBuilding.BuildingName}");

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
    }
}
