using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Core.Managers;
using SurvivalGame.Data.Quests;
using SurvivalGame.Data.Items;
using SurvivalGame.Data.Enemies;
using SurvivalGame.Core.Events;
using SurvivalGame.Data.Managers;

namespace SurvivalGame.Systems.Quests
{
    public class QuestManager : ManagerBase
    {
        [Header("Quest Settings")]
        [SerializeField] private int _maxActiveQuests = 20;

        [Header("References")]
        [SerializeField] private DataManager _dataManager;
        [SerializeField] private Inventory.InventoryManager _inventoryManager;

        private List<QuestInstance> _activeQuests = new List<QuestInstance>();
        private List<string> _completedQuestIDs = new List<string>();
        private Dictionary<string, QuestInstance> _questsByID = new Dictionary<string, QuestInstance>();

        public static QuestManager Instance => GetInstance<QuestManager>();

        public int ActiveQuestCount => _activeQuests.Count;
        public IReadOnlyList<QuestInstance> ActiveQuests => _activeQuests.AsReadOnly();
        public IReadOnlyList<string> CompletedQuestIDs => _completedQuestIDs.AsReadOnly();

        public override void Initialize()
        {
            base.Initialize();

            if (_dataManager == null)
                _dataManager = DataManager.Instance;
            if (_inventoryManager == null)
                _inventoryManager = Inventory.InventoryManager.Instance;

            RegisterEventListeners();
        }

        private void OnDestroy()
        {
            UnregisterEventListeners();
        }

        private void Update()
        {
            UpdateQuestTimers();
        }

        private void RegisterEventListeners()
        {
            EventManager.AddListener<Items.ItemInstance>(GameEvents.OnResourceGathered, OnResourceGathered);
            EventManager.AddListener<Characters.Enemies.Enemy>(GameEvents.OnEnemyKilled, OnEnemyKilled);
            EventManager.AddListener<Items.ItemInstance>("OnItemCrafted", OnItemCrafted);
            EventManager.AddListener<World.Buildings.Building>(GameEvents.OnBuildingPlaced, OnBuildingPlaced);
        }

        private void UnregisterEventListeners()
        {
            EventManager.RemoveListener<Items.ItemInstance>(GameEvents.OnResourceGathered, OnResourceGathered);
            EventManager.RemoveListener<Characters.Enemies.Enemy>(GameEvents.OnEnemyKilled, OnEnemyKilled);
            EventManager.RemoveListener<Items.ItemInstance>("OnItemCrafted", OnItemCrafted);
            EventManager.RemoveListener<World.Buildings.Building>(GameEvents.OnBuildingPlaced, OnBuildingPlaced);
        }

        private void UpdateQuestTimers()
        {
            foreach (QuestInstance quest in _activeQuests)
            {
                if (quest == null) continue;

                quest.UpdateTime(Time.deltaTime);
            }
        }

        public bool CanAcceptQuest(QuestData questData)
        {
            if (questData == null) return false;

            if (_activeQuests.Count >= _maxActiveQuests)
                return false;

            if (_questsByID.ContainsKey(questData.QuestID))
                return false;

            if (_completedQuestIDs.Contains(questData.QuestID) && !questData.IsRepeatable)
                return false;

            if (questData.PrerequisiteQuests != null)
            {
                foreach (string prerequisiteID in questData.PrerequisiteQuests)
                {
                    if (!_completedQuestIDs.Contains(prerequisiteID))
                        return false;
                }
            }

            return true;
        }

        public bool AcceptQuest(QuestData questData)
        {
            if (!CanAcceptQuest(questData))
                return false;

            QuestInstance questInstance = new QuestInstance(questData);
            questInstance.OnQuestCompleted += OnQuestCompleted;
            questInstance.OnQuestFailed += OnQuestFailed;

            _activeQuests.Add(questInstance);
            _questsByID[questData.QuestID] = questInstance;

            EventManager.TriggerEvent("OnQuestAccepted", questInstance);
            Debug.Log($"Accepted quest: {questData.QuestName}");

            return true;
        }

