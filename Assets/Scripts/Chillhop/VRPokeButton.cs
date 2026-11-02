using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR;

/// <summary>Controller-only rumble. Hands have no haptics, so this quietly does nothing in hand tracking.</summary>
public static class VRHaptics
{
    static readonly List<InputDevice> devices = new List<InputDevice>();

    public static void Pulse(float amplitude, float duration)
    {
        devices.Clear();
        InputDevices.GetDevicesWithCharacteristics(InputDeviceCharacteristics.Controller, devices);
        foreach (InputDevice device in devices)
        {
            if (device.TryGetHapticCapabilities(out HapticCapabilities caps) && caps.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, duration);
        }
    }
}

/// <summary>
/// Physical poke button for the START / RESTART buttons and the MPC pad.
/// A trigger collider senses <see cref="VRFingertip"/> spheres; the cap follows the fingertip down,
/// fires <see cref="onPressed"/> at <see cref="pressPoint"/> of its travel, and springs back when the finger leaves.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRPokeButton : MonoBehaviour, ISimResettable
{
    [Header("Cap motion")]
    [SerializeField] Transform cap;
    [Tooltip("Direction the cap moves when pressed, in the cap parent's local space.")]
    [SerializeField] Vector3 pressDirection = Vector3.down;
    [Tooltip("Max depression in the cap parent's local units (metres at scale 1).")]
    [SerializeField] float travel = 0.012f;
    [Tooltip("Distance from the cap pivot to its top face, so the finger meets the surface, not the pivot.")]
    [SerializeField] float surfaceHeight = 0.005f;
    [SerializeField] float springSpeed = 14f;
    [SerializeField, Range(0.5f, 0.95f)] float pressPoint = 0.8f;
    [SerializeField, Range(0.05f, 0.6f)] float releasePoint = 0.4f;
    [SerializeField] float cooldown = 0.25f;

    [Header("Step gate")]
    [SerializeField] bool gateByStep = true;
    [SerializeField] SimStep requiredStep = SimStep.MenuStart;

    [Header("Feedback")]
    [SerializeField] AudioSource clickSource;
    [SerializeField] AudioClip pressClip;
    [SerializeField] Renderer flashRenderer;
    [SerializeField, ColorUsage(false, true)] Color idleGlow = new Color(0.35f, 0.14f, 0f);
    [SerializeField, ColorUsage(false, true)] Color flashGlow = new Color(8f, 3.2f, 0f);
    [SerializeField] float flashSeconds = 0.4f;
    [SerializeField] float hapticAmplitude = 0.35f;
    [SerializeField] float hapticSeconds = 0.05f;

    [Header("Events")]
    public UnityEvent onPressed = new UnityEvent();
    public UnityEvent onReleased = new UnityEvent();

    static readonly int EmissionColor = Shader.PropertyToID("_EmissionColor");
    static readonly System.Predicate<VRFingertip> IsDead = t => t == null;

    readonly HashSet<VRFingertip> contacts = new HashSet<VRFingertip>();
    Vector3 restLocalPos;
    Vector3 dir;
    float depth;
    bool latched;                  // true from press until the finger lifts past releasePoint
    float armedAt;
    float lastFire = -10f;
    float flashClock = 99f;
    Material flashMaterial;

    void Awake()
    {
        if (cap == null) cap = transform;
        restLocalPos = cap.localPosition;
        dir = pressDirection.sqrMagnitude > 0f ? pressDirection.normalized : Vector3.down;

        if (flashRenderer != null)
        {
            flashMaterial = flashRenderer.material;          // per-button instance
            flashMaterial.EnableKeyword("_EMISSION");
            flashMaterial.SetColor(EmissionColor, idleGlow);
        }
    }

    void OnEnable()
    {
        // Appear "already pressed": a finger that is still down from the previous button (RESTART sits where START does)
        // must lift before this one can fire.
        latched = true;
        armedAt = Time.time + 0.3f;       // physics re-reports a resting finger a frame or two late
    }

    void OnDisable()
    {
        contacts.Clear();
        latched = false;
        depth = 0f;
        if (cap != null) cap.localPosition = restLocalPos;
    }

    void OnTriggerEnter(Collider other)
    {
        var tip = other.GetComponent<VRFingertip>();
        if (tip != null) contacts.Add(tip);
    }

    void OnTriggerExit(Collider other)
    {
        var tip = other.GetComponent<VRFingertip>();
        if (tip != null) contacts.Remove(tip);
    }

    void Update()
    {
        contacts.RemoveWhere(IsDead);

        float target = 0f;
        foreach (VRFingertip tip in contacts) target = Mathf.Max(target, DepthFor(tip));

        // Follow the finger instantly on the way down, spring back smoothly on the way up.
        depth = target > depth ? target : Mathf.Lerp(depth, target, 1f - Mathf.Exp(-springSpeed * Time.deltaTime));
        cap.localPosition = restLocalPos + dir * depth;

        if (!latched && depth >= travel * pressPoint)
        {
            latched = true;                                   // latch even when gated so a held finger can't fire later
            if (CanFire()) Fire();
        }
        else if (latched && depth <= travel * releasePoint && Time.time >= armedAt)
        {
            latched = false;
            onReleased.Invoke();
        }

        UpdateFlash();
    }

    float DepthFor(VRFingertip tip)
    {
        Transform space = cap.parent != null ? cap.parent : transform;
        Vector3 tipLocal = space.InverseTransformPoint(tip.transform.position);
        Vector3 surface = restLocalPos - dir * surfaceHeight;
        float radius = tip.Radius / Mathf.Max(0.0001f, space.lossyScale.x);
        return Mathf.Clamp(Vector3.Dot(tipLocal - surface, dir) + radius, 0f, travel);
    }

    bool CanFire()
    {
        if (Time.time - lastFire < cooldown) return false;
        return !gateByStep || VinylSimulationManager.IsStepActive(requiredStep);
    }

    /// <summary>
    /// Fire without a physical fingertip. Hook this to an XRI poke or select event
    /// if you would rather use XRPokeFilter than the fingertip trigger.
    /// </summary>
    public void Press()
    {
        depth = travel;
        if (CanFire()) Fire();
    }

    void Fire()
    {
        lastFire = Time.time;
        flashClock = 0f;
        if (clickSource != null && pressClip != null) clickSource.PlayOneShot(pressClip);
        VRHaptics.Pulse(hapticAmplitude, hapticSeconds);
        onPressed.Invoke();
    }

    void UpdateFlash()
    {
        if (flashMaterial == null || flashClock > flashSeconds) return;
        flashClock += Time.deltaTime;
        float k = Mathf.Clamp01(flashClock / flashSeconds);
        flashMaterial.SetColor(EmissionColor, Color.Lerp(flashGlow, idleGlow, k * k));
    }

    public void ResetForSimulation()
    {
        depth = 0f;
        flashClock = 99f;
        lastFire = -10f;
        if (cap != null) cap.localPosition = restLocalPos;
        if (flashMaterial != null) flashMaterial.SetColor(EmissionColor, idleGlow);
    }
}
