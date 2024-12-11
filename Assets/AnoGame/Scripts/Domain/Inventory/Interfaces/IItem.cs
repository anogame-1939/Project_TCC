namespace AnoGame.Domain.Item.Models
{
    public interface IItem
    {
        string ItemName { get; }
        bool IsStackable { get; }
        int MaxStackSize { get; }
    }
}