        public bool AbandonQuest(QuestInstance quest)
        {
            if (quest == null) return false;
            if (!_activeQuests.Contains(quest)) return false;

            _activeQuests.Remove(quest);
            _questsByID.Remove(quest.QuestData.QuestID);

            quest.OnQuestCompleted -= OnQuestCompleted;
            quest.OnQuestFailed -= OnQuestFailed;

            EventManager.TriggerEvent("OnQuestAbandoned", quest);
            Debug.Log($"Abandoned quest: {quest.QuestData.QuestName}");

            return true;
        }

        public bool TurnInQuest(QuestInstance quest)
        {
            if (quest == null) return false;
            if (quest.Status != QuestStatus.Completed) return false;

            GiveQuestRewards(quest);
            quest.TurnIn();

            if (!quest.QuestData.IsRepeatable)
            {
                _completedQuestIDs.Add(quest.QuestData.QuestID);
            }

            _activeQuests.Remove(quest);
            _questsByID.Remove(quest.QuestData.QuestID);

            quest.OnQuestCompleted -= OnQuestCompleted;
            quest.OnQuestFailed -= OnQuestFailed;

            EventManager.TriggerEvent("OnQuestTurnedIn", quest);
            Debug.Log($"Turned in quest: {quest.QuestData.QuestName}");

            return true;
        }

        private void GiveQuestRewards(QuestInstance quest)
        {
            if (quest == null || quest.QuestData == null) return;

            QuestData data = quest.QuestData;

            if (data.ExperienceReward > 0)
            {
                EventManager.TriggerEvent("OnExperienceGained", data.ExperienceReward);
            }

            if (data.CurrencyReward > 0)
            {
                EventManager.TriggerEvent("OnCurrencyGained", data.CurrencyReward);
            }

            if (data.ItemRewards != null && _inventoryManager != null)
            {
                foreach (QuestRewardItem reward in data.ItemRewards)
                {
                    if (reward.Item != null && reward.Quantity > 0)
                    {
                        _inventoryManager.AddItem(reward.Item, reward.Quantity);
                    }
                }
            }
        }

        private void OnQuestCompleted(QuestInstance quest)
        {
            EventManager.TriggerEvent(GameEvents.OnQuestCompleted, quest);
            Debug.Log($"Quest completed: {quest.QuestData.QuestName}");
        }

        private void OnQuestFailed(QuestInstance quest)
        {
            EventManager.TriggerEvent("OnQuestFailed", quest);
            Debug.Log($"Quest failed: {quest.QuestData.QuestName}");
        }

        #region Event Handlers

        private void OnResourceGathered(Items.ItemInstance item)
        {
            if (item == null || item.ItemData == null) return;

            UpdateObjectivesOfType(ObjectiveType.Gather, item.ItemData.ItemID, item.CurrentStackSize);
        }

        private void OnEnemyKilled(Characters.Enemies.Enemy enemy)
        {
            if (enemy == null || enemy.EnemyData == null) return;

            UpdateObjectivesOfType(ObjectiveType.Kill, enemy.EnemyData.EnemyID, 1);
        }

        private void OnItemCrafted(Items.ItemInstance item)
        {
            if (item == null || item.ItemData == null) return;

            UpdateObjectivesOfType(ObjectiveType.Craft, item.ItemData.ItemID, item.CurrentStackSize);
        }

        private void OnBuildingPlaced(World.Buildings.Building building)
        {
            if (building == null || building.BuildingData == null) return;

            UpdateObjectivesOfType(ObjectiveType.Build, building.BuildingData.BuildingID, 1);
        }

        #endregion

