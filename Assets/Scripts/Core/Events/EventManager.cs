using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalGame.Core.Events
{
    public class EventManager : ManagerBase
    {
        private readonly Dictionary<string, Delegate> _eventDictionary = new Dictionary<string, Delegate>();

        public static void AddListener(string eventName, Action listener)
        {
            if (Instance == null) return;

            if (!Instance._eventDictionary.ContainsKey(eventName))
            {
                Instance._eventDictionary.Add(eventName, null);
            }
            Instance._eventDictionary[eventName] = (Action)Instance._eventDictionary[eventName] + listener;
        }

        public static void AddListener<T>(string eventName, Action<T> listener)
        {
            if (Instance == null) return;

            if (!Instance._eventDictionary.ContainsKey(eventName))
            {
                Instance._eventDictionary.Add(eventName, null);
            }
            Instance._eventDictionary[eventName] = (Action<T>)Instance._eventDictionary[eventName] + listener;
        }

        public static void RemoveListener(string eventName, Action listener)
        {
            if (Instance == null) return;

            if (Instance._eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
            {
                Instance._eventDictionary[eventName] = (Action)thisEvent - listener;
            }
        }

        public static void RemoveListener<T>(string eventName, Action<T> listener)
        {
            if (Instance == null) return;

            if (Instance._eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
            {
                Instance._eventDictionary[eventName] = (Action<T>)thisEvent - listener;
            }
        }

        public static void TriggerEvent(string eventName)
        {
            if (Instance == null) return;

            if (Instance._eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
            {
                if (thisEvent is Action action)
                {
                    action.Invoke();
                }
            }
        }

        public static void TriggerEvent<T>(string eventName, T arg)
        {
            if (Instance == null) return;

            if (Instance._eventDictionary.TryGetValue(eventName, out Delegate thisEvent))
            {
                if (thisEvent is Action<T> action)
                {
                    action.Invoke(arg);
                }
            }
        }

        public static void ClearAllEvents()
        {
            if (Instance == null) return;
            Instance._eventDictionary.Clear();
        }
    }

    public static class GameEvents
    {
        public const string OnPlayerHealthChanged = "OnPlayerHealthChanged";
        public const string OnPlayerHungerChanged = "OnPlayerHungerChanged";
        public const string OnPlayerStaminaChanged = "OnPlayerStaminaChanged";
        public const string OnInventoryChanged = "OnInventoryChanged";
        public const string OnItemEquipped = "OnItemEquipped";
        public const string OnItemUnequipped = "OnItemUnequipped";
        public const string OnResourceGathered = "OnResourceGathered";
        public const string OnBuildingPlaced = "OnBuildingPlaced";
        public const string OnBuildingDestroyed = "OnBuildingDestroyed";
        public const string OnEnemyKilled = "OnEnemyKilled";
        public const string OnQuestUpdated = "OnQuestUpdated";
        public const string OnQuestCompleted = "OnQuestCompleted";
        public const string OnTimeOfDayChanged = "OnTimeOfDayChanged";
        public const string OnWeatherChanged = "OnWeatherChanged";
        public const string OnGamePaused = "OnGamePaused";
        public const string OnGameResumed = "OnGameResumed";
        public const string OnSaveGame = "OnSaveGame";
        public const string OnLoadGame = "OnLoadGame";
    }
}
