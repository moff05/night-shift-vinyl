using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// One script, two controls. Slider mode is the vertical master fader; Rotary mode is the filter dial.
/// Pinch the part and the value follows your hand, clamped 0..1. The Interactable is an XRSimpleInteractable:
/// the script moves the visual itself, so there is no physics to jitter under optical tracking.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRFaderKnob : MonoBehaviour, ISimResettable
{
    public enum ControlMode { Slider, Rotary }
    public enum OutputTarget { None, FilterCutoff, MasterVolume }

    [Header("Control")]
    [SerializeField] ControlMode mode = ControlMode.Rotary;
    [Tooltip("The mesh that moves or turns. Defaults to this object.")]
    [SerializeField] Transform movingPart;
    [Tooltip("Slider: travel direction. Rotary: spin axis. In the moving part's PARENT space.")]
    [SerializeField] Vector3 axis = Vector3.up;
    [SerializeField] float sliderTravel = 0.12f;
    [Tooltip("Degrees of rotation from 0 to 1. Positive = clockwise looking down the axis.")]
    [SerializeField] float rotaryRange = 90f;
    [SerializeField] bool invertRotation;
    [SerializeField, Range(0f, 1f)] float startValue;

    [Header("Output")]
    [SerializeField] VinylAudioController audioController;
    [SerializeField] OutputTarget output = OutputTarget.FilterCutoff;
    [SerializeField] float volumeAtMin = 0.3f;
    [SerializeField] float volumeAtMax = 1f;

    [Header("Step gate")]
    [SerializeField] bool gateByStep = true;
    [SerializeField] SimStep requiredStep = SimStep.Step4_FilterKnob;
    [SerializeField, Range(0.5f, 1f)] float completeThreshold = 0.97f;
    [SerializeField] bool lockOnComplete = true;

    [Header("Events")]
    public UnityEvent<float> onValueChanged = new UnityEvent<float>();
    public UnityEvent onCompleted = new UnityEvent();

    public float Value { get; private set; }

    XRSimpleInteractable interactable;
    IXRSelectInteractor activeInteractor;
    Transform attach;
    Transform space;                       // movingPart's parent; null = world space
    Vector3 baseLocalPos;
    Quaternion baseLocalRot;
    Vector3 axisN;
    Vector3 prevDir;
    bool hasPrevDir;
    float grabOffset;
    bool completed;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (movingPart == null) movingPart = transform;
        space = movingPart.parent;
        baseLocalPos = movingPart.localPosition;
        baseLocalRot = movingPart.localRotation;
        axisN = axis.sqrMagnitude > 0f ? axis.normalized : Vector3.up;
        SetValueInternal(startValue, false);
    }

    void OnEnable()
    {
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
    }

    void OnDisable()
    {
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
        activeInteractor = null;
        attach = null;
    }

    Vector3 AxisWorld => space != null ? space.TransformDirection(axisN) : axisN;

    bool GateOpen => !gateByStep || VinylSimulationManager.IsStepActive(requiredStep);

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (activeInteractor != null || completed || !GateOpen) return;
        activeInteractor = args.interactorObject;
        attach = activeInteractor.GetAttachTransform(interactable);
        BeginDrag();
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject != activeInteractor) return;
        activeInteractor = null;
        attach = null;
    }

    void BeginDrag()
    {
        if (mode == ControlMode.Rotary)
        {
            hasPrevDir = VRDragMath.TryPlanarDirection(attach.position, movingPart.position, AxisWorld, out prevDir);
        }
        else
        {
            grabOffset = HandCoordinate() - (Vector3.Dot(baseLocalPos, axisN) + Value * sliderTravel);
        }
    }

    float HandCoordinate()
    {
        Vector3 local = space != null ? space.InverseTransformPoint(attach.position) : attach.position;
        return Vector3.Dot(local, axisN);
    }

    void Update()
    {
        if (activeInteractor == null || attach == null) return;
        if (!GateOpen)
        {
            activeInteractor = null;
            return;
        }

        if (mode == ControlMode.Rotary) UpdateRotary();
        else UpdateSlider();
    }

    void UpdateRotary()
    {
        Vector3 axisWorld = AxisWorld;
        if (!VRDragMath.TryPlanarDirection(attach.position, movingPart.position, axisWorld, out Vector3 dir))
        {
            hasPrevDir = false;
            return;
        }
        if (!hasPrevDir)
        {
            prevDir = dir;
            hasPrevDir = true;
            return;
        }

        float sign = invertRotation ? -1f : 1f;
        float delta = Vector3.SignedAngle(prevDir, dir, axisWorld) * sign;
        prevDir = dir;

        float angle = Mathf.Clamp(Value * rotaryRange + delta, 0f, rotaryRange);
        SetValueInternal(rotaryRange > 0f ? angle / rotaryRange : 0f, true);
    }

    void UpdateSlider()
    {
        float origin = Vector3.Dot(baseLocalPos, axisN);
        float along = HandCoordinate() - grabOffset - origin;
        SetValueInternal(sliderTravel > 0f ? Mathf.Clamp01(along / sliderTravel) : 0f, true);
    }

    void SetValueInternal(float normalized, bool notify)
    {
        Value = Mathf.Clamp01(normalized);

        if (mode == ControlMode.Rotary)
        {
            float sign = invertRotation ? -1f : 1f;
            movingPart.localRotation = baseLocalRot * Quaternion.AngleAxis(sign * Value * rotaryRange, axisN);
        }
        else
        {
            movingPart.localPosition = baseLocalPos + axisN * (Value * sliderTravel);
        }

        if (!notify) return;

        PushOutput();
        onValueChanged.Invoke(Value);

        if (!completed && Value >= completeThreshold && GateOpen) Complete();
    }

    void PushOutput()
    {
        if (audioController == null) return;
        switch (output)
        {
            case OutputTarget.FilterCutoff:
                audioController.SetFilterCutoff(Value);
                break;
            case OutputTarget.MasterVolume:
                audioController.SetMasterVolume(Mathf.Lerp(volumeAtMin, volumeAtMax, Value));
                break;
        }
    }

    void Complete()
    {
        completed = true;
        SetValueInternal(1f, true);                // settle exactly on the end stop (re-enters with completed = true)
        VRHaptics.Pulse(0.5f, 0.1f);
        onCompleted.Invoke();
        if (gateByStep && VinylSimulationManager.Instance != null)
            VinylSimulationManager.Instance.AdvanceStep(requiredStep);

        if (lockOnComplete)
        {
            activeInteractor = null;
            attach = null;
        }
        else
        {
            completed = false;
        }
    }

    public void ResetForSimulation()
    {
        completed = false;
        activeInteractor = null;
        attach = null;
        SetValueInternal(startValue, false);
    }
}
