using System;
using System.Collections.Generic;
using UnityEngine;
using SurvivalGame.Inventory;
using SurvivalGame.World.Environment;
using SurvivalGame.Systems.Quests;

namespace SurvivalGame.SaveSystem
{
    [Serializable]
    public struct SerializableKeyValuePair<TKey, TValue>
    {
        public TKey Key;
        public TValue Value;

        public SerializableKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }

    [Serializable]
    public class StringIntDictionary : Dictionary<string, int>
    {
        [SerializeField] private List<SerializableKeyValuePair<string, int>> _items = new List<SerializableKeyValuePair<string, int>>();

        public void OnBeforeSerialize()
        {
            _items.Clear();
            foreach (var kvp in this)
            {
                _items.Add(new SerializableKeyValuePair<string, int>(kvp.Key, kvp.Value));
            }
        }

        public void OnAfterDeserialize()
        {
            this.Clear();
            foreach (var kvp in _items)
            {
                if (kvp.Key != null)
                {
                    this[kvp.Key] = kvp.Value;
                }
            }
        }
    }

    [Serializable]
    public class GameSaveData
    {
        public int SaveVersion = 1;
        public string SaveName;
        public long SaveTimeTicks;
        public int PlayTimeSeconds;

        public PlayerSaveData PlayerData;
        public WorldSaveData WorldData;
        public TimeSaveData TimeData;
        public WeatherSaveData WeatherData;
        public QuestSaveData QuestData;

        public DateTime SaveTime
        {
            get => DateTime.FromBinary(SaveTimeTicks);
            set => SaveTimeTicks = value.ToBinary();
        }
    }

    [Serializable]
    public class PlayerSaveData
    {
        public Vector3Serializable Position;
        public QuaternionSerializable Rotation;

        public float Health;
        public float MaxHealth;
        public float Hunger;
        public float MaxHunger;
        public float Stamina;
        public float MaxStamina;
        public float Temperature;

        public int Currency;
        public int SelectedHotbarSlot;

        public InventorySaveData Inventory;
        public InventorySaveData Hotbar;
        public InventorySaveData Equipment;
    }

    [Serializable]
    public class WorldSaveData
    {
        public List<BuildingSaveData> Buildings = new List<BuildingSaveData>();
        public List<ContainerSaveData> Containers = new List<ContainerSaveData>();
        public List<ResourceNodeSaveData> Resources = new List<ResourceNodeSaveData>();
        public List<EnemySaveData> Enemies = new List<EnemySaveData>();
        public List<ShopSaveData> Shops = new List<ShopSaveData>();
    }

    [Serializable]
    public struct Vector3Serializable
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3Serializable(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3Serializable(Vector3 vector)
        {
            X = vector.x;
            Y = vector.y;
            Z = vector.z;
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }

        public static implicit operator Vector3Serializable(Vector3 vector)
        {
            return new Vector3Serializable(vector);
        }

        public static implicit operator Vector3(Vector3Serializable serializable)
        {
            return serializable.ToVector3();
        }
    }

    [Serializable]
    public struct QuaternionSerializable
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public QuaternionSerializable(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public QuaternionSerializable(Quaternion quaternion)
        {
            X = quaternion.x;
            Y = quaternion.y;
            Z = quaternion.z;
            W = quaternion.w;
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }

        public static implicit operator QuaternionSerializable(Quaternion quaternion)
        {
            return new QuaternionSerializable(quaternion);
        }

        public static implicit operator Quaternion(QuaternionSerializable serializable)
        {
            return serializable.ToQuaternion();
        }
    }

    [Serializable]
    public class SaveFileInfo
    {
        public string FileName;
        public string SaveName;
        public long SaveTimeTicks;
        public int PlayTimeSeconds;
        public string PlayerPosition;
        public int DayCount;

        public DateTime SaveTime
        {
            get => DateTime.FromBinary(SaveTimeTicks);
            set => SaveTimeTicks = value.ToBinary();
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
