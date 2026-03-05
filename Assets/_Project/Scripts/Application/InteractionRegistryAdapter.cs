using AnoGame.AnoFlow;
using AnoGame.Application.Player;
using AnoGame.Application.Player.Interaction;

namespace AnoGame.Application
{
    /// <summary>
    /// IInteractionRegistryの実装。
    /// InteractionControllerのstatic APIへの橋渡しを行う。
    /// </summary>
    public class InteractionRegistryAdapter : IInteractionRegistry
    {
        public void Register(object interactable)
        {
            if (interactable is IInteractable target)
            {
                InteractionController.RegisterManual(target);
            }
        }

        public void Unregister(object interactable)
        {
            if (interactable is IInteractable target)
            {
                InteractionController.UnregisterManual(target);
            }
        }
    }
}
