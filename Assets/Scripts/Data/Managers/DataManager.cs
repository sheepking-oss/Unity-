using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Items;
using SurvivalGame.Data.Buildings;
using SurvivalGame.Data.Crafting;
using SurvivalGame.Data.Enemies;
using SurvivalGame.Data.Quests;
using SurvivalGame.Data.Resources;

namespace SurvivalGame.Data.Managers
{
    public class DataManager : Core.Managers.ManagerBase
    {
        [Header("Item Data")]
        [SerializeField] private List<ItemData> _items = new List<ItemData>();
        private Dictionary<string, ItemData> _itemDictionary = new Dictionary<string, ItemData>();

        [Header("Building Data")]
        [SerializeField] private List<BuildingData> _buildings = new List<BuildingData>();
        private Dictionary<string, BuildingData> _buildingDictionary = new Dictionary<string, BuildingData>();

        [Header("Recipe Data")]
        [SerializeField] private List<RecipeData> _recipes = new List<RecipeData>();
        private Dictionary<string, RecipeData> _recipeDictionary = new Dictionary<string, RecipeData>();
        private Dictionary<CraftingStationType, List<RecipeData>> _recipesByStation = new Dictionary<CraftingStationType, List<RecipeData>>();

        [Header("Enemy Data")]
        [SerializeField] private List<EnemyData> _enemies = new List<EnemyData>();
        private Dictionary<string, EnemyData> _enemyDictionary = new Dictionary<string, EnemyData>();

        [Header("Quest Data")]
        [SerializeField] private List<QuestData> _quests = new List<QuestData>();
        private Dictionary<string, QuestData> _questDictionary = new Dictionary<string, QuestData>();

        [Header("Resource Node Data")]
        [SerializeField] private List<ResourceNodeData> _resourceNodes = new List<ResourceNodeData>();
        private Dictionary<string, ResourceNodeData> _resourceNodeDictionary = new Dictionary<string, ResourceNodeData>();

        public static DataManager Instance => GetInstance<DataManager>();

        public override void Initialize()
        {
            base.Initialize();
            BuildDictionaries();
            OrganizeRecipesByStation();
        }

        private void BuildDictionaries()
        {
            _itemDictionary.Clear();
            foreach (ItemData item in _items)
            {
                if (item != null && !_itemDictionary.ContainsKey(item.ItemID))
                {
                    _itemDictionary.Add(item.ItemID, item);
                }
            }

            _buildingDictionary.Clear();
            foreach (BuildingData building in _buildings)
            {
                if (building != null && !_buildingDictionary.ContainsKey(building.BuildingID))
                {
                    _buildingDictionary.Add(building.BuildingID, building);
                }
            }

            _recipeDictionary.Clear();
            foreach (RecipeData recipe in _recipes)
            {
                if (recipe != null && !_recipeDictionary.ContainsKey(recipe.RecipeID))
                {
                    _recipeDictionary.Add(recipe.RecipeID, recipe);
                }
            }

            _enemyDictionary.Clear();
            foreach (EnemyData enemy in _enemies)
            {
                if (enemy != null && !_enemyDictionary.ContainsKey(enemy.EnemyID))
                {
                    _enemyDictionary.Add(enemy.EnemyID, enemy);
                }
            }

            _questDictionary.Clear();
            foreach (QuestData quest in _quests)
            {
                if (quest != null && !_questDictionary.ContainsKey(quest.QuestID))
                {
                    _questDictionary.Add(quest.QuestID, quest);
                }
            }

            _resourceNodeDictionary.Clear();
            foreach (ResourceNodeData node in _resourceNodes)
            {
                if (node != null && !_resourceNodeDictionary.ContainsKey(node.NodeID))
                {
                    _resourceNodeDictionary.Add(node.NodeID, node);
                }
            }
        }

        private void OrganizeRecipesByStation()
        {
            _recipesByStation.Clear();

            foreach (RecipeData recipe in _recipes)
            {
                if (recipe == null) continue;

                if (!_recipesByStation.ContainsKey(recipe.RequiredStation))
                {
                    _recipesByStation[recipe.RequiredStation] = new List<RecipeData>();
                }
                _recipesByStation[recipe.RequiredStation].Add(recipe);
            }
        }

        #region Item Access

        public ItemData GetItem(string itemID)
        {
            _itemDictionary.TryGetValue(itemID, out ItemData item);
            return item;
        }

