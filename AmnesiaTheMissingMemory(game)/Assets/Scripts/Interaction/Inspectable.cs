using UnityEngine;

// something you look at closely with E, like the body during the day.
// shows a message, gives achievements and can end the level.
public class Inspectable : Interactable
{
    [SerializeField] [TextArea] private string message = "";
    [SerializeField] private AchievementInfo[] achievements; // achievements you get for inspecting it
    [SerializeField] private bool completesLevel = false;
    [SerializeField] private string nextScene = "MainMenu";  // where to go after, if completesLevel is on

    bool inspected;

    void Reset() => promptText = "Inspect";

    public override bool CanInteract => !inspected;

    public override void Interact(PlayerInteractor interactor)
    {
        inspected = true;
        if (!string.IsNullOrEmpty(message)) interactor.ShowMessage(message);

        AchievementManager.UnlockAll(achievements);

        // placeholder: maybe a close up camera or clue screen here later
        if (completesLevel && !string.IsNullOrEmpty(nextScene)) SceneFader.GoTo(nextScene);
    }
}
