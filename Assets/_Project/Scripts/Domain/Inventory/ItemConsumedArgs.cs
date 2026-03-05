public readonly struct ItemConsumedArgs
{
    public readonly string ItemId;
    public readonly int Quantity;
    public readonly UnityEngine.GameObject User;
    public readonly UnityEngine.Vector3 UsePosition;

    public ItemConsumedArgs(string itemId, int quantity, UnityEngine.GameObject user, UnityEngine.Vector3 usePos)
    {
        ItemId = itemId;
        Quantity = quantity;
        User = user;
        UsePosition = usePos;
    }
}