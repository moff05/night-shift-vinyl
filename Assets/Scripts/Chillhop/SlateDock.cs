using System.Collections;
using UnityEngine;

/// <summary>
/// After "SESSION MASTERED" has been read, tucks the slate down to a small dock beside the deck so the
/// window and lounge are unobstructed. RESTART SESSION stays reachable on the dock.
/// </summary>
public class SlateDock : MonoBehaviour
{
    public Transform slate;
    public Vector3 dockPosition = new Vector3(0.88f, 1.40f, 0.78f);
    public float dockScale = 0.72f, waitSeconds = 7f, moveSeconds = 1.6f;
    Vector3 homeScale = Vector3.one;
    Quaternion homeRot = Quaternion.identity;
    Coroutine routine;

    void OnEnable()
    {
        if (slate != null) { homeScale = slate.localScale; homeRot = slate.rotation; }
        StartCoroutine(Hook());
    }

    IEnumerator Hook()
    {
        while (VinylSimulationManager.Instance == null) yield return null;
        VinylSimulationManager.Instance.StepChanged += OnStep;
    }

    void OnDisable()
    {
        if (VinylSimulationManager.Instance != null) VinylSimulationManager.Instance.StepChanged -= OnStep;
    }

    void OnStep(SimStep step)
    {
        if (routine != null) { StopCoroutine(routine); routine = null; }
        if (step == SimStep.Complete) routine = StartCoroutine(Dock());
        else if (slate != null) { slate.localScale = homeScale; slate.rotation = homeRot; }
    }

    IEnumerator Dock()
    {
        yield return new WaitForSeconds(waitSeconds);
        Vector3 p0 = slate.position, s0 = slate.localScale;
        Quaternion r0 = slate.rotation;
        Vector3 head = Camera.main != null ? Camera.main.transform.position : new Vector3(0, 1.5f, 0);
        Quaternion r1 = Quaternion.LookRotation(dockPosition - head, Vector3.up);
        for (float t = 0f; t < moveSeconds; t += Time.deltaTime)
        {
            float k = Mathf.SmoothStep(0f, 1f, t / moveSeconds);
            slate.position = Vector3.Lerp(p0, dockPosition, k);
            slate.rotation = Quaternion.Slerp(r0, r1, k);
            slate.localScale = Vector3.Lerp(s0, homeScale * dockScale, k);
            yield return null;
        }
        routine = null;
    }
}
