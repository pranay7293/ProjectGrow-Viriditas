public interface IInteractable
{
    void Interact();
    bool CanInteract();
    string GetInteractionPrompt();
    void SetInteractable(bool isInteractable);
}