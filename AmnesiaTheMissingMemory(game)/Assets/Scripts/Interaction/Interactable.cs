using UnityEngine;

// Base class for anything the player can use with E.
// Don't add this one directly, use PickupItem, InteractableObject or Victim
// (or make your own script that inherits from it).
// The object needs a Collider so the player can aim at it.
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string promptText = "Interact";

    // text shown after "[E]"
    public virtual string Prompt => promptText;

    // return false to hide the prompt (already used, dead, on cooldown...)
    public virtual bool CanInteract => true;

    public abstract void Interact(PlayerInteractor interactor);
}
