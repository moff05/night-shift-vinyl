using UnityEngine;

/// <summary>Spins the platter smoothly (33 1/3 RPM) once the record has landed. Hook StartSpin to the platter VRSnapZone's onSnapped.</summary>
public class PlatterSpinner : MonoBehaviour, ISimResettable
{
    [SerializeField] Transform platter;
    [SerializeField] Vector3 localAxis = Vector3.up;
    [SerializeField] float rpm = 33.33f;
    [SerializeField] float spinUpSeconds = 1.5f;

    bool spinning;
    float degreesPerSecond;
    Quaternion startRotation;

    void Awake()
    {
        if (platter == null) platter = transform;
        startRotation = platter.localRotation;
    }

    void Update()
    {
        float target = spinning ? rpm * 6f : 0f;                  // rpm * 360 / 60
        float accel = spinUpSeconds > 0f ? rpm * 6f / spinUpSeconds : float.MaxValue;
        degreesPerSecond = Mathf.MoveTowards(degreesPerSecond, target, accel * Time.deltaTime);
        if (degreesPerSecond != 0f) platter.Rotate(localAxis, degreesPerSecond * Time.deltaTime, Space.Self);
    }

    public void StartSpin() => spinning = true;
    public void StopSpin() => spinning = false;

    public void ResetForSimulation()
    {
        spinning = false;
        degreesPerSecond = 0f;
        platter.localRotation = startRotation;
    }
}
