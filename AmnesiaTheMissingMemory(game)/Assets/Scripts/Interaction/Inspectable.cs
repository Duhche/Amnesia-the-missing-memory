using UnityEngine;

// Something the player looks at closely with E, like the victim's body the next day.
// Shows a message, unlocks achievements and can finish the level.
public class Inspectable : Interactable
{
    [SerializeField] [TextArea] private string message = "";
    [SerializeField] private AchievementInfo[] achievements; // unlocked when inspected
    [SerializeField] private bool completesLevel = false;
    [SerializeField] private string nextScene = "MainMenu";  // loaded when completesLevel is ticked

    bool inspected;

    void Reset() => promptText = "Inspect";

    public override bool CanInteract => !inspected;

    public override void Interact(PlayerInteractor interactor)
    {
        inspected = true;
        if (!string.IsNullOrEmpty(message)) interactor.ShowMessage(message);

        AchievementManager.UnlockAll(achievements);

        // PLACEHOLDER: add a close-up camera / clue screen here for the final version
        if (completesLevel && !string.IsNullOrEmpty(nextScene)) SceneFader.GoTo(nextScene);
    }
}
