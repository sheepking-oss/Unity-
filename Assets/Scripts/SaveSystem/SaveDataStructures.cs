using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Inventory;
using SurvivalGame.World.Environment;
using SurvivalGame.Systems.Quests;

namespace SurvivalGame.SaveSystem
{
    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 1;
        public string SaveName;
        public string SaveTimeString;
        public int PlayTimeSeconds;

        public PlayerSaveData PlayerData;
        public WorldSaveData WorldData;
        public TimeSaveData TimeData;
        public WeatherSaveData WeatherData;
        public QuestSaveData QuestData;

        public DateTime SaveTime
        {
            get
            {
                if (string.IsNullOrEmpty(SaveTimeString))
                    return DateTime.MinValue;
                if (DateTime.TryParse(SaveTimeString, out DateTime time))
                    return time;
                return DateTime.MinValue;
            }
            set
            {
                SaveTimeString = value.ToString("o");
            }
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3 Position;
        public Quaternion Rotation;

        public float Health;
        public float MaxHealth;
        public float Hunger;
        public float MaxHunger;
        public float Stamina;
        public float MaxStamina;
        public float Temperature;

        public int Currency;

        public InventorySaveData Inventory;
        public InventorySaveData Hotbar;
        public InventorySaveData Equipment;

        public int SelectedHotbarSlot;
    }

    [Serializable]
    public class WorldSaveData
    {
        public List<BuildingSaveEntry> Buildings;
        public List<ContainerSaveEntry> Containers;
        public List<ResourceNodeSaveEntry> Resources;
        public List<EnemySaveEntry> Enemies;
        public List<ShopSaveEntry> Shops;

        public WorldSaveData()
        {
            Buildings = new List<BuildingSaveEntry>();
            Containers = new List<ContainerSaveEntry>();
            Resources = new List<ResourceNodeSaveEntry>();
            Enemies = new List<EnemySaveEntry>();
            Shops = new List<ShopSaveEntry>();
        }
    }

    [Serializable]
    public class BuildingSaveEntry
    {
        public string Key;
        public BuildingSaveData Data;

        public BuildingSaveEntry() { }

        public BuildingSaveEntry(string key, BuildingSaveData data)
        {
            Key = key;
            Data = data;
        }
    }

    [Serializable]
    public class ContainerSaveEntry
    {
        public string Key;
        public ContainerSaveData Data;

        public ContainerSaveEntry() { }

        public ContainerSaveEntry(string key, ContainerSaveData data)
        {
            Key = key;
            Data = data;
        }
    }

    [Serializable]
    public class ResourceNodeSaveEntry
    {
        public string Key;
        public ResourceNodeSaveData Data;

        public ResourceNodeSaveEntry() { }

        public ResourceNodeSaveEntry(string key, ResourceNodeSaveData data)
        {
            Key = key;
            Data = data;
        }
    }

    [Serializable]
    public class EnemySaveEntry
    {
        public string Key;
        public EnemySaveData Data;

        public EnemySaveEntry() { }

        public EnemySaveEntry(string key, EnemySaveData data)
        {
            Key = key;
            Data = data;
        }
    }

    [Serializable]
    public class ShopSaveEntry
    {
        public string Key;
        public ShopSaveData Data;

        public ShopSaveEntry() { }

        public ShopSaveEntry(string key, ShopSaveData data)
        {
            Key = key;
            Data = data;
        }
    }

    [Serializable]
    public class SaveFileInfo
    {
        public string FileName;
        public string SaveName;
        public string SaveTimeString;
        public int PlayTimeSeconds;
        public string PlayerPosition;
        public int DayCount;

        public DateTime SaveTime
        {
            get
            {
                if (string.IsNullOrEmpty(SaveTimeString))
                    return DateTime.MinValue;
                if (DateTime.TryParse(SaveTimeString, out DateTime time))
                    return time;
                return DateTime.MinValue;
            }
            set
            {
                SaveTimeString = value.ToString("o");
            }
        }
    }

