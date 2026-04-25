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

            Debug.Log($"[BuildingManager] Saved {saveData.Count} buildings");
            return saveData;
        }

        public List<BuildingSaveEntry> GetAllBuildingSaveEntries()
        {
            List<BuildingSaveEntry> saveData = new List<BuildingSaveEntry>();

            foreach (Building building in _registeredBuildings)
            {
                if (building == null) continue;
                if (string.IsNullOrEmpty(building.BuildingID)) continue;

                saveData.Add(new BuildingSaveEntry(building.BuildingID, building.GetSaveData()));
            }

            Debug.Log($"[BuildingManager] Saved {saveData.Count} buildings as entries");
            return saveData;
        }

        public void LoadBuildingSaveData(Dictionary<string, BuildingSaveData> saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[BuildingManager] Building save data is null");
                return;
            }

            DestroyAllBuildings();

            DataManager dataManager = DataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("[BuildingManager] DataManager is null, cannot load buildings");
                return;
            }

            int loadedCount = 0;
            foreach (var kvp in saveData)
            {
                BuildingSaveData buildingSave = kvp.Value;
                if (buildingSave == null) continue;

                BuildingData buildingData = dataManager.GetBuilding(buildingSave.BuildingDataID);
                if (buildingData == null)
                {
                    Debug.LogWarning($"[BuildingManager] BuildingData not found for ID: {buildingSave.BuildingDataID}");
                    continue;
                }

                Building building = PlaceBuildingWithID(buildingData, buildingSave.Position, buildingSave.Rotation, buildingSave.BuildingID);
                if (building != null)
                {
                    building.LoadFromSaveData(buildingSave);
                    loadedCount++;
                }
            }

            Debug.Log($"[BuildingManager] Loaded {loadedCount} buildings from save (total in data: {saveData.Count})");
        }

        public void LoadBuildingSaveEntries(List<BuildingSaveEntry> saveEntries)
        {
            if (saveEntries == null)
            {
                Debug.LogWarning("[BuildingManager] Building save entries is null");
                return;
            }

            DestroyAllBuildings();

            DataManager dataManager = DataManager.Instance;
            if (dataManager == null)
            {
                Debug.LogError("[BuildingManager] DataManager is null, cannot load buildings");
                return;
            }

            int loadedCount = 0;
            foreach (BuildingSaveEntry entry in saveEntries)
            {
                if (entry == null || entry.Data == null) continue;

                BuildingSaveData buildingSave = entry.Data;

                BuildingData buildingData = dataManager.GetBuilding(buildingSave.BuildingDataID);
                if (buildingData == null)
                {
                    Debug.LogWarning($"[BuildingManager] BuildingData not found for ID: {buildingSave.BuildingDataID}");
                    continue;
                }

                string buildingID = !string.IsNullOrEmpty(entry.Key) ? entry.Key : buildingSave.BuildingID;
                Building building = PlaceBuildingWithID(buildingData, buildingSave.Position, buildingSave.Rotation, buildingID);
                if (building != null)
                {
                    building.LoadFromSaveData(buildingSave);
                    loadedCount++;
                }
            }

            Debug.Log($"[BuildingManager] Loaded {loadedCount} buildings from entries (total entries: {saveEntries.Count})");
        }

        public Building PlaceBuildingWithID(BuildingData buildingData, Vector3 position, Quaternion rotation, string buildingID)
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
                building.InitializeWithID(buildingData, buildingID);
                RegisterBuilding(building);
                EventManager.TriggerEvent(GameEvents.OnBuildingPlaced, building);
            }

            return building;
        }
    }
}
