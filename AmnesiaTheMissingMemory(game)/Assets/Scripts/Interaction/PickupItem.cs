using UnityEngine;

// something you pick up with E (knife, collectibles...).
// if it has a collectible id it gets saved in the database too.
public class PickupItem : Interactable
{
    [SerializeField] private string itemName = "Item";
    [SerializeField] private int collectibleId = 0; // 0 means don't save it
    [SerializeField] private AchievementInfo[] achievements; // achievements you get for picking it up, can be empty

    public override string Prompt => $"Pick up {itemName}";

    public override void Interact(PlayerInteractor interactor)
    {
        // save it for the save slot you're playing on
        if (collectibleId > 0)
        {
            if (SaveSession.Slot == 0) Debug.Log("Collectible not saved, you're not playing on a save slot.");
            else if (DbClient.Instance != null) DbClient.Instance.CollectItem(collectibleId);
            else Debug.LogWarning("No DbClient in the scene, pickup not saved.");
        }

        PlayerInventory.Add(itemName);
        AchievementManager.UnlockAll(achievements);
        interactor.ShowMessage($"Picked up {itemName}");
        // placeholder: add a pickup sound here later
        Destroy(gameObject);
    }
}
