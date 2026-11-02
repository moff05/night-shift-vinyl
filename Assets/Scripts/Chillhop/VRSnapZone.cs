using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// Trigger volume that captures a released grabbable (the vinyl record).
/// Put it on a trigger collider over the platter. It only accepts the object while
/// <see cref="expectedStep"/> is the active step, then freezes it, glides it onto
/// <see cref="snapPoint"/>, fires <see cref="onSnapped"/> and advances the simulation.
/// </summary>
[RequireComponent(typeof(Collider))]
public class VRSnapZone : MonoBehaviour, ISimResettable
{
    [Header("What it accepts")]
    [Tooltip("Tag of the grabbable. Leave empty to rely on Target Object only.")]
    [SerializeField] string targetTag = "Vinyl";
    [Tooltip("Optional: exact object (or parent of it) that is allowed. Wins over the tag.")]
    [SerializeField] GameObject targetObject;

    [Header("Snap")]
    [SerializeField] Transform snapPoint;
    [SerializeField] float snapSeconds = 0.35f;
    [Tooltip("Parent the object to Snap Point so it spins with the platter.")]
    [SerializeField] bool parentToSnapPoint = true;

    [Header("Simulation")]
    [SerializeField] SimStep expectedStep = SimStep.Step1_PlaceVinyl;
    public UnityEvent onSnapped = new UnityEvent();

    class Home
    {
        public Transform parent;
        public Vector3 localPos;
        public Quaternion localRot;
        public bool wasKinematic;
        public bool grabWasEnabled;
        public XRGrabInteractable grab;
    }

    readonly Dictionary<Rigidbody, Home> homes = new Dictionary<Rigidbody, Home>();
    readonly HashSet<Rigidbody> inside = new HashSet<Rigidbody>();
    readonly List<Rigidbody> scratch = new List<Rigidbody>();
    Coroutine snapRoutine;
    bool busy;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void Start()
    {
        CacheHomePoses();
    }

    // Remember where the record lives so RESTART can put it back in the sleeve.
    void CacheHomePoses()
    {
        if (targetObject != null)
        {
            Register(targetObject.GetComponentInChildren<Rigidbody>() ?? targetObject.GetComponentInParent<Rigidbody>());
        }
        else if (!string.IsNullOrEmpty(targetTag))
        {
            try
            {
                foreach (GameObject go in GameObject.FindGameObjectsWithTag(targetTag))
                    Register(go.GetComponent<Rigidbody>());
            }
            catch (UnityException)
            {
                Debug.LogWarning($"[VRSnapZone] Tag '{targetTag}' is not defined (Project Settings > Tags and Layers).", this);
            }
        }
    }

    void Register(Rigidbody rb)
    {
        if (rb == null || homes.ContainsKey(rb)) return;
        var grab = rb.GetComponent<XRGrabInteractable>();
        homes[rb] = new Home
        {
            parent = rb.transform.parent,
            localPos = rb.transform.localPosition,
            localRot = rb.transform.localRotation,
            wasKinematic = rb.isKinematic,
            grab = grab,
            grabWasEnabled = grab != null && grab.enabled
        };
    }

    // ---------------------------------------------------------------- detection

    // Candidates are tracked on enter/exit and polled in Update, because OnTriggerStay
    // stops firing once a resting Rigidbody falls asleep.
    void OnTriggerEnter(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null && IsValid(other, rb)) inside.Add(rb);
    }

    void OnTriggerExit(Collider other)
    {
        Rigidbody rb = other.attachedRigidbody;
        if (rb != null) inside.Remove(rb);
    }

    bool IsValid(Collider other, Rigidbody rb)
    {
        if (targetObject != null)
            return rb.gameObject == targetObject || rb.transform.IsChildOf(targetObject.transform);

        if (string.IsNullOrEmpty(targetTag)) return false;
        return other.tag == targetTag || rb.tag == targetTag;
    }

    void Update()
    {
        if (busy || inside.Count == 0 || !VinylSimulationManager.IsStepActive(expectedStep)) return;

        scratch.Clear();
        scratch.AddRange(inside);
        foreach (Rigidbody rb in scratch)
        {
            if (rb == null)
            {
                inside.Remove(rb);
                continue;
            }

            // Wait for the hand to let go so the record is dropped, not dragged through.
            var grab = rb.GetComponent<XRGrabInteractable>();
            if (grab != null && grab.isSelected) continue;

            Register(rb);
            snapRoutine = StartCoroutine(SnapRoutine(rb, grab));
            return;
        }
    }

    // ---------------------------------------------------------------- snapping

    IEnumerator SnapRoutine(Rigidbody rb, XRGrabInteractable grab)
    {
        busy = true;
        inside.Remove(rb);

        // Order matters: disable the grab first (XRI restores Rigidbody settings when it lets go),
        // then freeze physics.
        if (grab != null) grab.enabled = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;

        Transform t = rb.transform;
        Transform target = snapPoint != null ? snapPoint : transform;
        if (parentToSnapPoint) t.SetParent(target, true);

        Vector3 fromPos = parentToSnapPoint ? t.localPosition : t.position;
        Quaternion fromRot = parentToSnapPoint ? t.localRotation : t.rotation;
        Vector3 toPos = parentToSnapPoint ? Vector3.zero : target.position;
        Quaternion toRot = parentToSnapPoint ? Quaternion.identity : target.rotation;

        float elapsed = 0f;
        while (elapsed < snapSeconds)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / snapSeconds));
            if (parentToSnapPoint)
            {
                t.localPosition = Vector3.Lerp(fromPos, toPos, k);
                t.localRotation = Quaternion.Slerp(fromRot, toRot, k);
            }
            else
            {
                t.SetPositionAndRotation(Vector3.Lerp(fromPos, toPos, k), Quaternion.Slerp(fromRot, toRot, k));
            }
            yield return null;
        }

        if (parentToSnapPoint) t.SetLocalPositionAndRotation(toPos, toRot);
        else t.SetPositionAndRotation(toPos, toRot);

        snapRoutine = null;
        onSnapped.Invoke();
        if (VinylSimulationManager.Instance != null) VinylSimulationManager.Instance.AdvanceStep(expectedStep);
        // busy stays true until reset: the record is locked in.
    }

    // ---------------------------------------------------------------- reset

    public void ResetForSimulation()
    {
        if (snapRoutine != null)
        {
            StopCoroutine(snapRoutine);
            snapRoutine = null;
        }
        busy = false;
        inside.Clear();

        foreach (KeyValuePair<Rigidbody, Home> pair in homes)
        {
            Rigidbody rb = pair.Key;
            Home h = pair.Value;
            if (rb == null) continue;

            rb.isKinematic = false;                      // zero velocity only works on a dynamic body
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.transform.SetParent(h.parent, false);
            rb.transform.SetLocalPositionAndRotation(h.localPos, h.localRot);
            rb.isKinematic = h.wasKinematic;
            if (h.grab != null) h.grab.enabled = h.grabWasEnabled;
        }
    }
}
