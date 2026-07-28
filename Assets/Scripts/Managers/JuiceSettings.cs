using UnityEngine;

/// <summary>
/// Visual-feedback quality tier. Deliberately only two rungs: the project has exactly one
/// Unity quality level ("Fantastic", ProjectSettings/QualitySettings.asset), so
/// QualitySettings.SetQualityLevel can never do anything and a real ladder would mean editing
/// project settings that every platform shares. This tier is code-side only.
/// </summary>
public enum JuiceQuality
{
    Low = 0,
    High = 1
}

/// <summary>
/// Tunables and on/off switches for the MS 2.0 juice pass (GDD 3.2).
///
/// Why this is a plain static class and NOT GameConstants:
///  - GameConstants is serialized inline on the GameManager instance inside
///    develop_overhaul.unity, so a newly added field's value comes from the scene YAML, not from
///    the C# initializer. CameraShake is the cautionary tale -- its code default is 0.7f but the
///    scene serializes 0.3, and the scene wins. Values here cannot be silently overridden.
///  - Every GameConstants field is a CodeStage ObscuredX that decrypts on every read. Juice
///    values have no cheat value and are read in Update, so they must stay plain floats.
///
/// Toggles follow the only settings precedent in the project (UISettings): lowerCamelCase key,
/// "Enabled" suffix, int 0/1 in PlayerPrefs.
/// </summary>
public static class JuiceSettings
{
    // ---------------------------------------------------------------------
    // Persisted toggles
    // ---------------------------------------------------------------------

    public const string ShakeKey = "shakeEnabled";
    public const string HitStopKey = "hitStopEnabled";
    public const string HitFlashKey = "hitFlashEnabled";
    public const string KillVfxKey = "killVfxEnabled";
    public const string MuzzleFlashKey = "muzzleFlashEnabled";
    public const string ScreenFxKey = "screenFxEnabled";
    public const string QualityKey = "juiceQuality";

    public static bool ShakeEnabled = true;
    public static bool HitStopEnabled = true;
    public static bool HitFlashEnabled = true;
    public static bool KillVfxEnabled = true;
    public static bool MuzzleFlashEnabled = true;
    public static bool ScreenFxEnabled = true;

    /// <summary>Auto-detected at boot, demotable at runtime by the frame-time watchdog.</summary>
    public static JuiceQuality Quality = JuiceQuality.High;

    /// <summary>
    /// What device detection decided at boot, before any runtime demotion. Kept so the watchdog's
    /// verdict can be re-armed per run instead of sticking for the whole browser session -- one
    /// bad stretch should not disable the juice pass until the player reloads the page.
    /// </summary>
    public static JuiceQuality DetectedQuality = JuiceQuality.High;

    /// <summary>True when the tier came from detection rather than an explicit player choice.</summary>
    public static bool QualityIsAutomatic = true;

    public static bool IsHigh => Quality == JuiceQuality.High;

    // ---------------------------------------------------------------------
    // Hit flash (Juice 1)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Enemies render with Trooper.mat on the built-in Sprites/Default shader, so
    /// SpriteRenderer.color MULTIPLIES -- it can tint and darken but can never brighten to
    /// white. A hot red-orange is the strongest impact read available without a custom shader
    /// plus an Always-Included-Shaders project edit.
    /// </summary>
    public static readonly Color HitFlashColor = new Color(1f, 0.35f, 0.24f, 1f);

    public const float HitFlashInSeconds = 0.035f;
    public const float HitFlashOutSeconds = 0.11f;

    // ---------------------------------------------------------------------
    // Hit stop (Juice 1)
    // ---------------------------------------------------------------------

    /// <summary>
    /// Never 0. At exactly 0 the scaled-time WaitForSeconds in GameEffectsPool and
    /// CoroutineManager stall outright and pooled effects never come back; at 0.08 they merely
    /// crawl. Also keeps the stolen-time compensation small.
    /// </summary>
    public const float HitStopScale = 0.08f;

