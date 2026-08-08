using UnityEngine;

/// <summary>
/// Per-level backdrop definition (Phase 3, juice item 6). Loaded by
/// BackgroundResolver via Resources.Load("Backdrops/Level{n}") -- Resources
/// rather than a serialized field so no scene surgery is needed and a missing
/// asset degrades to the scene-serialized arrays (Level 1 stays
/// pixel-identical to the pre-Phase-3 game).
///
/// No new art: variants draw on the environment sprites already in the
/// project, ambient particles reuse FORGE3D effect prefabs. Those prefabs are
/// authored as one-shot bursts, so BackgroundResolver forces looping and
/// applies the overrides below through the ParticleSystem API at spawn time
/// -- do NOT duplicate prefab YAML to make looping variants.
/// </summary>
[CreateAssetMenu(fileName = "BackdropProfile", menuName = "MedaShooter/Backdrop Profile")]
public class BackdropProfile : ScriptableObject
{
    [Header("Layer sprite variants (empty = keep the scene's)")]
    public Sprite[] MainPlaneVariants;
    public Sprite[] ForegroundVariants;
    public Sprite[] BackgroundVariants;

    [Header("Layer tints (white = untouched)")]
    public Color MainPlaneTint = Color.white;
    public Color ForegroundTint = Color.white;
    public Color BackgroundTint = Color.white;

    [Header("Decal overrides (only when enabled -- profiles carry whole sets)")]
    public bool OverrideDecals;
    public DecalsProfile MainPlaneAdditions;
    public DecalsProfile ForegroundAdditions;

    [Header("Ambient particles (optional; forced to loop at spawn)")]
    public GameObject AmbientParticlePrefab;
    [Range(1, 6)]
    public int AmbientInstanceCount = 2;
    public float AmbientEmissionRate = 4f;
    public float AmbientStartLifetime = 7f;
    public float AmbientScale = 3f;
    public Color AmbientTint = Color.white;
    public string AmbientSortingLayer = "";
    public int AmbientSortingOrder = 0;
}
