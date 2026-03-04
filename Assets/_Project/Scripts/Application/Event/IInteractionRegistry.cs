namespace AnoGame.AnoFlow
{
    /// <summary>
    /// インタラクション登録/解除のインターフェース。
    /// ゲーム側のInteractionControllerが実装する。
    /// </summary>
    public interface IInteractionRegistry
    {
        void Register(object interactable);
        void Unregister(object interactable);
    }
}
