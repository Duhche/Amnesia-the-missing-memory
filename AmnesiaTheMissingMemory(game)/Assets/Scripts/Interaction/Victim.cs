using UnityEngine;

// the person you kill at night. pressing E starts the kill minigame.
// no knife = mash space, with the knife = timing strike.
// win and they die, lose and they break free for a bit.
public class Victim : Interactable
{
    [SerializeField] private string achievementOnKill = "Night 1 Complete"; // has to match the name in the database
    [SerializeField] private string achievementDescription = "You finished the first night.";
    [SerializeField] private float retryCooldown = 2f;
    [SerializeField] private string nextScene = "Level1(day)"; // where to go after the kill, empty = stay

    [Header("Knife")]
    [SerializeField] private string weaponItem = "Knife"; // the pickup name that counts as the knife
    [SerializeField] private string knifeAchievement = "Cold Steel";
    [SerializeField] private string knifeAchievementDescription = "Kill someone with the knife.";

    public bool IsDead { get; private set; }

    float nextAttemptTime;

    void Reset() => promptText = "Kill";

    bool HasKnife => !string.IsNullOrEmpty(weaponItem) && PlayerInventory.Has(weaponItem);

    // says "Kill with Knife" when you have it
    public override string Prompt => HasKnife ? $"{promptText} with {weaponItem}" : promptText;

    public override bool CanInteract => !IsDead && Time.time >= nextAttemptTime
                                        && KillMinigame.Instance != null && !KillMinigame.Instance.IsRunning;

    public override void Interact(PlayerInteractor interactor)
    {
        KillMinigame.Instance.Begin(this, interactor, HasKnife);
    }

    // the minigame calls this when you win
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

    // the minigame calls this when you lose
    public void OnEscaped(PlayerInteractor interactor)
    {
        nextAttemptTime = Time.time + retryCooldown;
        interactor.ShowMessage("They broke free!");
    }

    // placeholder: for now they just fall over and turn dark red.
    // swap this for a real death animation, sound and blood later.
    void PlayDeath()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        float groundY = rend != null ? rend.bounds.min.y : transform.position.y;

        transform.rotation = Quaternion.Euler(90, transform.eulerAngles.y, 0);

        if (rend != null)
        {
            transform.position += Vector3.up * (groundY - rend.bounds.min.y); // put them on the floor so they don't float
            rend.material.color = new Color(0.35f, 0.02f, 0.02f);
        }

        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;
    }
}