    public const float HitStopKillSeconds = 0.055f;
    public const float HitStopCritSeconds = 0.04f;
    public const float HitStopBossKillSeconds = 0.12f;

    /// <summary>Floor between two hit-stops so a killing spree cannot chain them into a stutter.</summary>
    public const float HitStopCooldownSeconds = 0.18f;

    // ---------------------------------------------------------------------
    // Camera shake (Juice 2)
    // ---------------------------------------------------------------------

    public const float ShakeKillAmount = 0.055f;
    public const float ShakeKillDuration = 0.09f;
    public const float ShakeBossKillAmount = 0.22f;
    public const float ShakeBossKillDuration = 0.32f;
    public const float ShakeExplosionAmount = 0.16f;
    public const float ShakeExplosionDuration = 0.22f;
    public const float ShakeAbilityAmount = 0.19f;
    public const float ShakeAbilityDuration = 0.26f;

    /// <summary>
    /// Hard ceiling in world units. The tightest measured margin between the play area and the
    /// screen edge is 2.36 units (BoundBottom world Y -4.19 vs bottom edge -6.55), so this uses
    /// under 15% of it. CameraShake additionally clamps against the live orthographic size,
    /// because MatchWidth recomputes that every frame from the viewer's aspect.
    /// </summary>
    public const float ShakeMaxUnits = 0.35f;

    // ---------------------------------------------------------------------
    // Kill VFX (Juice 3)
    // ---------------------------------------------------------------------

    public const float KillBurstSeconds = 1.1f;

    // ---------------------------------------------------------------------
    // Screen FX overlay (Juice 4)
    // ---------------------------------------------------------------------

    public const float VignetteAlpha = 0.30f;
    public const float DamageTintAlpha = 0.30f;
    public const float DamageTintSeconds = 0.30f;
    public static readonly Color DamageTintColor = new Color(0.85f, 0.06f, 0.10f, 1f);

    // ---------------------------------------------------------------------
    // Muzzle flash + trails (Juice 5)
    // ---------------------------------------------------------------------

    public const float MuzzleFlashSeconds = 0.32f;

    /// <summary>
    /// Cap on a single projectile's lifetime. Weapon.SpawnMultiProjectileDelayed has always
    /// computed a lifetime from Weapon.ProjectileLifeTime and then thrown the value away, so a
    /// projectile that misses lives forever -- with a simulating Rigidbody2D, up to three looping
    /// ParticleSystems and a TrailRenderer whose per-instance material is recoloured every frame.
    /// That population grew for the whole run. This is the ceiling applied when a weapon's own
    /// configured lifetime is unusable (the prefabs ship 0.1-0.15s, far too short to be the real
    /// intent, so the fallback below is what actually governs).
    /// </summary>
    public const float ProjectileMaxLifeSeconds = 5f;

    // ---------------------------------------------------------------------
    // Perf watchdog
    // ---------------------------------------------------------------------

    /// <summary>Sustained frame time (seconds) above which High demotes to Low. ~40fps.</summary>
    public const float DemoteFrameTime = 1f / 40f;

    /// <summary>How long the frame time must stay bad before demoting.</summary>
    public const float DemoteAfterSeconds = 3f;

    // ---------------------------------------------------------------------
    // Boot
    // ---------------------------------------------------------------------

    private static bool _initialized;

