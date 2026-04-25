using UnityEngine;
using System.Collections.Generic;

namespace SurvivalGame.Data.Crafting
{
    [CreateAssetMenu(fileName = "NewRecipe", menuName = "SurvivalGame/Crafting/Recipe Data")]
    public class RecipeData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField] private string _recipeName = "New Recipe";
        [SerializeField] private string _recipeID = "recipe_0001";

        [Header("Recipe Type")]
        [SerializeField] private RecipeType _recipeType = RecipeType.Crafting;
        [SerializeField] private CraftingStationType _requiredStation = CraftingStationType.Hand;

        [Header("Input (Ingredients)")]
        [SerializeField] private List<RecipeIngredient> _ingredients = new List<RecipeIngredient>();

        [Header("Output (Result)")]
        [SerializeField] private Items.ItemData _resultItem;
        [SerializeField] private int _resultQuantity = 1;

        [Header("Crafting Settings")]
        [SerializeField] private float _craftTime = 1f;
        [SerializeField] private bool _isUnlockedByDefault = true;
        [SerializeField] private int _unlockLevel = 1;
        [SerializeField] private string[] _unlockRequirements;

        [Header("Furnace Specific")]
        [SerializeField] private float _fuelRequired = 0f;
        [SerializeField] private float _cookTime = 0f;

        #region Properties

        public string RecipeName => _recipeName;
        public string RecipeID => _recipeID;

        public RecipeType RecipeType => _recipeType;
        public CraftingStationType RequiredStation => _requiredStation;

        public List<RecipeIngredient> Ingredients => _ingredients;

        public Items.ItemData ResultItem => _resultItem;
        public int ResultQuantity => _resultQuantity;

        public float CraftTime => _craftTime;
        public bool IsUnlockedByDefault => _isUnlockedByDefault;
        public int UnlockLevel => _unlockLevel;
        public string[] UnlockRequirements => _unlockRequirements;

        public float FuelRequired => _fuelRequired;
        public float CookTime => _cookTime;

        #endregion

        public bool CanCraft(Dictionary<string, int> availableItems)
        {
            foreach (RecipeIngredient ingredient in _ingredients)
            {
                if (ingredient.Item == null) continue;

                string itemID = ingredient.Item.ItemID;
                if (!availableItems.ContainsKey(itemID) || availableItems[itemID] < ingredient.Quantity)
                {
                    return false;
                }
            }
            return true;
        }

        public HashSet<string> GetRequiredItemIDs()
        {
            HashSet<string> ids = new HashSet<string>();
            foreach (RecipeIngredient ingredient in _ingredients)
            {
                if (ingredient.Item != null)
                {
                    ids.Add(ingredient.Item.ItemID);
                }
            }
            return ids;
        }
    }

    [System.Serializable]
    public class RecipeIngredient
    {
        public Items.ItemData Item;
        public int Quantity;
        public bool Optional;
    }

    public enum RecipeType
    {
        Crafting,
        Smelting,
        Cooking,
        Drying,
        Brewing
    }

    public enum CraftingStationType
    {
        Hand,
        Workbench,
        Furnace,
        Anvil,
        Cauldron,
        Loom
    }
}