    [Serializable]
    public class BuildingSaveData
    {
        public string BuildingID;
        public string BuildingDataID;
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale;
        public float CurrentHealth;
    }

    [Serializable]
    public class ContainerSaveData
    {
        public string ContainerID;
        public string ContainerType;
        public Vector3 Position;
        public Quaternion Rotation;
        public InventorySaveData Inventory;
        public bool IsLocked;
    }

    [Serializable]
    public class ResourceNodeSaveData
    {
        public string ResourceID;
        public string ResourceDataID;
        public Vector3 Position;
        public Quaternion Rotation;
        public float CurrentHealth;
        public float RespawnTimer;
        public bool IsDepleted;
    }

    [Serializable]
    public class EnemySaveData
    {
        public string EnemyID;
        public string EnemyDataID;
        public Vector3 Position;
        public Quaternion Rotation;
        public float CurrentHealth;
        public int CurrentState;
    }

    [Serializable]
    public class ShopSaveData
    {
        public string ShopID;
        public string ShopName;
        public Vector3 Position;
        public Quaternion Rotation;
        public int Currency;
    }

    [Serializable]
    public class InventorySaveData
    {
        public string InventoryID;
        public int Size;
        public List<InventorySlotSaveData> Slots = new List<InventorySlotSaveData>();
    }

    [Serializable]
    public class InventorySlotSaveData
    {
        public int SlotIndex;
        public string ItemID;
        public int Quantity;
    }

    [Serializable]
    public class QuestSaveData
    {
        public List<QuestInstanceSaveEntry> ActiveQuests;
        public List<string> CompletedQuestIDs;

        public QuestSaveData()
        {
            ActiveQuests = new List<QuestInstanceSaveEntry>();
            CompletedQuestIDs = new List<string>();
        }
    }

    [Serializable]
    public class QuestInstanceSaveEntry
    {
        public string QuestID;
        public int Status;
        public List<ObjectiveProgressEntry> ObjectiveProgress;
        public float TimeRemaining;
    }

    [Serializable]
    public class ObjectiveProgressEntry
    {
        public string ObjectiveID;
        public int Progress;

        public ObjectiveProgressEntry() { }

        public ObjectiveProgressEntry(string objectiveID, int progress)
        {
            ObjectiveID = objectiveID;
            Progress = progress;
        }
    }

    [Serializable]
    public class QuestInstanceSaveData
    {
        public string QuestID;
        public int Status;
        public Dictionary<string, int> ObjectiveProgress;
        public float TimeRemaining;
    }

    [Serializable]
    public class TimeSaveData
    {
        public float CurrentTime;
        public int DayCount;
        public float TimeScale;
    }

    [Serializable]
    public class WeatherSaveData
    {
        public int CurrentWeather;
        public int TargetWeather;
        public float WeatherTimer;
        public bool IsTransitioning;
        public float TransitionTimer;
    }

    public static class SaveDataExtensions
    {
        public static Dictionary<string, BuildingSaveData> ToBuildingDictionary(this List<BuildingSaveEntry> entries)
        {
            Dictionary<string, BuildingSaveData> dict = new Dictionary<string, BuildingSaveData>();
            if (entries == null) return dict;

            foreach (BuildingSaveEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Data != null)
                {
                    dict[entry.Key] = entry.Data;
                }
            }
            return dict;
        }

        public static List<BuildingSaveEntry> ToBuildingEntryList(this Dictionary<string, BuildingSaveData> dict)
        {
            List<BuildingSaveEntry> list = new List<BuildingSaveEntry>();
            if (dict == null) return list;

            foreach (var kvp in dict)
            {
                list.Add(new BuildingSaveEntry(kvp.Key, kvp.Value));
            }
            return list;
        }

