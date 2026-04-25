using UnityEngine;
using System.Collections.Generic;

namespace SurvivalGame.Data.Quests
{
    [CreateAssetMenu(fileName = "NewQuest", menuName = "SurvivalGame/Quests/Quest Data")]
    public class QuestData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _questName = "New Quest";
        [SerializeField] private string _questID = "quest_0001";
        [TextArea(3, 5)]
        [SerializeField] private string _description = "Quest description";
        [TextArea(2, 3)]
        [SerializeField] private string _completionMessage = "Quest completed!";

        [Header("Quest Type")]
        [SerializeField] private QuestType _questType = QuestType.Gather;
        [SerializeField] private QuestDifficulty _difficulty = QuestDifficulty.Easy;

        [Header("Objectives")]
        [SerializeField] private List<QuestObjective> _objectives = new List<QuestObjective>();

        [Header("Requirements")]
        [SerializeField] private int _minLevel = 1;
        [SerializeField] private string[] _prerequisiteQuests;
        [SerializeField] private bool _isRepeatable = false;

        [Header("Rewards")]
        [SerializeField] private int _experienceReward = 50;
        [SerializeField] private int _currencyReward = 0;
        [SerializeField] private QuestRewardItem[] _itemRewards;

        [Header("Time Limit")]
        [SerializeField] private bool _hasTimeLimit = false;
        [SerializeField] private float _timeLimitSeconds = 300f;

        #region Properties

        public string QuestName => _questName;
        public string QuestID => _questID;
        public string Description => _description;
        public string CompletionMessage => _completionMessage;

        public QuestType QuestType => _questType;
        public QuestDifficulty Difficulty => _difficulty;

        public List<QuestObjective> Objectives => _objectives;

        public int MinLevel => _minLevel;
        public string[] PrerequisiteQuests => _prerequisiteQuests;
        public bool IsRepeatable => _isRepeatable;

        public int ExperienceReward => _experienceReward;
        public int CurrencyReward => _currencyReward;
        public QuestRewardItem[] ItemRewards => _itemRewards;

        public bool HasTimeLimit => _hasTimeLimit;
        public float TimeLimitSeconds => _timeLimitSeconds;

        #endregion

        public bool AreAllObjectivesComplete(Dictionary<string, int> objectiveProgress)
        {
            foreach (QuestObjective objective in _objectives)
            {
                if (!objectiveProgress.ContainsKey(objective.ObjectiveID))
                    return false;

                if (objectiveProgress[objective.ObjectiveID] < objective.RequiredAmount)
                    return false;
            }
            return true;
        }
    }

    [System.Serializable]
    public class QuestObjective
    {
        [SerializeField] private string _objectiveID = "obj_0001";
        [SerializeField] private string _description = "Objective description";
        [SerializeField] private ObjectiveType _type = ObjectiveType.Gather;
        [SerializeField] private string _targetID;
        [SerializeField] private int _requiredAmount = 1;
        [SerializeField] private bool _isOptional = false;

        public string ObjectiveID => _objectiveID;
        public string Description => _description;
        public ObjectiveType Type => _type;
        public string TargetID => _targetID;
        public int RequiredAmount => _requiredAmount;
        public bool IsOptional => _isOptional;
    }

    [System.Serializable]
    public class QuestRewardItem
    {
        public Items.ItemData Item;
        public int Quantity = 1;
    }

    public enum QuestType
    {
        Gather,
        Kill,
        Craft,
        Deliver,
        Explore,
        Interact,
        Escort,
        Survival
    }

    public enum ObjectiveType
    {
        Gather,
        Kill,
        Craft,
        Deliver,
        ReachLocation,
        Interact,
        UseItem,
        EquipItem,
        Build,
        SurviveTime
    }

    public enum QuestDifficulty
    {
        Trivial,
        Easy,
        Normal,
        Hard,
        Elite,
        Epic
    }

    public enum QuestStatus
    {
        NotAvailable,
        Available,
        Active,
        Completed,
        TurnedIn,
        Failed
    }
}
