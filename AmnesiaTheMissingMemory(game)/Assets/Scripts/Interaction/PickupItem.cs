using UnityEngine;

// An item the player picks up with E (letters, photos, keys, weapons...).
// If collectibleId matches a CollectibleID in the database, it is saved as collected.
public class PickupItem : Interactable
{
    [SerializeField] private string itemName = "Item";
    [SerializeField] private int collectibleId = 0; // 0 = not saved to the database
    [SerializeField] private AchievementInfo[] achievements; // unlocked when picked up, can be left empty

    public override string Prompt => $"Pick up {itemName}";

    public override void Interact(PlayerInteractor interactor)
    {
        if (collectibleId > 0)
        {
            if (DbClient.Instance != null) DbClient.Instance.CollectItem(collectibleId);
            else Debug.LogWarning("No DbClient in the scene, pickup not saved.");
        }

        AchievementManager.UnlockAll(achievements);
        interactor.ShowMessage($"Picked up {itemName}");
        // PLACEHOLDER: add a pickup sound / inventory here for the final version
        Destroy(gameObject);
    }
}
