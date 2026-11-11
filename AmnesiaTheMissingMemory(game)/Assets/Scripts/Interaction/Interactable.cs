using UnityEngine;

// the base for everything you can use with E.
// don't put this one on objects, use PickupItem, Inspectable, Victim or InteractableObject.
// the object needs a Collider so you can aim at it.
public abstract class Interactable : MonoBehaviour
{
    [SerializeField] protected string promptText = "Interact";

    // the text after "[E]"
    public virtual string Prompt => promptText;

    // false hides the prompt (already used, dead, waiting to retry...)
    public virtual bool CanInteract => true;

    // what happens when you press E
    public abstract void Interact(PlayerInteractor interactor);
}
