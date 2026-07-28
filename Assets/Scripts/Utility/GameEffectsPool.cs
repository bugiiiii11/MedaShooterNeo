using System.Collections.Generic;
using UnityEngine;

public class GameEffectsPool : Singleton<GameEffectsPool>
{
    public LevelProps props;

    // Runtime paths into the FORGE3D Resources root. Those assets are already inside the shipped
    // WebGL build (Resources folders are always included), so reusing them adds zero download
    // size -- today they are pure dead weight, referenced by nothing that actually runs.
    private const string SparksPath = "Effects/Hits/Pistol_Hit_01";
    private const string EnergyBurstPath = "Effects/Hits/AssaultLaser_Hit";
    private const string SmokePath = "Effects/Smoke/Sniper_Barrel_Smoke_01";

    private static GameObject _sparks;
    private static GameObject _energyBurst;
    private static GameObject _smoke;
    private static bool _resourcesLoaded;

    /// <summary>
    /// Pooled effects awaiting release, swept from Update.
    ///
    /// This replaces the per-spawn `StartCoroutine(ReturnToPoolAfterTime(...))`, which allocated
    /// both a coroutine and a `new WaitForSeconds` on every single explosion, and -- worse --
    /// waited in SCALED time. Since GameManager.PauseGame drives timeScale to 0, any effect
    /// spawned just before a pause never came back and the pool grew by one stranded instance
    /// each time.
    /// </summary>
    private readonly List<PendingRelease> _pending = new List<PendingRelease>(32);

    private struct PendingRelease
    {
        public GameObject Obj;
        public float ReleaseAt;
    }

    void Start()
    {
        PoolManager.WarmPool(props.EnemyDeathNormal, 3);
        PoolManager.WarmPool(props.EnemyDeathElectric, 2);
        PoolManager.WarmPool(props.ShieldAbsorbEffect, 2);
        PoolManager.WarmPool(props.SnapExplosion, 3);

        LoadEffectResources();
    }

    private void Update()
    {
        if (_pending.Count == 0)
            return;

        var now = Time.unscaledTime;

        for (var i = _pending.Count - 1; i >= 0; i--)
        {
            if (now < _pending[i].ReleaseAt)
                continue;

            var obj = _pending[i].Obj;
            _pending.RemoveAt(i);

            if (obj != null)
                PoolManager.ReleaseObject(obj);
        }
    }

    private void ReleaseAfter(GameObject obj, float seconds)
    {
        if (obj == null)
            return;

        _pending.Add(new PendingRelease { Obj = obj, ReleaseAt = Time.unscaledTime + seconds });
    }

    /// <summary>
    /// Resolves the reusable FORGE3D effects once. Resources.Load is a string-keyed lookup, so
    /// it must never run per kill.
    /// </summary>
    private static void LoadEffectResources()
    {
        if (_resourcesLoaded)
            return;

        _resourcesLoaded = true;
        _sparks = Resources.Load<GameObject>(SparksPath);
        _energyBurst = Resources.Load<GameObject>(EnergyBurstPath);
        _smoke = Resources.Load<GameObject>(SmokePath);
    }

    /// <summary>Spawns a pooled particle prefab and schedules its release. Null-safe.</summary>
    private static void SpawnParticles(GameObject prefab, Vector3 pos, float aliveSeconds)
    {
        if (prefab == null || instance == null)
            return;

        var clone = PoolManager.SpawnObject(prefab, pos, Quaternion.identity);

        // Not `?.` -- null-conditional bypasses UnityEngine.Object's overloaded null check.
        var restarter = PooledParticleEffect.Attach(clone);
        if (restarter != null)
            restarter.Restart();

        instance.ReleaseAfter(clone, aliveSeconds);
    }

    public static void SpawnShieldAbsorb(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.ShieldAbsorbEffect, pos, Quaternion.identity);
        instance.ReleaseAfter(explosion, aliveSeconds);
    }

    public static void SpawnNormalExplosion(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathNormal, pos, Quaternion.identity);
        instance.ReleaseAfter(explosion, aliveSeconds);
    }

    public static void SpawnNormalExplosionMuted(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathNormal, pos, Quaternion.identity);

        // Guarded because this body gets copied: the FORGE3D effect prefabs have no AudioSource
        // at all, and the original unguarded GetComponent would NullReference on them.
        if (explosion.TryGetComponent<AudioSource>(out var audio))
            audio.volume = 0;

        instance.ReleaseAfter(explosion, aliveSeconds);
    }

    public static void SpawnSnapMuted(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.SnapExplosion, pos, Quaternion.identity);
        instance.ReleaseAfter(explosion, aliveSeconds);
    }

    public static void SpawnElectricExplosion(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathElectric, pos, Quaternion.identity);
        instance.ReleaseAfter(explosion, aliveSeconds);
    }

    // ---------------------------------------------------------------------
    // Juice pass (GDD 3.2 item 3)
    // ---------------------------------------------------------------------

    /// <summary>
    /// The upgraded normal-enemy kill. Layers the existing sprite-flipbook explosion (which
    /// carries the kill sound) with particle sparks and the four-system Enemy_Explode prefab.
    ///
    /// Enemy_Explode is free: it is already assigned to LevelProps.EnemyExplosionNormal in
    /// develop_overhaul.unity and referenced by exactly zero lines of C#, so it has been shipping
    /// in the build and never once played.
    ///
    /// On the Low preset this degrades to precisely the pre-Phase-1 behaviour.
    /// </summary>
    public static void SpawnKillBurst(Vector3 pos, float aliveSeconds)
    {
        SpawnNormalExplosion(pos, aliveSeconds);

        CameraShake.Shake(JuiceSettings.ShakeKillDuration, JuiceSettings.ShakeKillAmount);

        if (!JuiceSettings.KillVfxEnabled || !JuiceSettings.IsHigh)
            return;

        LoadEffectResources();
        SpawnParticles(_sparks, pos, JuiceSettings.KillBurstSeconds);

        if (instance != null && instance.props != null)
            SpawnParticles(instance.props.EnemyExplosionNormal, pos, JuiceSettings.KillBurstSeconds);
    }

    /// <summary>Heavier version for bosses and minibosses -- bigger shake, extra smoke, a hit-stop.</summary>
    public static void SpawnBossKillBurst(Vector3 pos, float aliveSeconds)
    {
        SpawnElectricExplosion(pos, aliveSeconds);

        CameraShake.Shake(JuiceSettings.ShakeBossKillDuration, JuiceSettings.ShakeBossKillAmount);
        JuiceRuntime.RequestHitStop(JuiceSettings.HitStopBossKillSeconds, JuiceSettings.HitStopScale);

        if (!JuiceSettings.KillVfxEnabled)
            return;

        LoadEffectResources();
        SpawnParticles(_energyBurst, pos, JuiceSettings.KillBurstSeconds);

        if (!JuiceSettings.IsHigh)
            return;

        SpawnParticles(_sparks, pos, JuiceSettings.KillBurstSeconds);
        SpawnParticles(_smoke, pos, JuiceSettings.KillBurstSeconds * 1.6f);

        if (instance != null && instance.props != null)
            SpawnParticles(instance.props.EnemyExplosionNormal, pos, JuiceSettings.KillBurstSeconds);
    }
}
