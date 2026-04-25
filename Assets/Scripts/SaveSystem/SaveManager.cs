using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using SurvivalGame.Core.Managers;
using SurvivalGame.Core.Events;
using SurvivalGame.Inventory;
using SurvivalGame.World.Environment;
using SurvivalGame.World.Managers;
using SurvivalGame.Systems.Quests;
using SurvivalGame.Characters.Enemies;
using SurvivalGame.World.Containers;
using SurvivalGame.World.Resources;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.SaveSystem
{
    public class SaveManager : ManagerBase
    {
        [Header("Save Settings")]
        [SerializeField] private string _saveFolderName = "Saves";
        [SerializeField] private string _saveFileExtension = ".json";
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private int _autoSaveIntervalMinutes = 5;

        [Header("References")]
        [SerializeField] private Transform _player;
        [SerializeField] private Player.Controllers.PlayerController _playerController;
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private TimeManager _timeManager;
        [SerializeField] private WeatherManager _weatherManager;
        [SerializeField] private QuestManager _questManager;
        [SerializeField] private BuildingManager _buildingManager;
        [SerializeField] private ContainerManager _containerManager;
        [SerializeField] private ResourceManager _resourceManager;
        [SerializeField] private EnemyManager _enemyManager;

        private float _autoSaveTimer;
        private string _saveFolderPath;
        private int _playTimeSeconds;
        private DateTime _sessionStartTime;

        public static SaveManager Instance => GetInstance<SaveManager>();

        public string SaveFolderPath => _saveFolderPath;
        public int PlayTimeSeconds => _playTimeSeconds;
        public bool IsAutoSaveEnabled => _enableAutoSave;

        public override void Initialize()
        {
            base.Initialize();

            _saveFolderPath = Path.Combine(Application.persistentDataPath, _saveFolderName);
            if (!Directory.Exists(_saveFolderPath))
            {
                Directory.CreateDirectory(_saveFolderPath);
            }

            _sessionStartTime = DateTime.Now;
            _autoSaveTimer = _autoSaveIntervalMinutes * 60f;

            Debug.Log($"[SaveManager] Save folder: {_saveFolderPath}");
        }

        private void Update()
        {
            if (!IsInitialized) return;

            _playTimeSeconds = (int)(DateTime.Now - _sessionStartTime).TotalSeconds;

            if (_enableAutoSave)
            {
                _autoSaveTimer -= Time.deltaTime;
                if (_autoSaveTimer <= 0f)
                {
                    AutoSave();
                    _autoSaveTimer = _autoSaveIntervalMinutes * 60f;
                }
            }
        }

        #region Save Operations

        public bool SaveGame(string saveName)
        {
            try
            {
                GameSaveData saveData = CollectSaveData();
                saveData.SaveName = saveName;
                saveData.SaveTime = DateTime.Now;

                string json = JsonUtility.ToJson(saveData, true);
                string fileName = $"{saveName}_{DateTime.Now:yyyyMMdd_HHmmss}{_saveFileExtension}";
                string filePath = Path.Combine(_saveFolderPath, fileName);

                File.WriteAllText(filePath, json);

                EventManager.TriggerEvent(GameEvents.OnSaveGame);
                Debug.Log($"[SaveManager] Game saved: {filePath}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}");
                return false;
            }
        }

        public bool AutoSave()
        {
            return SaveGame("AutoSave");
        }

        public bool QuickSave()
        {
            return SaveGame("QuickSave");
        }

        #endregion

        #region Load Operations

        public bool LoadGame(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                {
                    Debug.LogError($"[SaveManager] Save file not found: {filePath}");
                    return false;
                }

                string json = File.ReadAllText(filePath);
                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                ApplySaveData(saveData);

                EventManager.TriggerEvent(GameEvents.OnLoadGame);
                Debug.Log($"[SaveManager] Game loaded: {filePath}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}");
                return false;
            }
        }

        public bool LoadMostRecentSave()
        {
            List<SaveFileInfo> saves = GetAllSaves();
            if (saves.Count == 0)
                return false;

            saves.Sort((a, b) => b.SaveTime.CompareTo(a.SaveTime));
            string filePath = Path.Combine(_saveFolderPath, saves[0].FileName);

            return LoadGame(filePath);
        }

        #endregion

        #region Data Collection

        private GameSaveData CollectSaveData()
        {
            GameSaveData saveData = new GameSaveData
            {
                SaveVersion = 1,
                PlayTimeSeconds = _playTimeSeconds,

                PlayerData = CollectPlayerData(),
                WorldData = CollectWorldData(),
                TimeData = _timeManager?.GetSaveData(),
                WeatherData = _weatherManager?.GetSaveData(),
                QuestData = _questManager?.GetSaveData()
            };

            return saveData;
        }

        private PlayerSaveData CollectPlayerData()
        {
            PlayerSaveData playerData = new PlayerSaveData();

            if (_player != null)
            {
                playerData.Position = _player.position;
                playerData.Rotation = _player.rotation;
            }

            if (_playerController != null)
            {
                playerData.Health = _playerController.Health;
                playerData.MaxHealth = _playerController.MaxHealth;
                playerData.Hunger = _playerController.Hunger;
                playerData.MaxHunger = _playerController.MaxHunger;
                playerData.Stamina = _playerController.Stamina;
                playerData.MaxStamina = _playerController.MaxStamina;
            }

            if (_inventoryManager != null)
            {
                playerData.Inventory = _inventoryManager.GetPlayerInventorySaveData();
                playerData.Hotbar = _inventoryManager.GetHotbarSaveData();
                playerData.Equipment = _inventoryManager.GetEquipmentSaveData();
            }

            return playerData;
        }

        private WorldSaveData CollectWorldData()
        {
            WorldSaveData worldData = new WorldSaveData();

            if (_buildingManager != null)
            {
                worldData.Buildings = _buildingManager.GetAllBuildingSaveData();
            }

            if (_containerManager != null)
            {
                worldData.Containers = _containerManager.GetAllContainerSaveData();
            }

            if (_resourceManager != null)
            {
                worldData.Resources = _resourceManager.GetAllResourceSaveData();
            }

            if (_enemyManager != null)
            {
                worldData.Enemies = _enemyManager.GetAllEnemySaveData();
            }

            return worldData;
        }

        #endregion

        #region Data Application

        private void ApplySaveData(GameSaveData saveData)
        {
            if (saveData == null) return;

            _playTimeSeconds = saveData.PlayTimeSeconds;

            ApplyPlayerData(saveData.PlayerData);
            ApplyWorldData(saveData.WorldData);

            if (saveData.TimeData != null && _timeManager != null)
            {
                _timeManager.LoadFromSaveData(saveData.TimeData);
            }

            if (saveData.WeatherData != null && _weatherManager != null)
            {
                _weatherManager.LoadFromSaveData(saveData.WeatherData);
            }

            if (saveData.QuestData != null && _questManager != null)
            {
                _questManager.LoadFromSaveData(saveData.QuestData);
            }
        }

        private void ApplyPlayerData(PlayerSaveData playerData)
        {
            if (playerData == null) return;

            if (_player != null)
            {
                _player.position = playerData.Position;
                _player.rotation = playerData.Rotation;
            }

            if (_playerController != null)
            {
                _playerController.SetMaxHealth(playerData.MaxHealth);
                _playerController.SetMaxHunger(playerData.MaxHunger);
                _playerController.SetMaxStamina(playerData.MaxStamina);
            }

            if (_inventoryManager != null)
            {
                if (playerData.Inventory != null)
                {
                    _inventoryManager.LoadPlayerInventory(playerData.Inventory);
                }
                if (playerData.Hotbar != null)
                {
                    _inventoryManager.LoadHotbar(playerData.Hotbar);
                }
                if (playerData.Equipment != null)
                {
                    _inventoryManager.LoadEquipment(playerData.Equipment);
                }
            }
        }

        private void ApplyWorldData(WorldSaveData worldData)
        {
            if (worldData == null) return;

            if (worldData.Buildings != null && _buildingManager != null)
            {
                _buildingManager.LoadBuildingSaveData(worldData.Buildings);
            }

            if (worldData.Containers != null && _containerManager != null)
            {
                _containerManager.LoadContainerSaveData(worldData.Containers);
            }

            if (worldData.Resources != null && _resourceManager != null)
            {
                _resourceManager.LoadResourceSaveData(worldData.Resources);
            }

            if (worldData.Enemies != null && _enemyManager != null)
            {
                _enemyManager.LoadEnemySaveData(worldData.Enemies);
            }
        }

        #endregion

        #region Save File Management

        public List<SaveFileInfo> GetAllSaves()
        {
            List<SaveFileInfo> saves = new List<SaveFileInfo>();

            if (!Directory.Exists(_saveFolderPath))
                return saves;

            string[] files = Directory.GetFiles(_saveFolderPath, $"*{_saveFileExtension}");

            foreach (string file in files)
            {
                try
                {
                    string json = File.ReadAllText(file);
                    GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

                    if (saveData != null)
                    {
                        SaveFileInfo info = new SaveFileInfo
                        {
                            FileName = Path.GetFileName(file),
                            SaveName = saveData.SaveName,
                            SaveTime = saveData.SaveTime,
                            PlayTimeSeconds = saveData.PlayTimeSeconds,
                            PlayerPosition = saveData.PlayerData?.Position.ToString() ?? "N/A",
                            DayCount = saveData.TimeData?.DayCount ?? 1
                        };

                        saves.Add(info);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[SaveManager] Failed to read save file {file}: {e.Message}");
                }
            }

            return saves;
        }

        public bool DeleteSave(string fileName)
        {
            try
            {
                string filePath = Path.Combine(_saveFolderPath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    Debug.Log($"[SaveManager] Deleted save: {fileName}");
                    return true;
                }
                return false;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Failed to delete save: {e.Message}");
                return false;
            }
        }

        public void SetAutoSaveEnabled(bool enabled)
        {
            _enableAutoSave = enabled;
        }

        public void SetAutoSaveInterval(int minutes)
        {
            _autoSaveIntervalMinutes = Mathf.Max(1, minutes);
            _autoSaveTimer = _autoSaveIntervalMinutes * 60f;
        }

        #endregion
    }
}
