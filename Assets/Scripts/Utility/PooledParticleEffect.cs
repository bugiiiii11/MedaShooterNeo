using UnityEngine;

/// <summary>
/// Restarts a pooled particle effect explicitly when it is taken out of the pool.
///
/// The FORGE3D effect prefabs are pure ParticleSystems with playOnAwake enabled and stopAction
/// None, so they neither restart reliably on SetActive(true) for every child system nor clean up
/// after themselves. Caching the systems once on the clone (which the pool reuses forever) keeps
/// the per-spawn cost at a loop over a cached array rather than a GetComponentsInChildren
/// allocation on every kill.
/// </summary>
public class PooledParticleEffect : MonoBehaviour
{
    private ParticleSystem[] _systems;

    /// <summary>Attaches on first use and returns the cached restarter.</summary>
    public static PooledParticleEffect Attach(GameObject target)
    {
        if (target == null)
            return null;

        if (!target.TryGetComponent<PooledParticleEffect>(out var effect))
            effect = target.AddComponent<PooledParticleEffect>();

        return effect;
    }

    public void Restart()
    {
        // Include inactive: some FORGE3D effects keep a sub-emitter disabled in the prefab.
        _systems ??= GetComponentsInChildren<ParticleSystem>(true);

        for (var i = 0; i < _systems.Length; i++)
        {
            var system = _systems[i];
            if (system == null)
                continue;

            system.Clear(false);
            system.Play(false);
        }
    }
}
