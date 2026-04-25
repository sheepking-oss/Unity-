using UnityEngine;

namespace SurvivalGame.Core.Interfaces
{
    public interface IInteractable
    {
        void Interact(GameObject interactor);
        string GetInteractionText();
        bool CanInteract(GameObject interactor);
    }
}
