using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

/// <summary>
/// Hinged tonearm. Pinch the handle and swing the arm around its pivot; when the needle is over the groove
/// it snaps down into contact. Let go early and it glides back to its rest. Only works once the record is on the platter.
/// Put this on the handle (it needs an XRSimpleInteractable) and point <see cref="arm"/> at the pivot object.
/// </summary>
[RequireComponent(typeof(XRSimpleInteractable))]
public class VRTonearm : MonoBehaviour, ISimResettable
{
    [Header("Hinge")]
    [Tooltip("Pivot object that rotates. Its origin must sit on the hinge.")]
    [SerializeField] Transform arm;
    [Tooltip("Hinge axis in the arm's PARENT space.")]
    [SerializeField] Vector3 axis = Vector3.up;
    [SerializeField] float restAngle = 0f;
    [Tooltip("Angle where the needle sits in the groove (degrees from rest, positive = clockwise from above).")]
    [SerializeField] float playAngle = 32f;
    [Tooltip("Within this many degrees of Play Angle the arm snaps in, even if still held.")]
    [SerializeField] float snapTolerance = 5f;
    [SerializeField] float snapSeconds = 0.3f;
    [SerializeField] float returnSeconds = 0.6f;

    [Header("Simulation")]
    [SerializeField] SimStep expectedStep = SimStep.Step2_DropNeedle;
    public UnityEvent onNeedleDropped = new UnityEvent();

    XRSimpleInteractable interactable;
    IXRSelectInteractor activeInteractor;
    Transform attach;
    Quaternion baseLocalRot;
    Vector3 axisN;
    Vector3 prevDir;
    bool hasPrevDir;
    float angle;
    bool dropped;
    Coroutine settleRoutine;

    void Awake()
    {
        interactable = GetComponent<XRSimpleInteractable>();
        if (arm == null) arm = transform.parent != null ? transform.parent : transform;
        baseLocalRot = arm.localRotation;
        axisN = axis.sqrMagnitude > 0f ? axis.normalized : Vector3.up;
        angle = restAngle;
        Apply();
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

    Vector3 AxisWorld => (arm.parent != null ? arm.parent : arm).TransformDirection(axisN);

    void OnSelectEntered(SelectEnterEventArgs args)
    {
        if (activeInteractor != null || dropped || !VinylSimulationManager.IsStepActive(expectedStep)) return;

        if (settleRoutine != null)
        {
            StopCoroutine(settleRoutine);
            settleRoutine = null;
        }
        activeInteractor = args.interactorObject;
        attach = activeInteractor.GetAttachTransform(interactable);
        hasPrevDir = VRDragMath.TryPlanarDirection(attach.position, arm.position, AxisWorld, out prevDir);
    }

    void OnSelectExited(SelectExitEventArgs args)
    {
        if (args.interactorObject != activeInteractor) return;
        activeInteractor = null;
        attach = null;
        if (!dropped) StartSettle(restAngle, returnSeconds, false);   // let go early: swing back to rest
    }

    void Update()
    {
        if (activeInteractor == null || attach == null || dropped) return;

        if (!VRDragMath.TryPlanarDirection(attach.position, arm.position, AxisWorld, out Vector3 dir))
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

        float delta = Vector3.SignedAngle(prevDir, dir, AxisWorld);
        prevDir = dir;

        float lo = Mathf.Min(restAngle, playAngle);
        float hi = Mathf.Max(restAngle, playAngle);
        angle = Mathf.Clamp(angle + delta, lo, hi);
        Apply();

        if (Mathf.Abs(angle - playAngle) <= snapTolerance) DropNeedle();
    }

    void DropNeedle()
    {
        dropped = true;
        activeInteractor = null;
        attach = null;
        StartSettle(playAngle, snapSeconds, true);
    }

    void StartSettle(float target, float seconds, bool isDrop)
    {
        if (settleRoutine != null) StopCoroutine(settleRoutine);
        settleRoutine = StartCoroutine(Settle(target, seconds, isDrop));
    }

    IEnumerator Settle(float target, float seconds, bool isDrop)
    {
        float from = angle;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            angle = Mathf.Lerp(from, target, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / seconds)));
            Apply();
            yield return null;
        }
        angle = target;
        Apply();
        settleRoutine = null;

        if (!isDrop) yield break;
        VRHaptics.Pulse(0.5f, 0.08f);
        onNeedleDropped.Invoke();
        if (VinylSimulationManager.Instance != null) VinylSimulationManager.Instance.AdvanceStep(expectedStep);
    }

    void Apply()
    {
        arm.localRotation = baseLocalRot * Quaternion.AngleAxis(angle, axisN);
    }

    public void ResetForSimulation()
    {
        if (settleRoutine != null)
        {
            StopCoroutine(settleRoutine);
            settleRoutine = null;
        }
        activeInteractor = null;
        attach = null;
        dropped = false;
        angle = restAngle;
        Apply();
    }
}
