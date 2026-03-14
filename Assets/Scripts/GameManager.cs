using UnityEngine;

/// <summary>
/// Persistent GameManager singleton.
/// Attach this script to a GameObject named "GameManager" in your first scene.
/// It will survive scene loads and ensure only one instance ever exists.
/// </summary>
public class GameManager : MonoBehaviour
{
    // ── Singleton ────────────────────────────────────────────────────────────
    public static GameManager Instance { get; private set; }

    // ── Inspector fields ──────────────────────────────────────────────────────
    [Header("Music")]
    [Tooltip("Drag your background music clips here. They will play in order (looping the list).")]
    [SerializeField] private AudioClip[] musicTracks;

    [Range(0f, 1f)]
    [SerializeField] private float musicVolume = 0.5f;

    [Tooltip("Crossfade duration in seconds between tracks (0 = instant swap).")]
    [SerializeField] private float crossfadeDuration = 1.5f;

    // ── Private state ─────────────────────────────────────────────────────────
    private AudioSource _sourceA;
    private AudioSource _sourceB;
    private bool        _usingA   = true;   // which source is currently active
    private int         _trackIndex = 0;
    private Coroutine   _crossfadeRoutine;

    // =========================================================================
    //  Unity lifecycle
    // =========================================================================

    private void Awake()
    {
        // ── Singleton enforcement ─────────────────────────────────────────────
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);    // a duplicate crept in — kill it
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);  // survive scene transitions

        // ── Set up two AudioSources for crossfading ───────────────────────────
        _sourceA = gameObject.AddComponent<AudioSource>();
        _sourceB = gameObject.AddComponent<AudioSource>();

        ConfigureSource(_sourceA);
        ConfigureSource(_sourceB);

        // ── Start first track ─────────────────────────────────────────────────
        if (musicTracks != null && musicTracks.Length > 0)
            PlayTrack(_trackIndex);
    }

    private void Update()
    {
        // Auto-advance to the next track when the active source finishes
        AudioSource active = ActiveSource();
        if (musicTracks != null && musicTracks.Length > 0 &&
            !active.isPlaying && crossfadeDuration == 0f)
        {
            NextTrack();
        }
    }

    // =========================================================================
    //  Public API
    // =========================================================================

    /// <summary>Play a specific track by index.</summary>
    public void PlayTrack(int index)
    {
        if (musicTracks == null || musicTracks.Length == 0) return;

        _trackIndex = Mathf.Clamp(index, 0, musicTracks.Length - 1);

        if (_crossfadeRoutine != null)
            StopCoroutine(_crossfadeRoutine);

        if (crossfadeDuration > 0f)
            _crossfadeRoutine = StartCoroutine(CrossfadeTo(musicTracks[_trackIndex]));
        else
            InstantSwap(musicTracks[_trackIndex]);
    }

    /// <summary>Skip to the next track in the list.</summary>
    public void NextTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        PlayTrack((_trackIndex + 1) % musicTracks.Length);
    }

    /// <summary>Go back to the previous track.</summary>
    public void PreviousTrack()
    {
        if (musicTracks == null || musicTracks.Length == 0) return;
        PlayTrack((_trackIndex - 1 + musicTracks.Length) % musicTracks.Length);
    }

    /// <summary>Change master music volume at runtime (0–1).</summary>
    public void SetVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        ActiveSource().volume = musicVolume;
    }

    /// <summary>Pause the music.</summary>
    public void PauseMusic()  => ActiveSource().Pause();

    /// <summary>Resume the music.</summary>
    public void ResumeMusic() => ActiveSource().UnPause();

    /// <summary>Stop the music completely.</summary>
    public void StopMusic()
    {
        _sourceA.Stop();
        _sourceB.Stop();
    }

    // =========================================================================
    //  Private helpers
    // =========================================================================

    private void ConfigureSource(AudioSource src)
    {
        src.loop         = false;   // we handle looping / advancement manually
        src.playOnAwake  = false;
        src.volume       = 0f;
    }

    private AudioSource ActiveSource()  => _usingA ? _sourceA : _sourceB;
    private AudioSource InactiveSource() => _usingA ? _sourceB : _sourceA;

    private void InstantSwap(AudioClip clip)
    {
        AudioSource old = ActiveSource();
        _usingA = !_usingA;
        AudioSource fresh = ActiveSource();

        old.Stop();
        old.volume = 0f;

        fresh.clip   = clip;
        fresh.volume = musicVolume;
        fresh.Play();

        // Watch for end-of-track to auto-advance
        StartCoroutine(WaitForTrackEnd(fresh));
    }

    private System.Collections.IEnumerator CrossfadeTo(AudioClip clip)
    {
        AudioSource outgoing = ActiveSource();
        _usingA = !_usingA;
        AudioSource incoming = ActiveSource();

        incoming.clip   = clip;
        incoming.volume = 0f;
        incoming.Play();

        float elapsed = 0f;
        float startVol = outgoing.volume;

        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / crossfadeDuration;
            outgoing.volume = Mathf.Lerp(startVol, 0f,          t);
            incoming.volume = Mathf.Lerp(0f,       musicVolume,  t);
            yield return null;
        }

        outgoing.Stop();
        outgoing.volume = 0f;
        incoming.volume = musicVolume;

        _crossfadeRoutine = null;

        // Watch for end-of-track to auto-advance
        yield return WaitForTrackEnd(incoming);
    }

    private System.Collections.IEnumerator WaitForTrackEnd(AudioSource src)
    {
        // Poll until the clip is nearly done, then move to the next track
        if (src.clip == null) yield break;

        float waitTime = src.clip.length - crossfadeDuration;
        if (waitTime > 0f) yield return new WaitForSeconds(waitTime);

        // Make sure this source is still the active one (not interrupted)
        if (src == ActiveSource())
            NextTrack();
    }
}