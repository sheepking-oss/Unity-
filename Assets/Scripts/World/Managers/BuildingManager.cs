using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.World.Buildings;
using SurvivalGame.Data.Buildings;
using SurvivalGame.Data.Managers;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;

namespace SurvivalGame.World.Managers
{
    public class BuildingManager : ManagerBase
    {
        [Header("Building Settings")]
        [SerializeField] private int _maxBuildings = 1000;

        private List<Building> _registeredBuildings = new List<Building>();
        private Dictionary<string, Building> _buildingsByID = new Dictionary<string, Building>();

        public static BuildingManager Instance => GetInstance<BuildingManager>();

        public int BuildingCount => _registeredBuildings.Count;
        public IReadOnlyList<Building> Buildings => _registeredBuildings.AsReadOnly();

        public override void Initialize()
        {
            base.Initialize();

            RefreshBuildingList();

            EventManager.AddListener<Building>(GameEvents.OnBuildingPlaced, OnBuildingPlaced);
            EventManager.AddListener<Building>(GameEvents.OnBuildingDestroyed, OnBuildingDestroyed);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<Building>(GameEvents.OnBuildingPlaced, OnBuildingPlaced);
            EventManager.RemoveListener<Building>(GameEvents.OnBuildingDestroyed, OnBuildingDestroyed);
        }

        private void OnBuildingPlaced(Building building)
        {
            RegisterBuilding(building);
        }

        private void OnBuildingDestroyed(Building building)
        {
            UnregisterBuilding(building);
        }

        public void RefreshBuildingList()
        {
            _registeredBuildings.Clear();
            _buildingsByID.Clear();

            Building[] allBuildings = FindObjectsOfType<Building>();
            foreach (Building building in allBuildings)
            {
                RegisterBuilding(building);
            }

            Debug.Log($"[BuildingManager] Registered {_registeredBuildings.Count} buildings.");
        }

        public void RegisterBuilding(Building building)
        {
            if (building == null) return;

            if (!_registeredBuildings.Contains(building))
            {
                _registeredBuildings.Add(building);
            }

            if (!string.IsNullOrEmpty(building.BuildingID) && !_buildingsByID.ContainsKey(building.BuildingID))
            {
                _buildingsByID[building.BuildingID] = building;
            }

            building.OnBuildingDestroyed += OnBuildingDestroyed;
        }

        public void UnregisterBuilding(Building building)
        {
            if (building == null) return;

            _registeredBuildings.Remove(building);

            if (!string.IsNullOrEmpty(building.BuildingID))
            {
                _buildingsByID.Remove(building.BuildingID);
            }

            building.OnBuildingDestroyed -= OnBuildingDestroyed;
        }

        public Building GetBuildingByID(string buildingID)
        {
            if (_buildingsByID.TryGetValue(buildingID, out Building building))
            {
                return building;
            }
            return null;
        }

        public List<Building> GetBuildingsInRange(Vector3 position, float range)
        {
            List<Building> result = new List<Building>();

            foreach (Building building in _registeredBuildings)
            {
                if (building == null) continue;

                float distance = Vector3.Distance(position, building.transform.position);
                if (distance <= range)
                {
                    result.Add(building);
                }
            }

            return result;
        }

        public List<Building> GetBuildingsByCategory(BuildingCategory category)
        {
            List<Building> result = new List<Building>();

            foreach (Building building in _registeredBuildings)
            {
                if (building == null || building.BuildingData == null) continue;

                if (building.BuildingData.Category == category)
                {
                    result.Add(building);
                }
            }

            return result;
        }

        public bool CanPlaceBuilding(BuildingData buildingData, Vector3 position, Quaternion rotation, LayerMask obstacleLayers)
        {
            if (buildingData == null) return false;

            Vector3 center = position + buildingData.PlacementOffset;
            Vector3 halfExtents = buildingData.Size * 0.5f;

            Collider[] hitColliders = Physics.OverlapBox(center, halfExtents, rotation, obstacleLayers);

            foreach (Collider hit in hitColliders)
            {
                if (hit.isTrigger) continue;

                Building building = hit.GetComponent<Building>();
                if (building != null)
                {
                    if (building.BuildingData != null && building.BuildingData.Category == BuildingCategory.Foundation)
                    {
                        continue;
                    }
                }

                return false;
            }

            return true;
        }

        public Building PlaceBuilding(BuildingData buildingData, Vector3 position, Quaternion rotation)
        {
            if (buildingData == null || buildingData.Prefab == null)
                return null;

            if (_registeredBuildings.Count >= _maxBuildings)
            {
                Debug.LogWarning("[BuildingManager] Maximum building count reached!");
                return null;
            }

            GameObject buildingObject = Instantiate(buildingData.Prefab, position, rotation);
            Building building = buildingObject.GetComponent<Building>();

            if (building != null)
            {
                building.Initialize(buildingData);
                RegisterBuilding(building);
                EventManager.TriggerEvent(GameEvents.OnBuildingPlaced, building);
            }

            return building;
        }

        public bool DestroyBuilding(Building building)
        {
            if (building == null) return false;

            building.TakeDamage(building.MaxHealth + 1f);
            return true;
        }

        public void DestroyAllBuildings()
        {
            foreach (Building building in new List<Building>(_registeredBuildings))
            {
                if (building != null)
                {
                    Destroy(building.gameObject);
                }
            }

            _registeredBuildings.Clear();
            _buildingsByID.Clear();

            Debug.Log("[BuildingManager] Destroyed all buildings.");
        }

        public Dictionary<string, BuildingSaveData> GetAllBuildingSaveData()
        {
            Dictionary<string, BuildingSaveData> saveData = new Dictionary<string, BuildingSaveData>();

            foreach (Building building in _registeredBuildings)
            {
                if (building == null) continue;
                if (string.IsNullOrEmpty(building.BuildingID)) continue;

                saveData[building.BuildingID] = building.GetSaveData();
            }

            return saveData;
        }

        public void LoadBuildingSaveData(Dictionary<string, BuildingSaveData> saveData)
        {
            if (saveData == null) return;

            DestroyAllBuildings();

            DataManager dataManager = DataManager.Instance;
            if (dataManager == null) return;

            foreach (var kvp in saveData)
            {
                BuildingSaveData buildingSave = kvp.Value;

                BuildingData buildingData = dataManager.GetBuilding(buildingSave.BuildingDataID);
                if (buildingData == null) continue;

                Building building = PlaceBuilding(buildingData, buildingSave.Position, buildingSave.Rotation);
                if (building != null)
                {
                    building.LoadFromSaveData(buildingSave);
                }
            }

            Debug.Log($"[BuildingManager] Loaded {saveData.Count} buildings from save.");
        }
    }
}
