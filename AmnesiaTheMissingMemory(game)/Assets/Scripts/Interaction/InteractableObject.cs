using UnityEngine;
using UnityEngine.Events;

// A general "use with E" object: doors, switches, notes on the wall...
// Shows a message and runs whatever you hook up in "On Interact" in the Inspector
// (e.g. turn on a light, play an animation, open a door).
public class InteractableObject : Interactable
{
    [SerializeField] [TextArea] private string message = "";
    [SerializeField] private bool oneTimeOnly = false;
    [SerializeField] private UnityEvent onInteract;

    bool used;

    public override bool CanInteract => !(oneTimeOnly && used);

    public override void Interact(PlayerInteractor interactor)
    {
        used = true;
        if (!string.IsNullOrEmpty(message)) interactor.ShowMessage(message);
        onInteract.Invoke();
    }
}
