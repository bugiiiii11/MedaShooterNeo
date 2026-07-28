using UnityEngine;

/// <summary>
/// Screen shake (GDD 3.2 item 2).
///
/// This component was already serialized onto the Main Camera in develop_overhaul.unity, so
/// Phase 1 rewrites it in place rather than adding anything -- no scene edit, and the one
/// existing caller (PlayerMovement, on the player surviving damage) keeps compiling against the
/// single-argument overload.
///
/// Shaking the camera transform is safe here specifically because the camera is decoupled from
/// everything: it has no children, every Canvas in the scene is Screen Space Overlay with a null
/// camera reference, and the world scrolls past a static camera rather than the camera moving
/// through the world (BackgroundResolver and ScrollingObject both move objects in world space).
/// The one exception is handled by <see cref="CurrentOffset"/> -- see the note there.
///
/// What changed in S202:
///  - amplitude is now a per-request argument with a hard cap, instead of one global value;
///  - the offset is 2D and biased toward X (parallax planes tile infinitely in X but are single
///    fixed-height sprites, so vertical shake is the axis that can expose a sprite edge);
///  - decay is unscaled and a pause kills the shake. Previously decay used scaled time while
///    GameManager.PauseGame sets timeScale to 0, so pausing mid-shake left the camera
///    re-randomising its position forever with zero decay. The old PerkSelectionEvent guard
///    covered only one of roughly eight pause paths;
///  - the idle path no longer writes the transform every frame.
/// </summary>
public class CameraShake : Singleton<CameraShake>
{
    // Transform of the camera to shake. Grabs the gameObject's transform if null.
    public Transform camTransform;

    // How long the object should shake for.
    public float shakeDuration = 0f;

    // Fallback amplitude for callers that do not pass one. Serialized to 0.3 in
    // develop_overhaul.unity -- the scene value wins over this initializer.
    public float shakeAmount = 0.7f;
    public float decreaseFactor = 1.0f;

    /// <summary>Vertical shake as a fraction of horizontal. See the class note.</summary>
    private const float VerticalBias = 0.5f;

    private Vector3 originalPos;
    private Camera _camera;
    private float _amplitude;
    private bool _atRest = true;

    /// <summary>
    /// Displacement applied to the camera this frame, or zero at rest.
    ///
    /// BackgroundResolver.CreateDecals reads Camera.main.ScreenToWorldPoint mid-gameplay and
    /// bakes the resulting Y into a decal that is then parented to a scrolling plane -- so
    /// without subtracting this, a decal that happens to spawn during a shake is permanently
    /// misplaced. That is the only path by which camera shake can leak into world state, and it
    /// was already subtly wrong at the old 0.3 amplitude.
    /// </summary>
    public Vector3 CurrentOffset { get; private set; }

    public override void Awake()
    {
        base.Awake();

        if (camTransform == null)
        {
            camTransform = transform;
        }

        _camera = GetComponent<Camera>();

        // Captured here rather than in OnEnable: OnEnable re-captures on every enable, so a
        // component toggled while shaking would adopt a shaken pose as its rest position and the
        // camera would stay permanently offset.
        originalPos = camTransform.localPosition;
        _amplitude = shakeAmount;

        if (GameManager.instance == null || GameManager.instance.EventManager == null)
            return;

        GameManager.instance.EventManager.AddListener<PerkSelectionEvent>(OnDisplayPerkSelection);
        GameManager.instance.EventManager.AddListener<GamePauseEvent>(OnGamePaused);
    }

    private void OnDisplayPerkSelection(PerkSelectionEvent obj)
    {
        StopShake();
    }

    private void OnGamePaused(GamePauseEvent obj)
    {
        if (obj.IsPaused)
            StopShake();
    }

    public void SetShake(float duration)
    {
        SetShake(duration, shakeAmount);
    }

    /// <summary>
    /// Requests a shake. Overlapping requests take the strongest of each value rather than
    /// summing, so a burst of kills cannot stack into something that throws the play area off
    /// screen.
    /// </summary>
    public void SetShake(float duration, float amplitude)
    {
        if (!JuiceSettings.ShakeEnabled)
            return;

        // A zero-amplitude request is a disabled shake, not a short one, so it must not touch
        // shakeDuration either -- Mathf.Max below would otherwise EXTEND a shake already in
        // flight at its own amplitude, letting a silenced source prolong a loud one. This is what
        // makes JuiceSettings.ShakeKillAmount = 0 a genuine no-op rather than a zero-length write.
        if (amplitude <= 0f)
            return;

        // Only take the max against an amplitude that is still in flight. Carrying the previous
        // value across an idle gap would mean one big shake permanently raised the floor for
        // every small one after it.
        var alreadyShaking = shakeDuration > 0f;
        _amplitude = alreadyShaking ? Mathf.Max(_amplitude, amplitude) : amplitude;
        shakeDuration = Mathf.Max(shakeDuration, duration);
    }

    /// <summary>Convenience for the many gameplay call sites, which run where instance may be null.</summary>
    public static void Shake(float duration, float amplitude)
    {
        if (instance != null)
            instance.SetShake(duration, amplitude);
    }

    private void StopShake()
    {
        shakeDuration = 0f;
        SnapToRest();
    }

    private void SnapToRest()
    {
        if (_atRest)
            return;

        camTransform.localPosition = originalPos;
        CurrentOffset = Vector3.zero;
        _atRest = true;
    }

    /// <summary>
    /// Caps amplitude both absolutely and as a fraction of the visible frame. MatchWidth
    /// recomputes orthographicSize from the live aspect every frame, so a viewer on a wider
    /// window sees a shorter world and a fixed world-unit cap would eat more of the margin than
    /// intended.
    /// </summary>
    private float ResolveAmplitude()
    {
        var cap = JuiceSettings.ShakeMaxUnits;

        if (_camera != null && _camera.orthographic)
            cap = Mathf.Min(cap, _camera.orthographicSize * 0.05f);

        return Mathf.Clamp(_amplitude, 0f, cap);
    }

    private void Update()
    {
        if (shakeDuration <= 0f)
        {
            shakeDuration = 0f;
            SnapToRest();
            return;
        }

        if (!JuiceSettings.ShakeEnabled)
        {
            StopShake();
            return;
        }

        var amplitude = ResolveAmplitude();

        // insideUnitCircle is a struct, so the active path stays allocation-free. The old code
        // used insideUnitSphere, whose Z component does nothing on an orthographic camera except
        // risk clipping sprites against the near plane.
        var random = Random.insideUnitCircle;
        var offset = new Vector3(random.x * amplitude, random.y * amplitude * VerticalBias, 0f);

        CurrentOffset = offset;
        camTransform.localPosition = originalPos + offset;
        _atRest = false;

        // Unscaled: GameManager.PauseGame drives timeScale to 0, and a scaled decay there never
        // reaches zero.
        shakeDuration -= Time.unscaledDeltaTime * decreaseFactor;

        if (shakeDuration <= 0f)
        {
            shakeDuration = 0f;
            SnapToRest();
        }
    }
}
