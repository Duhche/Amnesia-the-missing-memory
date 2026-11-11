using UnityEngine;
using UnityEngine.Events;

// a general E object for later (doors, switches, notes...).
// shows a message and does whatever you set up in "On Interact" in the Inspector.
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
