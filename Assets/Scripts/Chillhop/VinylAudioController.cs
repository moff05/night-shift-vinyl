using System.Collections;
using UnityEngine;

/// <summary>
/// All of the session audio: platter hum, vinyl crackle, drum loop, keys loop,
/// the low-pass filter and the master level.
/// Put the <see cref="AudioLowPassFilter"/> on the same object as the AudioListener (the XR camera)
/// so it muffles the whole mix. Drum and keys loops must be the same length (or the keys an exact multiple)
/// so they stay locked together.
/// </summary>
public class VinylAudioController : MonoBehaviour
{
    [Header("Sources (all Loop = true, Play On Awake = false)")]
    [SerializeField] AudioSource humSource;       // platter motor
    [SerializeField] AudioSource needleSource;    // vinyl crackle / surface noise
    [SerializeField] AudioSource drumSource;      // lo-fi drum beat
    [SerializeField] AudioSource keysSource;      // electric piano / synth chords

    [Header("Levels")]
    [SerializeField, Range(0f, 1f)] float humLevel = 0.25f;
    [SerializeField, Range(0f, 1f)] float needleLevel = 0.5f;
    [SerializeField, Range(0f, 1f)] float drumLevel = 0.85f;
    [SerializeField, Range(0f, 1f)] float keysLevel = 0.7f;
    [SerializeField] float fadeSeconds = 2f;
    [SerializeField] float drumDelaySeconds = 1.2f;   // beat enters after the crackle

    [Header("Low-pass filter")]
    [SerializeField] AudioLowPassFilter lowPass;
    [SerializeField] float minCutoff = 800f;
    [SerializeField] float maxCutoff = 20000f;
    [SerializeField] float filterSmoothSeconds = 0.25f;

    [Header("Master volume")]
    [SerializeField, Range(0f, 1f)] float startVolume = 0.3f;
    [SerializeField] float volumeUnitsPerSecond = 1.5f;

    float cutoffNormalized;           // smoothed value actually applied
    float cutoffTarget;
    float masterTarget;
    bool needleDropped;
    bool synthStarted;

    public bool NeedleDropped => needleDropped;

    void Awake()
    {
        foreach (AudioSource s in Sources())
        {
            if (s == null) continue;
            s.loop = true;
            s.playOnAwake = false;
        }
        ApplyImmediateDefaults();
    }

    void Update()
    {
        // Filter: ease the normalized position, then map exponentially so the sweep sounds even.
        if (!Mathf.Approximately(cutoffNormalized, cutoffTarget))
        {
            float step = filterSmoothSeconds > 0f ? Time.deltaTime / filterSmoothSeconds : 1f;
            cutoffNormalized = Mathf.MoveTowards(cutoffNormalized, cutoffTarget, step);
            ApplyCutoff();
        }

        // Master level glides toward its target.
        if (!Mathf.Approximately(AudioListener.volume, masterTarget))
            AudioListener.volume = Mathf.MoveTowards(AudioListener.volume, masterTarget, volumeUnitsPerSecond * Time.deltaTime);
    }

    // ---------------------------------------------------------------- public API

    /// <summary>Platter motor hum. Call when the record lands on the platter.</summary>
    public void PlayPlatterHum()
    {
        StartLoop(humSource, humLevel, 0f);
    }

    /// <summary>Needle lands in the groove: crackle fades in, then the drum loop follows.</summary>
    public void PlayNeedleDrop()
    {
        if (needleDropped) return;
        needleDropped = true;
        StartLoop(needleSource, needleLevel, 0f);
        StartLoop(drumSource, drumLevel, drumDelaySeconds);
    }

    /// <summary>Starts the keys loop on the next drum-loop boundary so the two stay in time.</summary>
    public void TriggerSynthPad()
    {
        if (synthStarted || keysSource == null || keysSource.clip == null) return;
        synthStarted = true;
        StartCoroutine(StartKeysInSync());
    }

    /// <summary>0 = 800 Hz (muffled), 1 = 20,000 Hz (open). Interpolated exponentially.</summary>
    public void SetFilterCutoff(float normalizedValue)
    {
        cutoffTarget = Mathf.Clamp01(normalizedValue);
    }

    /// <summary>Absolute master level, 0 to 1. Glides rather than jumps.</summary>
    public void SetMasterVolume(float volume)
    {
        masterTarget = Mathf.Clamp01(volume);
    }

    /// <summary>Back to the opening mix: silent, muffled, 30% master.</summary>
    public void ResetAudio()
    {
        StopAllCoroutines();
        foreach (AudioSource s in Sources())
        {
            if (s == null) continue;
            s.Stop();
            s.volume = 0f;
        }
        ApplyImmediateDefaults();
    }

    // ---------------------------------------------------------------- internals

    AudioSource[] Sources() => new[] { humSource, needleSource, drumSource, keysSource };

    void ApplyImmediateDefaults()
    {
        needleDropped = false;
        synthStarted = false;
        cutoffNormalized = cutoffTarget = 0f;
        masterTarget = startVolume;
        AudioListener.volume = startVolume;

        foreach (AudioSource s in Sources())
            if (s != null) s.volume = 0f;

        if (lowPass != null) lowPass.enabled = true;
        ApplyCutoff();
    }

    void ApplyCutoff()
    {
        if (lowPass == null) return;
        lowPass.cutoffFrequency = minCutoff * Mathf.Pow(maxCutoff / minCutoff, cutoffNormalized);
    }

    void StartLoop(AudioSource source, float level, float delay)
    {
        if (source == null || source.clip == null) return;
        StartCoroutine(FadeInLoop(source, level, delay));
    }

    IEnumerator FadeInLoop(AudioSource source, float level, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        source.volume = 0f;
        if (!source.isPlaying) source.Play();
        yield return Fade(source, level, fadeSeconds);
    }

    IEnumerator StartKeysInSync()
    {
        keysSource.volume = 0f;

        if (drumSource != null && drumSource.isPlaying && drumSource.clip != null)
        {
            double pitch = Mathf.Max(0.01f, Mathf.Abs(drumSource.pitch));
            double remaining = (drumSource.clip.samples - drumSource.timeSamples) / (double)drumSource.clip.frequency / pitch;
            double startAt = AudioSettings.dspTime + remaining;
            keysSource.PlayScheduled(startAt);
            while (AudioSettings.dspTime < startAt) yield return null;
        }
        else
        {
            keysSource.Play();
        }

        yield return Fade(keysSource, keysLevel, fadeSeconds * 0.5f);
    }

    static IEnumerator Fade(AudioSource source, float to, float seconds)
    {
        float from = source.volume;
        float elapsed = 0f;
        while (elapsed < seconds)
        {
            elapsed += Time.deltaTime;
            source.volume = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / seconds));
            yield return null;
        }
        source.volume = to;
    }
}
