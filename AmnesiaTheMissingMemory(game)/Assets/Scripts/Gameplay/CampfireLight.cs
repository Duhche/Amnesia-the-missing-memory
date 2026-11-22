using UnityEngine;

// makes the campfire light flicker like a real fire.
// goes on the light, the Level 1 builder adds it on its own.
[RequireComponent(typeof(Light))]
public class CampfireLight : MonoBehaviour
{
    [SerializeField] private float baseIntensity = 3f;
    [SerializeField] private float flicker = 0.35f; // how much it goes up and down
    [SerializeField] private float speed = 4f;

    Light fireLight;
    float seed;

    void Awake()
    {
        fireLight = GetComponent<Light>();
        seed = Random.value * 100f; // so two fires wouldn't flicker the same
    }

    void Update()
    {
        // smooth random noise between 0 and 1
        float noise = Mathf.PerlinNoise(seed, Time.time * speed);
        fireLight.intensity = baseIntensity * (1f - flicker + noise * flicker * 2f);
    }
}
