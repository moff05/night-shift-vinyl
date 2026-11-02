using System;
using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

/// <summary>The ordered stages of the Chillhop Crate session.</summary>
public enum SimStep
{
    MenuStart,
    Step1_PlaceVinyl,
    Step2_DropNeedle,
    Step3_TriggerSamplePad,
    Step4_FilterKnob,
    Step5_MasterFader,
    Complete
}

/// <summary>Implemented by anything that must return to its starting state on RESTART SESSION.</summary>
public interface ISimResettable
{
    void ResetForSimulation();
}

/// <summary>
/// Singleton state machine for the whole session. Owns the checklist text, the step chime,
/// the conclusion lighting and the completion slate, and resets everything on restart.
/// </summary>
public class VinylSimulationManager : MonoBehaviour
{
    public static VinylSimulationManager Instance { get; private set; }

    /// <summary>Fires after every step change (new step passed in).</summary>
    public event Action<SimStep> StepChanged;

    public SimStep CurrentStep { get; private set; } = SimStep.MenuStart;
    public float SessionSeconds { get; private set; }

    [Header("World-Space UI")]
    [SerializeField] GameObject introRoot;          // welcome text + START SESSION button
    [SerializeField] GameObject checklistRoot;      // panel that holds the checklist text
    [SerializeField] TMP_Text checklistText;
    [SerializeField] GameObject completeRoot;       // "SESSION MASTERED" slate + RESTART button
    [SerializeField] TMP_Text completionTimeText;
    [SerializeField] float slateDropHeight = 0.8f;
    [SerializeField] float slateDropSeconds = 1.2f;

    [Header("Checklist")]
    [SerializeField]
    string[] stepLabels =
    {
        "Load Vinyl Platter",
        "Drop the Stylus Needle",
        "Trigger Beat Sample Pad",
        "Open Low-Pass Filter",
        "Push Master Output Fader"
    };
    [SerializeField] Color currentStepColor = new Color(1f, 0.69f, 0.2f);

    [Header("Audio")]
    [SerializeField] AudioSource chimeSource;       // enable "Bypass Listener Effects" so the low-pass never muffles it
    [SerializeField] AudioClip stepChime;
    [SerializeField] AudioClip completeChime;
    [SerializeField] VinylAudioController audioController;

    [Header("Conclusion Lighting")]
    [SerializeField] Light deskLamp;
    [SerializeField] float lampDimIntensity = 0.15f;
    [SerializeField] Color lampDimColor = new Color(1f, 0.55f, 0.25f);
    [SerializeField] Light[] neonLights;
    [SerializeField] Color[] neonColors =
    {
        new Color(1f, 0.2f, 0.7f), new Color(0.2f, 0.9f, 1f), new Color(0.7f, 0.3f, 1f)
    };
    [SerializeField] float neonIdleIntensity = 0.2f;
    [SerializeField] float neonPulseIntensity = 2.5f;
    [SerializeField] float beatsPerMinute = 80f;    // match the drum loop tempo
    [SerializeField] float lightingTransitionSeconds = 3f;

