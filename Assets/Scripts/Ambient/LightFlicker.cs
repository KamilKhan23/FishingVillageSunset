using UnityEngine;

/// <summary>
/// Varies a light's brightness with Perlin noise, so campfires and oil lanterns flicker instead of staying flat.
/// </summary>
[RequireComponent(typeof(Light))]
public class LightFlicker : MonoBehaviour
{
    [SerializeField] private float flickerAmount = 0.25f;
    [SerializeField] private float flickerSpeed = 3f;

    private Light targetLight;
    private float baseIntensity;
    private float noiseSeed;

    private void Awake()
    {
        targetLight = GetComponent<Light>();
        baseIntensity = targetLight.intensity;
        noiseSeed = Random.value * 100f;
    }

    private void Update()
    {
        float noise = Mathf.PerlinNoise(noiseSeed, Time.time * flickerSpeed); // 0..1
        targetLight.intensity = baseIntensity * (1f + (noise - 0.5f) * 2f * flickerAmount);
    }
}
