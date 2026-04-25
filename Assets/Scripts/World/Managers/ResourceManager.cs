using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.World.Resources;
using SurvivalGame.Core.Interfaces;

namespace SurvivalGame.World.Managers
{
    public class ResourceManager : Core.Managers.ManagerBase
    {
        [Header("Resource Settings")]
        [SerializeField] private int _maxActiveResources = 100;
        [SerializeField] private float _resourceSpawnCheckInterval = 30f;

        private List<ResourceNode> _resourceNodes = new List<ResourceNode>();
        private Dictionary<string, ResourceNode> _resourceNodesByID = new Dictionary<string, ResourceNode>();

        private float _lastSpawnCheckTime;

        public static ResourceManager Instance => GetInstance<ResourceManager>();
        public int ActiveResourceCount => _resourceNodes.Count;

        public override void Initialize()
        {
            base.Initialize();
            RefreshResourceList();
            _lastSpawnCheckTime = Time.time;
        }

        private void Update()
        {
            if (!IsInitialized) return;

            if (Time.time - _lastSpawnCheckTime >= _resourceSpawnCheckInterval)
            {
                CheckResourceRespawns();
                _lastSpawnCheckTime = Time.time;
            }
        }

        public void RefreshResourceList()
        {
            _resourceNodes.Clear();
            _resourceNodesByID.Clear();

            ResourceNode[] allNodes = FindObjectsOfType<ResourceNode>();
            foreach (ResourceNode node in allNodes)
            {
                RegisterResourceNode(node);
            }

            Debug.Log($"[ResourceManager] Registered {_resourceNodes.Count} resource nodes.");
        }

        public void RegisterResourceNode(ResourceNode node)
        {
            if (node == null) return;

            if (!_resourceNodes.Contains(node))
            {
                _resourceNodes.Add(node);
            }

            if (!string.IsNullOrEmpty(node.SaveID) && !_resourceNodesByID.ContainsKey(node.SaveID))
            {
                _resourceNodesByID[node.SaveID] = node;
            }
        }

        public void UnregisterResourceNode(ResourceNode node)
        {
            if (node == null) return;

            _resourceNodes.Remove(node);

            if (!string.IsNullOrEmpty(node.SaveID))
            {
                _resourceNodesByID.Remove(node.SaveID);
            }
        }

        public ResourceNode GetResourceNodeByID(string saveID)
        {
            if (_resourceNodesByID.TryGetValue(saveID, out ResourceNode node))
            {
                return node;
            }
            return null;
        }

        public List<ResourceNode> GetAllResourceNodes()
        {
            return new List<ResourceNode>(_resourceNodes);
        }

        public List<ResourceNode> GetResourceNodesInRange(Vector3 position, float range)
        {
            List<ResourceNode> result = new List<ResourceNode>();

            foreach (ResourceNode node in _resourceNodes)
            {
                if (node == null) continue;

                float distance = Vector3.Distance(position, node.transform.position);
                if (distance <= range)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        public List<ResourceNode> GetHarvestedResourceNodes()
        {
            List<ResourceNode> result = new List<ResourceNode>();

            foreach (ResourceNode node in _resourceNodes)
            {
                if (node != null && node.IsHarvested)
                {
                    result.Add(node);
                }
            }

            return result;
        }

        private void CheckResourceRespawns()
        {
            foreach (ResourceNode node in _resourceNodes)
            {
                if (node == null) continue;

                if (node.IsHarvested && node.NodeData != null && node.NodeData.IsRenewable)
                {
                }
            }
        }

        public void ForceRespawnAll()
        {
            foreach (ResourceNode node in _resourceNodes)
            {
                if (node != null && node.IsHarvested)
                {
                }
            }

            Debug.Log("[ResourceManager] Force respawned all renewable resources.");
        }

        public List<ResourceNodeSaveData> GetAllResourceSaveData()
        {
            List<ResourceNodeSaveData> saveData = new List<ResourceNodeSaveData>();

            foreach (ResourceNode node in _resourceNodes)
            {
                if (node == null) continue;
                if (string.IsNullOrEmpty(node.SaveID)) continue;

                saveData.Add(node.GetSaveData());
            }

            Debug.Log($"[ResourceManager] Saved {saveData.Count} resources.");
            return saveData;
        }

        public void LoadResourceSaveData(List<ResourceNodeSaveData> saveData)
        {
            if (saveData == null) return;

            int loadedCount = 0;
            foreach (ResourceNodeSaveData resourceSave in saveData)
            {
                if (resourceSave == null) continue;
                if (string.IsNullOrEmpty(resourceSave.SaveID)) continue;

                ResourceNode node = GetResourceNodeByID(resourceSave.SaveID);
                if (node != null)
                {
                    node.LoadFromSaveData(resourceSave);
                    loadedCount++;
                }
            }

            Debug.Log($"[ResourceManager] Loaded save data for {loadedCount}/{saveData.Count} resources.");
        }
    }
}
