namespace AnoGame.Domain.Inventory.Services
{
    public interface IInventoryService
    {
        void TriggerItemCollected(string itemName, string uniqueId = null);
        void RegisterItemHandler(string itemName, System.Action<string> handler);
        void UnregisterItemHandler(string itemName, System.Action<string> handler);
        bool HasItem(string itemName);
    }
}