        public static Dictionary<string, ContainerSaveData> ToContainerDictionary(this List<ContainerSaveEntry> entries)
        {
            Dictionary<string, ContainerSaveData> dict = new Dictionary<string, ContainerSaveData>();
            if (entries == null) return dict;

            foreach (ContainerSaveEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Data != null)
                {
                    dict[entry.Key] = entry.Data;
                }
            }
            return dict;
        }

        public static List<ContainerSaveEntry> ToContainerEntryList(this Dictionary<string, ContainerSaveData> dict)
        {
            List<ContainerSaveEntry> list = new List<ContainerSaveEntry>();
            if (dict == null) return list;

            foreach (var kvp in dict)
            {
                list.Add(new ContainerSaveEntry(kvp.Key, kvp.Value));
            }
            return list;
        }

        public static Dictionary<string, ResourceNodeSaveData> ToResourceDictionary(this List<ResourceNodeSaveEntry> entries)
        {
            Dictionary<string, ResourceNodeSaveData> dict = new Dictionary<string, ResourceNodeSaveData>();
            if (entries == null) return dict;

            foreach (ResourceNodeSaveEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Data != null)
                {
                    dict[entry.Key] = entry.Data;
                }
            }
            return dict;
        }

        public static List<ResourceNodeSaveEntry> ToResourceEntryList(this Dictionary<string, ResourceNodeSaveData> dict)
        {
            List<ResourceNodeSaveEntry> list = new List<ResourceNodeSaveEntry>();
            if (dict == null) return list;

            foreach (var kvp in dict)
            {
                list.Add(new ResourceNodeSaveEntry(kvp.Key, kvp.Value));
            }
            return list;
        }

        public static Dictionary<string, EnemySaveData> ToEnemyDictionary(this List<EnemySaveEntry> entries)
        {
            Dictionary<string, EnemySaveData> dict = new Dictionary<string, EnemySaveData>();
            if (entries == null) return dict;

            foreach (EnemySaveEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.Key) && entry.Data != null)
                {
                    dict[entry.Key] = entry.Data;
                }
            }
            return dict;
        }

        public static List<EnemySaveEntry> ToEnemyEntryList(this Dictionary<string, EnemySaveData> dict)
        {
            List<EnemySaveEntry> list = new List<EnemySaveEntry>();
            if (dict == null) return list;

            foreach (var kvp in dict)
            {
                list.Add(new EnemySaveEntry(kvp.Key, kvp.Value));
            }
            return list;
        }

        public static Dictionary<string, int> ToObjectiveDictionary(this List<ObjectiveProgressEntry> entries)
        {
            Dictionary<string, int> dict = new Dictionary<string, int>();
            if (entries == null) return dict;

            foreach (ObjectiveProgressEntry entry in entries)
            {
                if (!string.IsNullOrEmpty(entry.ObjectiveID))
                {
                    dict[entry.ObjectiveID] = entry.Progress;
                }
            }
            return dict;
        }

        public static List<ObjectiveProgressEntry> ToObjectiveEntryList(this Dictionary<string, int> dict)
        {
            List<ObjectiveProgressEntry> list = new List<ObjectiveProgressEntry>();
            if (dict == null) return list;

            foreach (var kvp in dict)
            {
                list.Add(new ObjectiveProgressEntry(kvp.Key, kvp.Value));
            }
            return list;
        }
    }

    public static class SaveFileExtensions
    {
        public static string ToReadableString(this TimeSpan span)
        {
            string formatted = string.Format("{0}{1}{2}{3}",
                span.Days > 0 ? string.Format("{0:0}d ", span.Days) : "",
                span.Hours > 0 ? string.Format("{0:0}h ", span.Hours) : "",
                span.Minutes > 0 ? string.Format("{0:0}m ", span.Minutes) : "",
                span.Seconds > 0 ? string.Format("{0:0}s", span.Seconds) : "");

            return formatted.Trim();
        }
    }
}
