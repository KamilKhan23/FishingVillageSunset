using UnityEngine;

/// <summary>
/// Spins an object at a constant speed. Used for the lighthouse lamp and its sweeping beams.
/// </summary>
public class Rotator : MonoBehaviour
{
    [SerializeField] private Vector3 degreesPerSecond = new Vector3(0f, 30f, 0f);
    [SerializeField] private Space rotationSpace = Space.Self;

    private void Update()
    {
        transform.Rotate(degreesPerSecond * Time.deltaTime, rotationSpace);
    }
}