        private void UpdateObjectivesOfType(ObjectiveType type, string targetID, int amount)
        {
            foreach (QuestInstance quest in _activeQuests)
            {
                if (quest == null || quest.Status != QuestStatus.Active) continue;
                if (quest.QuestData == null || quest.QuestData.Objectives == null) continue;

                foreach (QuestObjective objective in quest.QuestData.Objectives)
                {
                    if (objective == null) continue;
                    if (objective.Type != type) continue;
                    if (objective.TargetID != targetID) continue;

                    quest.UpdateObjective(objective.ObjectiveID, amount);
                }
            }
        }

        public QuestInstance GetQuestByID(string questID)
        {
            if (_questsByID.TryGetValue(questID, out QuestInstance quest))
            {
                return quest;
            }
            return null;
        }

        public List<QuestInstance> GetQuestsByType(QuestType type)
        {
            List<QuestInstance> result = new List<QuestInstance>();

            foreach (QuestInstance quest in _activeQuests)
            {
                if (quest != null && quest.QuestData != null && quest.QuestData.QuestType == type)
                {
                    result.Add(quest);
                }
            }

            return result;
        }

        public bool HasCompletedQuest(string questID)
        {
            return _completedQuestIDs.Contains(questID);
        }

        public QuestSaveData GetSaveData()
        {
            QuestSaveData saveData = new QuestSaveData();

            foreach (QuestInstance quest in _activeQuests)
            {
                if (quest == null) continue;

                saveData.ActiveQuests.Add(quest.GetSaveEntry());
            }

            saveData.CompletedQuestIDs.AddRange(_completedQuestIDs);

            Debug.Log($"[QuestManager] Saved {saveData.ActiveQuests.Count} active quests, {saveData.CompletedQuestIDs.Count} completed quests");

            return saveData;
        }

        public void LoadFromSaveData(QuestSaveData saveData)
        {
            if (saveData == null)
            {
                Debug.LogWarning("[QuestManager] QuestSaveData is null");
                return;
            }

            foreach (QuestInstance quest in new List<QuestInstance>(_activeQuests))
            {
                AbandonQuest(quest);
            }

            _completedQuestIDs.Clear();
            if (saveData.CompletedQuestIDs != null)
            {
                _completedQuestIDs.AddRange(saveData.CompletedQuestIDs);
                Debug.Log($"[QuestManager] Loaded {_completedQuestIDs.Count} completed quest IDs");
            }

            if (_dataManager == null)
            {
                Debug.LogError("[QuestManager] DataManager is null, cannot load quests");
                return;
            }

            if (saveData.ActiveQuests == null)
            {
                Debug.LogWarning("[QuestManager] ActiveQuests list is null");
                return;
            }

            int loadedCount = 0;
            foreach (QuestInstanceSaveEntry questSave in saveData.ActiveQuests)
            {
                if (questSave == null || string.IsNullOrEmpty(questSave.QuestID))
                {
                    Debug.LogWarning("[QuestManager] Invalid quest save entry");
                    continue;
                }

                QuestData questData = _dataManager.GetQuest(questSave.QuestID);
                if (questData == null)
                {
                    Debug.LogWarning($"[QuestManager] QuestData not found for ID: {questSave.QuestID}");
                    continue;
                }

                QuestInstance questInstance = new QuestInstance(questData);
                questInstance.OnQuestCompleted += OnQuestCompleted;
                questInstance.OnQuestFailed += OnQuestFailed;

                if (questSave.ObjectiveProgress != null)
                {
                    Dictionary<string, int> progressDict = questSave.ObjectiveProgress.ToObjectiveDictionary();
                    foreach (var kvp in progressDict)
                    {
                        questInstance.SetObjectiveProgress(kvp.Key, kvp.Value);
                    }
                }

                _activeQuests.Add(questInstance);
                _questsByID[questData.QuestID] = questInstance;
                loadedCount++;
            }

            Debug.Log($"[QuestManager] Loaded {loadedCount} active quests");
        }
    }
}
