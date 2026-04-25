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
        [SerializeField] private bool _enableDebugLogging = true;

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

            Debug.Log($"[SaveManager] Initialized. Save folder: {_saveFolderPath}");
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
                Log($"Starting save: {saveName}");

                GameSaveData saveData = CollectSaveData();
                saveData.SaveName = saveName;
                saveData.SaveTime = DateTime.Now;

                string json = JsonUtility.ToJson(saveData, true);
                Log($"Serialized JSON length: {json.Length} characters");

                string fileName = $"{saveName}_{DateTime.Now:yyyyMMdd_HHmmss}{_saveFileExtension}";
                string filePath = Path.Combine(_saveFolderPath, fileName);

                File.WriteAllText(filePath, json);

                EventManager.TriggerEvent(GameEvents.OnSaveGame);
                Debug.Log($"[SaveManager] Game saved successfully: {filePath}");

                LogSaveSummary(saveData);

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Save failed: {e.Message}\n{e.StackTrace}");
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

                Log($"Starting load: {filePath}");

                string json = File.ReadAllText(filePath);
                Log($"Loaded JSON length: {json.Length} characters");

                GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
                if (saveData == null)
                {
                    Debug.LogError("[SaveManager] Failed to deserialize save data");
                    return false;
                }

                LogLoadSummary(saveData);

                ApplySaveData(saveData);

                EventManager.TriggerEvent(GameEvents.OnLoadGame);
                Debug.Log($"[SaveManager] Game loaded successfully: {filePath}");

                return true;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Load failed: {e.Message}\n{e.StackTrace}");
                return false;
            }
        }

        public bool LoadMostRecentSave()
        {
            List<SaveFileInfo> saves = GetAllSaves();
            if (saves.Count == 0)
            {
                Log("No saves found to load");
                return false;
            }

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
                Log($"Player position: {_player.position}, rotation: {_player.rotation.eulerAngles}");
            }

            if (_playerController != null)
            {
                playerData.Health = _playerController.Health;
                playerData.MaxHealth = _playerController.MaxHealth;
                playerData.Hunger = _playerController.Hunger;
                playerData.MaxHunger = _playerController.MaxHunger;
                playerData.Stamina = _playerController.Stamina;
                playerData.MaxStamina = _playerController.MaxStamina;
                playerData.Temperature = _playerController.Temperature;
                Log($"Player stats - Health: {playerData.Health}/{playerData.MaxHealth}, Hunger: {playerData.Hunger}/{playerData.MaxHunger}, Stamina: {playerData.Stamina}/{playerData.MaxStamina}");
            }

            if (_inventoryManager != null)
            {
                playerData.Inventory = _inventoryManager.GetPlayerInventorySaveData();
                playerData.Hotbar = _inventoryManager.GetHotbarSaveData();
                playerData.Equipment = _inventoryManager.GetEquipmentSaveData();
                playerData.SelectedHotbarSlot = _inventoryManager.SelectedHotbarSlot;

                int invSlots = playerData.Inventory?.Slots?.Count ?? 0;
                int hotbarSlots = playerData.Hotbar?.Slots?.Count ?? 0;
                Log($"Inventory: {invSlots} slots with items, Hotbar: {hotbarSlots} slots with items, Selected slot: {playerData.SelectedHotbarSlot}");
            }

            return playerData;
        }

        private WorldSaveData CollectWorldData()
        {
            WorldSaveData worldData = new WorldSaveData();

            if (_buildingManager != null)
            {
                worldData.Buildings = _buildingManager.GetAllBuildingSaveEntries();
                Log($"Collected {worldData.Buildings.Count} buildings");
            }

            if (_containerManager != null)
            {
                var containers = _containerManager.GetAllContainerSaveData();
                worldData.Containers = containers.ToContainerEntryList();
                Log($"Collected {worldData.Containers.Count} containers");
            }

            if (_resourceManager != null)
            {
                var resources = _resourceManager.GetAllResourceSaveData();
                worldData.Resources = resources.ToResourceEntryList();
                Log($"Collected {worldData.Resources.Count} resource nodes");
            }

            if (_enemyManager != null)
            {
                var enemies = _enemyManager.GetAllEnemySaveData();
                worldData.Enemies = enemies.ToEnemyEntryList();
                Log($"Collected {worldData.Enemies.Count} enemies");
            }

            return worldData;
        }

        #endregion

        #region Data Application

        private void ApplySaveData(GameSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[SaveManager] ApplySaveData: saveData is null");
                return;
            }

            _playTimeSeconds = saveData.PlayTimeSeconds;
            Log($"Applying save data - Play time: {saveData.PlayTimeSeconds}s, Save time: {saveData.SaveTimeString}");

            ApplyPlayerData(saveData.PlayerData);
            ApplyWorldData(saveData.WorldData);

            if (saveData.TimeData != null && _timeManager != null)
            {
                _timeManager.LoadFromSaveData(saveData.TimeData);
                Log($"Applied time data - Day: {saveData.TimeData.DayCount}, Time: {saveData.TimeData.CurrentTime}");
            }
            else
            {
                LogWarning("TimeData is null or TimeManager is missing");
            }

            if (saveData.WeatherData != null && _weatherManager != null)
            {
                _weatherManager.LoadFromSaveData(saveData.WeatherData);
                Log($"Applied weather data - Current: {(WeatherType)saveData.WeatherData.CurrentWeather}");
            }
            else
            {
                LogWarning("WeatherData is null or WeatherManager is missing");
            }

            if (saveData.QuestData != null && _questManager != null)
            {
                _questManager.LoadFromSaveData(saveData.QuestData);
            }
            else
            {
                LogWarning("QuestData is null or QuestManager is missing");
            }

            Log("Save data application complete");
        }

        private void ApplyPlayerData(PlayerSaveData playerData)
        {
            if (playerData == null)
            {
                LogWarning("PlayerSaveData is null");
                return;
            }

            if (_player != null)
            {
                _player.position = playerData.Position;
                _player.rotation = playerData.Rotation;
                Log($"Applied player position: {playerData.Position}, rotation: {playerData.Rotation.eulerAngles}");
            }
            else
            {
                LogWarning("Player transform is null, cannot apply position/rotation");
            }

            if (_playerController != null)
            {
                _playerController.SetMaxHealth(playerData.MaxHealth);
                _playerController.SetMaxHunger(playerData.MaxHunger);
                _playerController.SetMaxStamina(playerData.MaxStamina);

                _playerController.SetHealthDirect(playerData.Health);
                _playerController.SetHungerDirect(playerData.Hunger);
                _playerController.SetStaminaDirect(playerData.Stamina);
                _playerController.SetTemperatureDirect(playerData.Temperature);

                Log($"Applied player stats - Health: {playerData.Health}/{playerData.MaxHealth}, Hunger: {playerData.Hunger}/{playerData.MaxHunger}, Stamina: {playerData.Stamina}/{playerData.MaxStamina}, Temp: {playerData.Temperature}");
            }
            else
            {
                LogWarning("PlayerController is null, cannot apply stats");
            }

            if (_inventoryManager != null)
            {
                if (playerData.Inventory != null)
                {
                    _inventoryManager.LoadPlayerInventory(playerData.Inventory);
                    Log($"Loaded player inventory: {playerData.Inventory.Slots?.Count ?? 0} slots");
                }
                else
                {
                    LogWarning("Player Inventory save data is null");
                }

                if (playerData.Hotbar != null)
                {
                    _inventoryManager.LoadHotbar(playerData.Hotbar);
                    Log($"Loaded hotbar: {playerData.Hotbar.Slots?.Count ?? 0} slots");
                }
                else
                {
                    LogWarning("Hotbar save data is null");
                }

                if (playerData.Equipment != null)
                {
                    _inventoryManager.LoadEquipment(playerData.Equipment);
                    Log($"Loaded equipment: {playerData.Equipment.Slots?.Count ?? 0} slots");
                }
                else
                {
                    LogWarning("Equipment save data is null");
                }
            }
            else
            {
                LogWarning("InventoryManager is null, cannot apply inventory data");
            }
        }

        private void ApplyWorldData(WorldSaveData worldData)
        {
            if (worldData == null)
            {
                LogWarning("WorldSaveData is null");
                return;
            }

            if (worldData.Buildings != null && _buildingManager != null)
            {
                _buildingManager.LoadBuildingSaveEntries(worldData.Buildings);
            }
            else
            {
                LogWarning("Buildings data is null or BuildingManager is missing");
            }

            if (worldData.Containers != null && _containerManager != null)
            {
                _containerManager.LoadContainerSaveData(worldData.Containers.ToContainerDictionary());
            }
            else
            {
                LogWarning("Containers data is null or ContainerManager is missing");
            }

            if (worldData.Resources != null && _resourceManager != null)
            {
                _resourceManager.LoadResourceSaveData(worldData.Resources.ToResourceDictionary());
            }
            else
            {
                LogWarning("Resources data is null or ResourceManager is missing");
            }

            if (worldData.Enemies != null && _enemyManager != null)
            {
                _enemyManager.LoadEnemySaveData(worldData.Enemies.ToEnemyDictionary());
            }
            else
            {
                LogWarning("Enemies data is null or EnemyManager is missing");
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
                            SaveTimeString = saveData.SaveTimeString,
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
                    Log($"Deleted save: {fileName}");
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

        #region Debug Logging

        private void Log(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[SaveManager] {message}");
            }
        }

        private void LogWarning(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.LogWarning($"[SaveManager] {message}");
            }
        }

        private void LogSaveSummary(GameSaveData saveData)
        {
            Log($"=== SAVE SUMMARY ===");
            Log($"Save version: {saveData.SaveVersion}");
            Log($"Save time: {saveData.SaveTimeString}");
            Log($"Play time: {saveData.PlayTimeSeconds}s");

            if (saveData.PlayerData != null)
            {
                Log($"Player position: {saveData.PlayerData.Position}");
                Log($"Player health: {saveData.PlayerData.Health}/{saveData.PlayerData.MaxHealth}");
                Log($"Player hunger: {saveData.PlayerData.Hunger}/{saveData.PlayerData.MaxHunger}");
                Log($"Player stamina: {saveData.PlayerData.Stamina}/{saveData.PlayerData.MaxStamina}");
            }

            if (saveData.WorldData != null)
            {
                Log($"Buildings: {saveData.WorldData.Buildings?.Count ?? 0}");
                Log($"Containers: {saveData.WorldData.Containers?.Count ?? 0}");
                Log($"Resources: {saveData.WorldData.Resources?.Count ?? 0}");
                Log($"Enemies: {saveData.WorldData.Enemies?.Count ?? 0}");
            }

            if (saveData.TimeData != null)
            {
                Log($"Day: {saveData.TimeData.DayCount}, Time: {saveData.TimeData.CurrentTime}");
            }

            if (saveData.WeatherData != null)
            {
                Log($"Weather: {(WeatherType)saveData.WeatherData.CurrentWeather}");
            }

            if (saveData.QuestData != null)
            {
                Log($"Active quests: {saveData.QuestData.ActiveQuests?.Count ?? 0}");
                Log($"Completed quests: {saveData.QuestData.CompletedQuestIDs?.Count ?? 0}");
            }

            Log($"=== END SAVE SUMMARY ===");
        }

        private void LogLoadSummary(GameSaveData saveData)
        {
            Log($"=== LOAD SUMMARY ===");
            Log($"Save version: {saveData.SaveVersion}");
            Log($"Save time string: {saveData.SaveTimeString}");
            Log($"Parsed save time: {saveData.SaveTime}");
            Log($"Play time: {saveData.PlayTimeSeconds}s");

            if (saveData.PlayerData != null)
            {
                Log($"Player position: {saveData.PlayerData.Position}");
                Log($"Player health: {saveData.PlayerData.Health}/{saveData.PlayerData.MaxHealth}");
                Log($"Player hunger: {saveData.PlayerData.Hunger}/{saveData.PlayerData.MaxHunger}");
                Log($"Player stamina: {saveData.PlayerData.Stamina}/{saveData.PlayerData.MaxStamina}");
                Log($"Inventory slots with items: {saveData.PlayerData.Inventory?.Slots?.Count ?? 0}");
                Log($"Hotbar slots with items: {saveData.PlayerData.Hotbar?.Slots?.Count ?? 0}");
            }

            if (saveData.WorldData != null)
            {
                Log($"Buildings entries: {saveData.WorldData.Buildings?.Count ?? 0}");
                Log($"Containers entries: {saveData.WorldData.Containers?.Count ?? 0}");
                Log($"Resources entries: {saveData.WorldData.Resources?.Count ?? 0}");
                Log($"Enemies entries: {saveData.WorldData.Enemies?.Count ?? 0}");
            }

            if (saveData.QuestData != null)
            {
                Log($"Active quest entries: {saveData.QuestData.ActiveQuests?.Count ?? 0}");
                Log($"Completed quest IDs: {saveData.QuestData.CompletedQuestIDs?.Count ?? 0}");
            }

            Log($"=== END LOAD SUMMARY ===");
        }

        #endregion
    }
}