    float startTime;
    float lightBlend;                               // 0 = gameplay lighting, 1 = conclusion lighting
    float lampBaseIntensity;
    Color lampBaseColor;
    Vector3 slateHomeLocalPos;
    Coroutine slateRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (deskLamp != null)
        {
            lampBaseIntensity = deskLamp.intensity;
            lampBaseColor = deskLamp.color;
        }
        if (completeRoot != null) slateHomeLocalPos = completeRoot.transform.localPosition;
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        ApplyStepToUI();
        ApplyLighting();
    }

    void Update()
    {
        float target = CurrentStep == SimStep.Complete ? 1f : 0f;
        if (!Mathf.Approximately(lightBlend, target) || lightBlend > 0f)
        {
            float step = lightingTransitionSeconds > 0f ? Time.deltaTime / lightingTransitionSeconds : 1f;
            lightBlend = Mathf.MoveTowards(lightBlend, target, step);
            ApplyLighting();
        }
    }

    /// <summary>True when <paramref name="step"/> is the active step, or when no manager exists (bench testing).</summary>
    public static bool IsStepActive(SimStep step)
    {
        return Instance == null || Instance.CurrentStep == step;
    }

    // ---------------------------------------------------------------- step flow

    /// <summary>Wire to the START SESSION poke button's onPressed event.</summary>
    public void StartSession()
    {
        if (CurrentStep != SimStep.MenuStart) return;
        startTime = Time.time;
        AdvanceStep(SimStep.MenuStart);
    }

    /// <summary>Wire to the MPC pad poke button's onPressed event.</summary>
    public void OnSamplePadPressed()
    {
        AdvanceStep(SimStep.Step3_TriggerSamplePad);
    }

    /// <summary>
    /// Completes <paramref name="expected"/> and moves to the next step. Ignored (returns false)
    /// unless <paramref name="expected"/> is the current step, so interactions can't be done out of order.
    /// </summary>
    public bool AdvanceStep(SimStep expected)
    {
        if (CurrentStep != expected || expected == SimStep.Complete) return false;

        SimStep next = expected + 1;
        CurrentStep = next;

        if (next == SimStep.Complete)
        {
            SessionSeconds = Time.time - startTime;
            PlayClip(completeChime != null ? completeChime : stepChime);
            ShowCompletionSlate();
        }
        else
        {
            PlayClip(stepChime);
        }

        ApplyStepToUI();
        StepChanged?.Invoke(CurrentStep);
        return true;
    }

    // ---------------------------------------------------------------- reset

    /// <summary>Wire to the RESTART SESSION poke button. Returns the whole scene to its first frame.</summary>
    public void ResetSimulation()
    {
        if (slateRoutine != null)
        {
            StopCoroutine(slateRoutine);
            slateRoutine = null;
        }

        CurrentStep = SimStep.MenuStart;
        SessionSeconds = 0f;

        if (completeRoot != null)
        {
            completeRoot.SetActive(false);
            completeRoot.transform.localPosition = slateHomeLocalPos;
        }

        if (audioController != null) audioController.ResetAudio();

        foreach (MonoBehaviour mb in FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include))
        {
            if (mb is ISimResettable resettable) resettable.ResetForSimulation();
        }

        ApplyStepToUI();
        StepChanged?.Invoke(CurrentStep);
    }

    // ---------------------------------------------------------------- UI

    void ApplyStepToUI()
    {
        if (introRoot != null) introRoot.SetActive(CurrentStep == SimStep.MenuStart);
        if (checklistRoot != null) checklistRoot.SetActive(true);
        if (checklistText != null) checklistText.text = BuildChecklist();
    }

    string BuildChecklist()
    {
        var sb = new StringBuilder();
        for (int i = 0; i < stepLabels.Length; i++)
        {
            int stepNumber = i + 1;                       // Step1_PlaceVinyl == 1
            bool done = (int)CurrentStep > stepNumber;
            bool current = (int)CurrentStep == stepNumber;

            if (done)
            {
                sb.Append("<color=green>[X]</color> ");
                sb.Append(stepNumber).Append(". ").Append(stepLabels[i]);
            }
            else if (current)
            {
                sb.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(currentStepColor)).Append(">[ ] ");
                sb.Append(stepNumber).Append(". ").Append(stepLabels[i]).Append("</color>");
            }
            else
            {
                sb.Append("[ ] ").Append(stepNumber).Append(". ").Append(stepLabels[i]);
            }
            if (i < stepLabels.Length - 1) sb.Append('\n');
        }
        return sb.ToString();
    }

    void ShowCompletionSlate()
    {
        if (completionTimeText != null)
        {
            int minutes = (int)(SessionSeconds / 60f);
            float seconds = SessionSeconds - minutes * 60f;
            completionTimeText.text = $"Session time {minutes:00}:{seconds:00.0}";
        }
        if (completeRoot == null) return;

        completeRoot.SetActive(true);
        if (slateRoutine != null) StopCoroutine(slateRoutine);
        slateRoutine = StartCoroutine(DropSlate());
    }

    IEnumerator DropSlate()
    {
        Transform t = completeRoot.transform;
        Vector3 from = slateHomeLocalPos + Vector3.up * slateDropHeight;
        t.localPosition = from;

        float elapsed = 0f;
        while (elapsed < slateDropSeconds)
        {
            elapsed += Time.deltaTime;
            float k = Mathf.Clamp01(elapsed / slateDropSeconds);
            float eased = 1f - Mathf.Pow(1f - k, 3f);                      // ease-out cubic
            float settle = Mathf.Sin(k * Mathf.PI) * 0.06f * (1f - k);     // tiny overshoot
            t.localPosition = Vector3.LerpUnclamped(from, slateHomeLocalPos, eased + settle);
            yield return null;
        }
        t.localPosition = slateHomeLocalPos;
        slateRoutine = null;
    }

    // ---------------------------------------------------------------- lighting

    void ApplyLighting()
    {
        if (deskLamp != null)
        {
            deskLamp.intensity = Mathf.Lerp(lampBaseIntensity, lampDimIntensity, lightBlend);
            deskLamp.color = Color.Lerp(lampBaseColor, lampDimColor, lightBlend);
        }

        if (neonLights == null || neonLights.Length == 0) return;

        // Smooth, beat-locked pulse: peaks once per beat.
        float beatPhase = Time.time * beatsPerMinute / 60f;
        float pulse = 0.5f + 0.5f * Mathf.Cos(beatPhase * Mathf.PI * 2f);
        pulse *= pulse;

        for (int i = 0; i < neonLights.Length; i++)
        {
            Light neon = neonLights[i];
            if (neon == null) continue;

            neon.intensity = neonIdleIntensity + lightBlend * neonPulseIntensity * pulse;
            if (neonColors.Length > 0)
            {
                float colorPos = Time.time * 0.2f + i * 0.5f;
                int a = Mathf.FloorToInt(colorPos) % neonColors.Length;
                int b = (a + 1) % neonColors.Length;
                neon.color = Color.Lerp(neonColors[a], neonColors[b], colorPos - Mathf.Floor(colorPos));
            }
        }
    }

    void PlayClip(AudioClip clip)
    {
        if (chimeSource != null && clip != null) chimeSource.PlayOneShot(clip);
    }
}
