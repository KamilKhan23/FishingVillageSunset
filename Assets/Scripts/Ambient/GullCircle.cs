using UnityEngine;

/// <summary>
/// Flies a seagull in a lazy circle around a centre point, banking into the turn and rising and falling slightly.
/// The centre is wherever the gull's parent object sits, so circles can be moved in the Scene view.
/// </summary>
public class GullCircle : MonoBehaviour
{
    [SerializeField] private float radius = 14f;
    [SerializeField] private float degreesPerSecond = 18f;
    [SerializeField] private float startAngle = 0f;
    [SerializeField] private float heightWave = 1.5f;
    [SerializeField] private float heightCyclesPerSecond = 0.1f;
    [SerializeField] private float bankDegrees = 18f;
    [SerializeField] private bool clockwise = false;

    private void Update()
    {
        float direction = clockwise ? -1f : 1f;
        float angle = (startAngle + Time.time * degreesPerSecond * direction) * Mathf.Deg2Rad;
        float height = Mathf.Sin(Time.time * heightCyclesPerSecond * Mathf.PI * 2f + startAngle) * heightWave;

        Vector3 local = new Vector3(Mathf.Cos(angle) * radius, height, Mathf.Sin(angle) * radius);
        Vector3 tangent = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle)) * direction;

        transform.localPosition = local;
        transform.localRotation = Quaternion.LookRotation(tangent, Vector3.up) * Quaternion.Euler(0f, 0f, bankDegrees * direction);
    }
}
