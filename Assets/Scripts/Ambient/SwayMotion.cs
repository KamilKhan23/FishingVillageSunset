using UnityEngine;

/// <summary>
/// Swings an object back and forth around a local axis, like a lantern or drying fish hanging in the sea breeze.
/// </summary>
public class SwayMotion : MonoBehaviour
{
    [SerializeField] private Vector3 swayAxis = new Vector3(0f, 0f, 1f);
    [SerializeField] private float swayAngle = 6f;
    [SerializeField] private float swayCyclesPerSecond = 0.35f;
    [SerializeField] private float phaseOffset = 0f;

    private Quaternion startLocalRotation;

    private void Awake()
    {
        startLocalRotation = transform.localRotation;
    }

    private void Update()
    {
        float angle = Mathf.Sin(Time.time * swayCyclesPerSecond * Mathf.PI * 2f + phaseOffset) * swayAngle;
        transform.localRotation = startLocalRotation * Quaternion.AngleAxis(angle, swayAxis.normalized);
    }
}
