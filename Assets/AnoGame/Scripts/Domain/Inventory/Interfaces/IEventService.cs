namespace AnoGame.Domain.Inventory.Services
{
    public interface IEventService 
    {
        void TriggerKeyItemEvent(string itemName);
        void RegisterKeyItemHandler(string itemName, System.Action handler);
        void UnregisterKeyItemHandler(string itemName, System.Action handler);
    }

    public interface IKeyItemService 
    {
        bool IsKeyItem(string itemName);
    }
}
