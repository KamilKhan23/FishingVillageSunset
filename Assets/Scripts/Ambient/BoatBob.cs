using UnityEngine;

/// <summary>
/// Makes a boat (or buoy) ride the swell: it rises and falls, and rocks gently fore-and-aft and side to side.
/// Each axis uses its own speed so the motion never looks like a simple loop.
/// </summary>
public class BoatBob : MonoBehaviour
{
    [SerializeField] private float bobHeight = 0.12f;
    [SerializeField] private float bobCyclesPerSecond = 0.22f;
    [SerializeField] private float rollDegrees = 3f;
    [SerializeField] private float rollCyclesPerSecond = 0.17f;
    [SerializeField] private float pitchDegrees = 1.5f;
    [SerializeField] private float pitchCyclesPerSecond = 0.13f;
    [SerializeField] private float phaseOffset = 0f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    private void Awake()
    {
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;
    }

    private void Update()
    {
        float t = Time.time;
        float twoPi = Mathf.PI * 2f;
        float bob = Mathf.Sin(t * bobCyclesPerSecond * twoPi + phaseOffset) * bobHeight;
        float roll = Mathf.Sin(t * rollCyclesPerSecond * twoPi + phaseOffset * 1.7f) * rollDegrees;
        float pitch = Mathf.Sin(t * pitchCyclesPerSecond * twoPi + phaseOffset * 0.6f) * pitchDegrees;

        transform.localPosition = startPosition + Vector3.up * bob;
        transform.localRotation = startRotation * Quaternion.Euler(pitch, 0f, roll);
    }
}
