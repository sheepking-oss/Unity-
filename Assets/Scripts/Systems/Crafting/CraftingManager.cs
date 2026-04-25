using UnityEngine;
using System.Collections.Generic;
using SurvivalGame.Data.Crafting;
using SurvivalGame.Data.Items;
using SurvivalGame.Inventory;
using SurvivalGame.Data.Managers;
using SurvivalGame.Core.Events;
using SurvivalGame.Core.Managers;

namespace SurvivalGame.Systems.Crafting
{
    public class CraftingManager : ManagerBase
    {
        [Header("References")]
        [SerializeField] private InventoryManager _inventoryManager;
        [SerializeField] private DataManager _dataManager;

        private Dictionary<string, CraftingProcess> _activeCraftingProcesses = new Dictionary<string, CraftingProcess>();
        private List<CraftingStation> _registeredStations = new List<CraftingStation>();

        public static CraftingManager Instance => GetInstance<CraftingManager>();

        public int ActiveCraftingCount => _activeCraftingProcesses.Count;
        public IReadOnlyList<CraftingStation> RegisteredStations => _registeredStations.AsReadOnly();

        public override void Initialize()
        {
            base.Initialize();

            if (_inventoryManager == null)
                _inventoryManager = InventoryManager.Instance;
            if (_dataManager == null)
                _dataManager = DataManager.Instance;
        }

        private void Update()
        {
            UpdateCraftingProcesses();
        }

        private void UpdateCraftingProcesses()
        {
            List<string> completedKeys = new List<string>();

            foreach (var kvp in _activeCraftingProcesses)
            {
                CraftingProcess process = kvp.Value;
                process.Progress += Time.deltaTime;

                if (process.Progress >= process.TotalTime)
                {
                    completedKeys.Add(kvp.Key);
                    CompleteCraftingProcess(process);
                }
            }

            foreach (string key in completedKeys)
            {
                _activeCraftingProcesses.Remove(key);
            }
        }

        public bool CanCraft(RecipeData recipe)
        {
            if (recipe == null) return false;
            if (_inventoryManager == null) return false;

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (ingredient.Optional) continue;

                if (!_inventoryManager.HasItem(ingredient.Item, ingredient.Quantity))
                {
                    return false;
                }
            }

            return true;
        }

        public bool TryCraft(RecipeData recipe, CraftingStationType stationType = CraftingStationType.Hand)
        {
            if (!CanCraft(recipe))
            {
                Debug.Log($"Cannot craft {recipe.RecipeName}: Not enough materials");
                return false;
            }

            if (recipe.RequiredStation != stationType && stationType != CraftingStationType.Hand)
            {
                Debug.Log($"Cannot craft {recipe.RecipeName}: Wrong crafting station");
                return false;
            }

            foreach (RecipeIngredient ingredient in recipe.Ingredients)
            {
                if (ingredient.Item == null) continue;
                if (ingredient.Optional) continue;

                _inventoryManager.RemoveItem(ingredient.Item, ingredient.Quantity);
            }

            string processID = System.Guid.NewGuid().ToString();
            CraftingProcess process = new CraftingProcess
            {
                ProcessID = processID,
                Recipe = recipe,
                StationType = stationType,
                Progress = 0f,
                TotalTime = recipe.CraftTime
            };

            _activeCraftingProcesses[processID] = process;
            EventManager.TriggerEvent("OnCraftingStarted", recipe);

            if (recipe.CraftTime <= 0f)
            {
                CompleteCraftingProcess(process);
                _activeCraftingProcesses.Remove(processID);
            }

            return true;
        }

        private void CompleteCraftingProcess(CraftingProcess process)
        {
            if (process.Recipe == null) return;

            ItemInstance resultItem = new ItemInstance(process.Recipe.ResultItem, process.Recipe.ResultQuantity);

            if (_inventoryManager != null)
            {
                _inventoryManager.AddItem(resultItem);
            }

            EventManager.TriggerEvent("OnCraftingCompleted", process.Recipe);
            Debug.Log($"Crafted: {process.Recipe.ResultItem?.ItemName} x{process.Recipe.ResultQuantity}");
        }

        public List<RecipeData> GetAvailableRecipes(CraftingStationType stationType)
        {
            if (_dataManager == null)
                return new List<RecipeData>();

            List<RecipeData> stationRecipes = _dataManager.GetRecipesForStation(stationType);
            List<RecipeData> available = new List<RecipeData>();

            foreach (RecipeData recipe in stationRecipes)
            {
                if (CanCraft(recipe))
                {
                    available.Add(recipe);
                }
            }

            return available;
        }

        public List<RecipeData> GetAllRecipesForStation(CraftingStationType stationType)
        {
            if (_dataManager == null)
                return new List<RecipeData>();

            return _dataManager.GetRecipesForStation(stationType);
        }

        public void RegisterCraftingStation(CraftingStation station)
        {
            if (station == null) return;
            if (!_registeredStations.Contains(station))
            {
                _registeredStations.Add(station);
            }
        }

        public void UnregisterCraftingStation(CraftingStation station)
        {
            if (station == null) return;
            _registeredStations.Remove(station);
        }

        public CraftingStation GetNearestCraftingStation(CraftingStationType type, Vector3 position, float maxDistance = 10f)
        {
            CraftingStation nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (CraftingStation station in _registeredStations)
            {
                if (station == null) continue;
                if (station.StationType != type) continue;

                float distance = Vector3.Distance(position, station.transform.position);
                if (distance <= maxDistance && distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = station;
                }
            }

            return nearest;
        }

        public void CancelAllCrafting()
        {
            foreach (var kvp in _activeCraftingProcesses)
            {
                CraftingProcess process = kvp.Value;
                if (process.Recipe != null)
                {
                    foreach (RecipeIngredient ingredient in process.Recipe.Ingredients)
                    {
                        if (ingredient.Item != null && !ingredient.Optional)
                        {
                            _inventoryManager?.AddItem(ingredient.Item, ingredient.Quantity);
                        }
                    }
                }
            }

            _activeCraftingProcesses.Clear();
        }
    }

    [System.Serializable]
    public class CraftingProcess
    {
        public string ProcessID;
        public RecipeData Recipe;
        public CraftingStationType StationType;
        public float Progress;
        public float TotalTime;

        public float ProgressPercent => TotalTime > 0 ? Progress / TotalTime : 1f;
    }
}
