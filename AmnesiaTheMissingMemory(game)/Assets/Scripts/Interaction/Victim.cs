using UnityEngine;

// A victim the killer can attack with E. Starts the space-mash KillMinigame.
// Win = victim dies and the achievement unlocks. Lose = victim breaks free, try again after a cooldown.
public class Victim : Interactable
{
    [SerializeField] private string achievementOnKill = "Night 1 Complete"; // must match Achievements.Name in the database
    [SerializeField] private string achievementDescription = "You finished the first night.";
    [SerializeField] private float retryCooldown = 2f;

    public bool IsDead { get; private set; }

    float nextAttemptTime;

    void Reset() => promptText = "Kill";

    public override bool CanInteract => !IsDead && Time.time >= nextAttemptTime
                                        && KillMinigame.Instance != null && !KillMinigame.Instance.IsRunning;

    public override void Interact(PlayerInteractor interactor)
    {
        KillMinigame.Instance.Begin(this, interactor);
    }

    // called by KillMinigame
    public void OnKilled()
    {
        IsDead = true;
        PlayDeath();

        if (!string.IsNullOrEmpty(achievementOnKill))
        {
            if (AchievementManager.Instance != null) AchievementManager.Instance.Unlock(achievementOnKill, achievementDescription);
            else Debug.LogWarning("No AchievementManager in the scene, achievement not shown.");
        }
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
