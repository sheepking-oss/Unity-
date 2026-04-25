using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Inventory;
using SurvivalGame.Core.Events;
using SurvivalGame.Core.Managers;

namespace SurvivalGame.World.Containers
{
    public class ContainerManager : ManagerBase
    {
        [Header("Container Settings")]
        [SerializeField] private int _maxOpenContainers = 2;

        private List<Container> _registeredContainers = new List<Container>();
        private Dictionary<string, Container> _containersByID = new Dictionary<string, Container>();
        private List<Container> _openContainers = new List<Container>();

        public static ContainerManager Instance => GetInstance<ContainerManager>();

        public int OpenContainerCount => _openContainers.Count;
        public bool HasOpenContainers => _openContainers.Count > 0;
        public IReadOnlyList<Container> OpenContainers => _openContainers.AsReadOnly();

        public override void Initialize()
        {
            base.Initialize();

            RefreshContainerList();

            EventManager.AddListener<Container>("OnContainerOpened", OnContainerOpened);
            EventManager.AddListener<Container>("OnContainerClosed", OnContainerClosed);
        }

        private void OnDestroy()
        {
            EventManager.RemoveListener<Container>("OnContainerOpened", OnContainerOpened);
            EventManager.RemoveListener<Container>("OnContainerClosed", OnContainerClosed);
        }

        public void RefreshContainerList()
        {
            _registeredContainers.Clear();
            _containersByID.Clear();

            Container[] allContainers = FindObjectsOfType<Container>();
            foreach (Container container in allContainers)
            {
                RegisterContainer(container);
            }

            Debug.Log($"[ContainerManager] Registered {_registeredContainers.Count} containers.");
        }

        public void RegisterContainer(Container container)
        {
            if (container == null) return;

            if (!_registeredContainers.Contains(container))
            {
                _registeredContainers.Add(container);
            }

            if (!string.IsNullOrEmpty(container.ContainerID) && !_containersByID.ContainsKey(container.ContainerID))
            {
                _containersByID[container.ContainerID] = container;
            }
        }

        public void UnregisterContainer(Container container)
        {
            if (container == null) return;

            _registeredContainers.Remove(container);

            if (!string.IsNullOrEmpty(container.ContainerID))
            {
                _containersByID.Remove(container.ContainerID);
            }

            if (_openContainers.Contains(container))
            {
                _openContainers.Remove(container);
            }
        }

        public Container GetContainerByID(string containerID)
        {
            if (_containersByID.TryGetValue(containerID, out Container container))
            {
                return container;
            }
            return null;
        }

        public List<Container> GetContainersInRange(Vector3 position, float range)
        {
            List<Container> result = new List<Container>();

            foreach (Container container in _registeredContainers)
            {
                if (container == null) continue;

                float distance = Vector3.Distance(position, container.transform.position);
                if (distance <= range)
                {
                    result.Add(container);
                }
            }

            return result;
        }

        private void OnContainerOpened(Container container)
        {
            if (container == null) return;

            if (!_openContainers.Contains(container))
            {
                if (_openContainers.Count >= _maxOpenContainers)
                {
                    Container oldest = _openContainers[0];
                    oldest.Close();
                }

                _openContainers.Add(container);
            }

            EventManager.TriggerEvent("OnOpenContainerUI", container);
        }

        private void OnContainerClosed(Container container)
        {
            if (container == null) return;

            _openContainers.Remove(container);
            EventManager.TriggerEvent("OnCloseContainerUI", container);
        }

        public void CloseAllContainers()
        {
            foreach (Container container in new List<Container>(_openContainers))
            {
                container.Close();
            }
            _openContainers.Clear();
        }

        public List<ContainerSaveData> GetAllContainerSaveData()
        {
            List<ContainerSaveData> saveData = new List<ContainerSaveData>();

            foreach (Container container in _registeredContainers)
            {
                if (container == null) continue;
                if (string.IsNullOrEmpty(container.ContainerID)) continue;

                saveData.Add(container.GetSaveData());
            }

            Debug.Log($"[ContainerManager] Saved {saveData.Count} containers.");
            return saveData;
        }

        public void LoadContainerSaveData(List<ContainerSaveData> saveData)
        {
            if (saveData == null) return;

            int loadedCount = 0;
            foreach (ContainerSaveData containerSave in saveData)
            {
                if (containerSave == null) continue;
                if (string.IsNullOrEmpty(containerSave.ContainerID)) continue;

                Container container = GetContainerByID(containerSave.ContainerID);
                if (container != null)
                {
                    container.LoadFromSaveData(containerSave);
                    loadedCount++;
                }
            }

            Debug.Log($"[ContainerManager] Loaded save data for {loadedCount}/{saveData.Count} containers.");
        }
    }
}
