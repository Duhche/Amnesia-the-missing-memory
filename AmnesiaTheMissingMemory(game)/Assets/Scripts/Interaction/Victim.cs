using UnityEngine;

// A victim the killer can attack with E. Starts the KillMinigame:
// mash space with bare hands, or the timing strike if the player picked up the knife.
// Win = victim dies and the achievement unlocks. Lose = victim breaks free, try again after a cooldown.
public class Victim : Interactable
{
    [SerializeField] private string achievementOnKill = "Night 1 Complete"; // must match Achievements.Name in the database
    [SerializeField] private string achievementDescription = "You finished the first night.";
    [SerializeField] private float retryCooldown = 2f;
    [SerializeField] private string nextScene = "Level1(day)"; // loaded after the kill, leave empty to stay

    [Header("Knife")]
    [SerializeField] private string weaponItem = "Knife"; // PickupItem name that switches to the knife minigame
    [SerializeField] private string knifeAchievement = "Cold Steel";
    [SerializeField] private string knifeAchievementDescription = "Kill someone with the knife.";

    public bool IsDead { get; private set; }

    float nextAttemptTime;

    void Reset() => promptText = "Kill";

    bool HasKnife => !string.IsNullOrEmpty(weaponItem) && PlayerInventory.Has(weaponItem);

    public override string Prompt => HasKnife ? $"{promptText} with {weaponItem}" : promptText;

    public override bool CanInteract => !IsDead && Time.time >= nextAttemptTime
                                        && KillMinigame.Instance != null && !KillMinigame.Instance.IsRunning;

    public override void Interact(PlayerInteractor interactor)
    {
        KillMinigame.Instance.Begin(this, interactor, HasKnife);
    }

    // called by KillMinigame
    public void OnKilled(bool withKnife)
    {
        IsDead = true;
        PlayDeath();

        if (AchievementManager.Instance != null)
        {
            if (!string.IsNullOrEmpty(achievementOnKill)) AchievementManager.Instance.Unlock(achievementOnKill, achievementDescription);
            if (withKnife && !string.IsNullOrEmpty(knifeAchievement)) AchievementManager.Instance.Unlock(knifeAchievement, knifeAchievementDescription);
        }
        else Debug.LogWarning("No AchievementManager in the scene, achievement not shown.");

        if (!string.IsNullOrEmpty(nextScene)) SceneFader.GoTo(nextScene);
    }

    // called by KillMinigame
    public void OnEscaped(PlayerInteractor interactor)
    {
        nextAttemptTime = Time.time + retryCooldown;
        interactor.ShowMessage("They broke free!");
    }

    // PLACEHOLDER: replace with a death animation, sound and blood effect for the final version.
    // For now the victim falls over and turns dark red.
    void PlayDeath()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        float groundY = rend != null ? rend.bounds.min.y : transform.position.y;

        transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);

        if (rend != null)
        {
            transform.position += Vector3.up * (groundY - rend.bounds.min.y); // lie on the floor, not float
            rend.material.color = new Color(0.35f, 0.02f, 0.02f);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }
}
