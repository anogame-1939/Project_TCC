namespace AnoGame.Domain.Inventory.Models
{
    public interface IItem
    {
        string ItemName { get; }
        bool IsStackable { get; }
        int MaxStackSize { get; }
    }
}