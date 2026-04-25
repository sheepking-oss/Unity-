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
        public DateTime SaveTime;
        public int PlayTimeSeconds;

        public PlayerSaveData PlayerData;
        public WorldSaveData WorldData;
        public TimeSaveData TimeData;
        public WeatherSaveData WeatherData;
        public QuestSaveData QuestData;
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
        public Dictionary<string, BuildingSaveData> Buildings;
        public Dictionary<string, ContainerSaveData> Containers;
        public Dictionary<string, ResourceNodeSaveData> Resources;
        public Dictionary<string, EnemySaveData> Enemies;
        public Dictionary<string, ShopSaveData> Shops;
    }

    [Serializable]
    public class SaveFileInfo
    {
        public string FileName;
        public string SaveName;
        public DateTime SaveTime;
        public int PlayTimeSeconds;
        public string PlayerPosition;
        public int DayCount;
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