        public T GetItem<T>(string itemID) where T : ItemData
        {
            if (_itemDictionary.TryGetValue(itemID, out ItemData item))
            {
                return item as T;
            }
            return null;
        }

        public List<ItemData> GetAllItems()
        {
            return new List<ItemData>(_itemDictionary.Values);
        }

        public List<ItemData> GetItemsByType(ItemType type)
        {
            List<ItemData> result = new List<ItemData>();
            foreach (ItemData item in _itemDictionary.Values)
            {
                if (item.ItemType == type)
                {
                    result.Add(item);
                }
            }
            return result;
        }

        #endregion

        #region Building Access

        public BuildingData GetBuilding(string buildingID)
        {
            _buildingDictionary.TryGetValue(buildingID, out BuildingData building);
            return building;
        }

        public List<BuildingData> GetAllBuildings()
        {
            return new List<BuildingData>(_buildingDictionary.Values);
        }

        public List<BuildingData> GetBuildingsByCategory(BuildingCategory category)
        {
            List<BuildingData> result = new List<BuildingData>();
            foreach (BuildingData building in _buildingDictionary.Values)
            {
                if (building.Category == category)
                {
                    result.Add(building);
                }
            }
            return result;
        }

        #endregion

        #region Recipe Access

        public RecipeData GetRecipe(string recipeID)
        {
            _recipeDictionary.TryGetValue(recipeID, out RecipeData recipe);
            return recipe;
        }

        public List<RecipeData> GetAllRecipes()
        {
            return new List<RecipeData>(_recipeDictionary.Values);
        }

        public List<RecipeData> GetRecipesForStation(CraftingStationType stationType)
        {
            if (_recipesByStation.TryGetValue(stationType, out List<RecipeData> recipes))
            {
                return new List<RecipeData>(recipes);
            }
            return new List<RecipeData>();
        }

        public List<RecipeData> GetRecipesByType(RecipeType type)
        {
            List<RecipeData> result = new List<RecipeData>();
            foreach (RecipeData recipe in _recipeDictionary.Values)
            {
                if (recipe.RecipeType == type)
                {
                    result.Add(recipe);
                }
            }
            return result;
        }

        #endregion

        #region Enemy Access

        public EnemyData GetEnemy(string enemyID)
        {
            _enemyDictionary.TryGetValue(enemyID, out EnemyData enemy);
            return enemy;
        }

        public List<EnemyData> GetAllEnemies()
        {
            return new List<EnemyData>(_enemyDictionary.Values);
        }

        public List<EnemyData> GetEnemiesByType(EnemyType type)
        {
            List<EnemyData> result = new List<EnemyData>();
            foreach (EnemyData enemy in _enemyDictionary.Values)
            {
                if (enemy.EnemyType == type)
                {
                    result.Add(enemy);
                }
            }
            return result;
        }

        #endregion

        #region Quest Access

        public QuestData GetQuest(string questID)
        {
            _questDictionary.TryGetValue(questID, out QuestData quest);
            return quest;
        }

        public List<QuestData> GetAllQuests()
        {
            return new List<QuestData>(_questDictionary.Values);
        }

        public List<QuestData> GetQuestsByType(QuestType type)
        {
            List<QuestData> result = new List<QuestData>();
            foreach (QuestData quest in _questDictionary.Values)
            {
                if (quest.QuestType == type)
                {
                    result.Add(quest);
                }
            }
            return result;
        }

        #endregion

        #region Resource Node Access

        public ResourceNodeData GetResourceNode(string nodeID)
        {
            _resourceNodeDictionary.TryGetValue(nodeID, out ResourceNodeData node);
            return node;
        }

        public List<ResourceNodeData> GetAllResourceNodes()
        {
            return new List<ResourceNodeData>(_resourceNodeDictionary.Values);
        }

        public List<ResourceNodeData> GetResourceNodesByType(ResourceNodeType type)
        {
            List<ResourceNodeData> result = new List<ResourceNodeData>();
            foreach (ResourceNodeData node in _resourceNodeDictionary.Values)
            {
                if (node.NodeType == type)
                {
                    result.Add(node);
                }
            }
            return result;
        }

        #endregion

        #region Editor Methods

        public void RefreshData()
        {
            BuildDictionaries();
            OrganizeRecipesByStation();
            Debug.Log("[DataManager] Data dictionaries refreshed.");
        }

        #endregion
    }
}