    /// <summary>
    /// Runs before the first scene's Awake so components that read these flags in Awake
    /// (CameraShake) see real values. There is precedent for the attribute in this build:
    /// SimpleInput.cs and WebGLWindow.cs both use it.
    /// </summary>
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    public static void Initialize()
    {
        if (_initialized)
            return;

        _initialized = true;

        ShakeEnabled = ReadFlag(ShakeKey, true);
        HitStopEnabled = ReadFlag(HitStopKey, true);
        HitFlashEnabled = ReadFlag(HitFlashKey, true);
        KillVfxEnabled = ReadFlag(KillVfxKey, true);
        MuzzleFlashEnabled = ReadFlag(MuzzleFlashKey, true);
        ScreenFxEnabled = ReadFlag(ScreenFxKey, true);

        // An explicit user choice always wins over detection.
        QualityIsAutomatic = !PlayerPrefs.HasKey(QualityKey);

        DetectedQuality = QualityIsAutomatic
            ? DetectQuality()
            : (JuiceQuality)Mathf.Clamp(PlayerPrefs.GetInt(QualityKey), 0, 1);

        Quality = DetectedQuality;
    }

    /// <summary>
    /// Undoes a runtime demotion at the start of a new run. Does nothing when the player chose a
    /// tier explicitly.
    /// </summary>
    public static void RearmDetectedQuality()
    {
        if (QualityIsAutomatic)
            Quality = DetectedQuality;
    }

    private static bool ReadFlag(string key, bool fallback)
    {
        return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetInt(key) != 0 : fallback;
    }

    /// <summary>
    /// Best-effort device tiering, only ever downward -- a wrong guess costs a plainer picture,
    /// never a broken frame budget. The frame-time watchdog in JuiceRuntime is the real safety
    /// net.
    /// </summary>
    private static JuiceQuality DetectQuality()
    {
        // Implemented properly on WebGL (JS_SystemInfo_IsMobile, a user-agent check), so this is
        // the one probe worth trusting there.
        if (Application.isMobilePlatform)
            return JuiceQuality.Low;

        // NEITHER probe below means anything in a browser, and both fail toward Low:
        //  - SystemInfo.systemMemorySize is JS_SystemInfo_GetMemory, literally
        //    `HEAPU8.length / (1024*1024)` -- the wasm heap, which this project ships at 482 MB.
        //    A "< 4096" test is therefore true on 100% of clients, forever;
        //  - processorCount is not backed by hardwareConcurrency with webGLThreadsSupport off.
        // Trusting them made the High preset unreachable in the only build that actually ships,
        // silently disabling the kill bursts, enemy muzzle flashes and the watchdog itself. This
        // was caught in review, not in testing, because the Editor reports real RAM and looks fine.
        if (Application.platform == RuntimePlatform.WebGLPlayer)
            return JuiceQuality.High;

        var cores = SystemInfo.processorCount;
        if (cores > 0 && cores <= 2)
            return JuiceQuality.Low;

        var memory = SystemInfo.systemMemorySize;
        if (memory > 0 && memory < 4096)
            return JuiceQuality.Low;

        return JuiceQuality.High;
    }

    /// <summary>
    /// Writes a toggle and persists it. UISettings never calls PlayerPrefs.Save(), which on
    /// WebGL (IndexedDB-backed) means a player who closes the tab straight after flipping a
    /// switch loses it. EnemySpawner and SceneLoader are the precedent for saving explicitly.
    /// </summary>
    public static void SetFlag(string key, bool value)
    {
        PlayerPrefs.SetInt(key, value ? 1 : 0);
        PlayerPrefs.Save();

        switch (key)
        {
            case ShakeKey: ShakeEnabled = value; break;
            case HitStopKey: HitStopEnabled = value; break;
            case HitFlashKey: HitFlashEnabled = value; break;
            case KillVfxKey: KillVfxEnabled = value; break;
            case MuzzleFlashKey: MuzzleFlashEnabled = value; break;
            case ScreenFxKey: ScreenFxEnabled = value; break;
        }
    }

    public static void SetQuality(JuiceQuality quality, bool persist)
    {
        Quality = quality;

        if (!persist)
            return;

        DetectedQuality = quality;
        QualityIsAutomatic = false;

        PlayerPrefs.SetInt(QualityKey, (int)quality);
        PlayerPrefs.Save();
    }
}
