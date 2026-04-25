using System;
using System.Collections.Generic;
using SurvivalGame.Data.Quests;
using SurvivalGame.Data.Items;
using SurvivalGame.Core.Events;

namespace SurvivalGame.Systems.Quests
{
    [Serializable]
    public class QuestInstance
    {
        [SerializeField] private QuestData _questData;
        [SerializeField] private QuestStatus _status;
        [SerializeField] private Dictionary<string, int> _objectiveProgress = new Dictionary<string, int>();
        [SerializeField] private float _timeRemaining;

        private DateTime _acceptTime;

        public QuestData QuestData => _questData;
        public QuestStatus Status => _status;
        public IReadOnlyDictionary<string, int> ObjectiveProgress => _objectiveProgress;
        public float TimeRemaining => _timeRemaining;

        public event Action<QuestInstance> OnQuestUpdated;
        public event Action<QuestInstance> OnQuestCompleted;
        public event Action<QuestInstance> OnQuestFailed;

        public QuestInstance(QuestData data)
        {
            _questData = data;
            _status = QuestStatus.Active;
            _objectiveProgress = new Dictionary<string, int>();
            _acceptTime = DateTime.Now;

            InitializeObjectives();

            if (data.HasTimeLimit)
            {
                _timeRemaining = data.TimeLimitSeconds;
            }
        }

        private void InitializeObjectives()
        {
            if (_questData == null || _questData.Objectives == null)
                return;

            foreach (QuestObjective objective in _questData.Objectives)
            {
                if (objective == null) continue;

                _objectiveProgress[objective.ObjectiveID] = 0;
            }
        }

        public void UpdateObjective(string objectiveID, int amount = 1)
        {
            if (_status != QuestStatus.Active) return;
            if (!_objectiveProgress.ContainsKey(objectiveID)) return;

            _objectiveProgress[objectiveID] += amount;

            QuestObjective objective = GetObjectiveByID(objectiveID);
            if (objective != null)
            {
                _objectiveProgress[objectiveID] = Mathf.Min(_objectiveProgress[objectiveID], objective.RequiredAmount);
            }

            OnQuestUpdated?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnQuestUpdated, this);

            CheckCompletion();
        }

        public void SetObjectiveProgress(string objectiveID, int amount)
        {
            if (!_objectiveProgress.ContainsKey(objectiveID)) return;

            _objectiveProgress[objectiveID] = amount;

            QuestObjective objective = GetObjectiveByID(objectiveID);
            if (objective != null)
            {
                _objectiveProgress[objectiveID] = Mathf.Min(_objectiveProgress[objectiveID], objective.RequiredAmount);
            }

            OnQuestUpdated?.Invoke(this);
            CheckCompletion();
        }

        public int GetObjectiveProgress(string objectiveID)
        {
            if (_objectiveProgress.TryGetValue(objectiveID, out int progress))
            {
                return progress;
            }
            return 0;
        }

        public float GetObjectiveProgressPercent(string objectiveID)
        {
            QuestObjective objective = GetObjectiveByID(objectiveID);
            if (objective == null) return 0f;

            int progress = GetObjectiveProgress(objectiveID);
            return (float)progress / objective.RequiredAmount;
        }

        public QuestObjective GetObjectiveByID(string objectiveID)
        {
            if (_questData == null || _questData.Objectives == null)
                return null;

            foreach (QuestObjective objective in _questData.Objectives)
            {
                if (objective.ObjectiveID == objectiveID)
                    return objective;
            }

            return null;
        }

        public bool IsObjectiveComplete(string objectiveID)
        {
            QuestObjective objective = GetObjectiveByID(objectiveID);
            if (objective == null) return false;

            return GetObjectiveProgress(objectiveID) >= objective.RequiredAmount;
        }

        public void CheckCompletion()
        {
            if (_questData == null || _questData.Objectives == null)
                return;

            bool allComplete = true;

            foreach (QuestObjective objective in _questData.Objectives)
            {
                if (objective == null || objective.IsOptional) continue;

                if (!IsObjectiveComplete(objective.ObjectiveID))
                {
                    allComplete = false;
                    break;
                }
            }

            if (allComplete)
            {
                Complete();
            }
        }

        public void Complete()
        {
            if (_status != QuestStatus.Active) return;

            _status = QuestStatus.Completed;
            OnQuestCompleted?.Invoke(this);
            EventManager.TriggerEvent(GameEvents.OnQuestCompleted, this);
        }

        public void Fail()
        {
            if (_status != QuestStatus.Active) return;

            _status = QuestStatus.Failed;
            OnQuestFailed?.Invoke(this);
            EventManager.TriggerEvent("OnQuestFailed", this);
        }

        public void TurnIn()
        {
            if (_status != QuestStatus.Completed) return;

            _status = QuestStatus.TurnedIn;
            EventManager.TriggerEvent("OnQuestTurnedIn", this);
        }

        public void UpdateTime(float deltaTime)
        {
            if (_status != QuestStatus.Active) return;
            if (!_questData.HasTimeLimit) return;

            _timeRemaining -= deltaTime;

            if (_timeRemaining <= 0f)
            {
                _timeRemaining = 0f;
                Fail();
            }
        }

        public QuestInstanceSaveData GetSaveData()
        {
            return new QuestInstanceSaveData
            {
                QuestID = _questData?.QuestID,
                Status = (int)_status,
                ObjectiveProgress = new Dictionary<string, int>(_objectiveProgress),
                TimeRemaining = _timeRemaining
            };
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
}
