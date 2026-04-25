namespace SurvivalGame.Core.Interfaces
{
    public interface IInventoryItem
    {
        int MaxStackSize { get; }
        int CurrentStackSize { get; set; }
        bool IsStackable { get; }
        bool CanStackWith(IInventoryItem other);
    }
}
