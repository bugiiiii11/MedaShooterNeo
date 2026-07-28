using UnityEngine;

/// <summary>
/// Scene-independent host for the two juice systems that cannot live on a scene object:
/// the hit-stop state machine and the frame-time watchdog that demotes <see cref="JuiceSettings.Quality"/>.
///
/// It is created on demand rather than placed in develop_overhaul.unity because
/// <c>Singleton&lt;T&gt;.instance</c> is a bare static assigned in Awake with no lazy fallback --
/// every existing manager is a hand-placed GameObject, and adding one more would mean editing a
/// 62k-line scene YAML for no gain.
/// </summary>
public class JuiceRuntime : MonoBehaviour
{
    private static JuiceRuntime _instance;
    private static bool _quitting;

    public static JuiceRuntime Instance
    {
        get
        {
            if (_quitting)
                return null;

            if (_instance == null)
            {
                var go = new GameObject("[JuiceRuntime]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<JuiceRuntime>();
            }

            return _instance;
        }
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        _quitting = false;
        Application.quitting += () => _quitting = true;

        // Touch the property so the watchdog starts measuring from the first frame rather than
        // from the first kill.
        _ = Instance;
    }

    // ---------------------------------------------------------------------
    // Hit stop
    // ---------------------------------------------------------------------

    private bool _hitStopActive;
    private float _hitStopRemaining;
    private float _appliedScale;
    private float _restoreScale = 1f;
    private float _hitStopReadyAt;

    /// <summary>
    /// Seconds of SCALED time this run has lost to hit-stop. The backend receives a duration
    /// derived from scaled Time.time (RealtimeDurationChecker -> UIGameOverScreen -> BuildScore)
    /// and compares it against a server-measured duration, so every millisecond stolen here has
    /// to be handed back or a long run drifts in exactly the direction that reads as cheating.
    /// </summary>
    public static float StolenSeconds { get; private set; }

    public static void ResetStolenSeconds()
    {
        StolenSeconds = 0f;
    }

    /// <summary>
    /// Briefly slows time. Never freezes it outright: at timeScale 0 the scaled-time
    /// WaitForSeconds waits in GameEffectsPool.ReturnToPoolAfterTime and
    /// CoroutineManager.InvokeAction stop advancing entirely and pooled effects are stranded.
    /// </summary>
    public static void RequestHitStop(float seconds, float scale)
    {
        if (!JuiceSettings.HitStopEnabled || seconds <= 0f)
            return;

        var runtime = Instance;
        if (runtime != null)
            runtime.BeginHitStop(seconds, scale);
    }

    private void BeginHitStop(float seconds, float scale)
    {
        if (Time.unscaledTime < _hitStopReadyAt)
            return;

        // Never engage on top of a pause: GameManager.PauseGame owns timeScale while paused and
        // hard-assigns 0/1 with no save/restore, so we would either be clobbered or would
        // un-pause the game underneath the ESC menu.
        if (_hitStopActive || Time.timeScale <= 0.001f)
            return;

        if (GameManager.instance != null && GameManager.instance.IsGamePaused)
            return;

        _restoreScale = Time.timeScale;
        _appliedScale = Mathf.Clamp(scale, 0.02f, 0.9f) * _restoreScale;
        _hitStopRemaining = seconds;
        _hitStopActive = true;
        _hitStopReadyAt = Time.unscaledTime + seconds + JuiceSettings.HitStopCooldownSeconds;

        Time.timeScale = _appliedScale;
    }

    /// <summary>Drops the hit-stop without touching timeScale -- for when someone else took it.</summary>
    private void AbandonHitStop()
    {
        _hitStopActive = false;
        _hitStopRemaining = 0f;
    }

    private void EndHitStop()
    {
        _hitStopActive = false;
        _hitStopRemaining = 0f;
        Time.timeScale = _restoreScale;
    }

    private void TickHitStop()
    {
        if (!_hitStopActive)
            return;

        // Someone else (PauseGame, the debug console) moved timeScale out from under us. Yield
        // ownership rather than fight for it -- restoring here is what would un-pause a paused
        // game.
        if (!Mathf.Approximately(Time.timeScale, _appliedScale))
        {
            AbandonHitStop();
            return;
        }

        if (GameManager.instance != null && GameManager.instance.IsGamePaused)
        {
            EndHitStop();
            return;
        }

        var step = Time.unscaledDeltaTime;
        _hitStopRemaining -= step;

        // Scaled time advances at _appliedScale during the window, so the shortfall against real
        // elapsed time is what has to be compensated.
        StolenSeconds += step * (1f - Mathf.Clamp01(_appliedScale));

        if (_hitStopRemaining <= 0f)
            EndHitStop();
    }

    // ---------------------------------------------------------------------
    // Frame-time watchdog
    // ---------------------------------------------------------------------

    private const float SampleWindowSeconds = 0.5f;
    private const float WarmupSeconds = 4f;

    /// <summary>
    /// A frame longer than this is a load, a stall or a backgrounded tab -- not a frame-budget
    /// miss. Averaging one into a window would demote on a single sample.
    /// </summary>
    private const float StallFrameSeconds = 0.25f;

    private float _windowTime;
    private int _windowFrames;
    private float _badStreak;
    private float _startedAt = -1f;

    /// <summary>
    /// Re-arms the warm-up. Called on every scene load, because this component is
    /// DontDestroyOnLoad and bootstrapped at the FIRST scene: without this, the 4-second warm-up
    /// only ever covered the loading screen and the synchronous load of the gameplay scene was
    /// measured as though it were a gameplay frame.
    /// </summary>
    private void ResetWatchdog()
    {
        _startedAt = -1f;
        _windowTime = 0f;
        _windowFrames = 0;
        _badStreak = 0f;
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        ResetWatchdog();

        // Give each run a fresh verdict. A demotion earned by one bad stretch used to persist for
        // the entire browser session with no way back short of reloading the page.
        JuiceSettings.RearmDetectedQuality();
    }

    private void TickWatchdog()
    {
        if (JuiceSettings.Quality == JuiceQuality.Low)
            return;

        if (_startedAt < 0f)
            _startedAt = Time.unscaledTime;

        // Ignore the load spike and the first seconds of shader/texture warm-up.
        if (Time.unscaledTime - _startedAt < WarmupSeconds)
            return;

        if (Time.unscaledDeltaTime > StallFrameSeconds)
        {
            ResetWatchdog();
            return;
        }

        _windowTime += Time.unscaledDeltaTime;
        _windowFrames++;

        if (_windowTime < SampleWindowSeconds)
            return;

        var averageFrameTime = _windowTime / Mathf.Max(1, _windowFrames);
        _badStreak = averageFrameTime > JuiceSettings.DemoteFrameTime ? _badStreak + _windowTime : 0f;

        _windowTime = 0f;
        _windowFrames = 0;

        if (_badStreak < JuiceSettings.DemoteAfterSeconds)
            return;

        // Not persisted: this is a response to what this session is doing, not a user choice.
        JuiceSettings.SetQuality(JuiceQuality.Low, persist: false);
        _badStreak = 0f;
        Debug.Log("[Juice] Sustained frame time above budget -- dropping visual effects to the Low preset.");
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Update()
    {
        TickHitStop();
        TickWatchdog();
    }

    private void OnDestroy()
    {
        // Leaving a slowed timeScale behind would soft-lock the next scene.
        if (_hitStopActive)
            Time.timeScale = _restoreScale;

        if (_instance == this)
            _instance = null;
    }
}
