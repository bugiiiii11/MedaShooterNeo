using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackgroundResolver : MonoBehaviour
{
    [Serializable]
    public class ScavengeHuntWord
    {
        public string Text;
        public int WaveIndex;
    }

    [SerializeField]
    private GameObject middlePlanePrefab, foregroundPrefab, backgroundPrefab;

    public Transform LeftBoundary;

    [Header("Planes")]
    public List<ParallaxHolder> MainPlanes;
    public List<ParallaxHolder> ForegroundPlanes;
    public List<ParallaxHolder> BackgroundPlanes;

    [Header("Variants")]
    public Sprite[] MainPlaneVariants;
    public Sprite[] ForegroundVariants, BackgroundVariants;

    [Header("Decals")]
    public DecalsProfile MainPlaneAdditions;
    public DecalsProfile ForegroundAdditions;

    [Header("Scavenge Hunt")]
    public List<ScavengeHuntWord> Words;
    public GameObject WordHoldingSignPrefab;

    public static float NormalSpeed;

    // Tiles are butted together by half-width sums rather than by assuming one
    // shared width: the ground variants are NOT all the same size (Ground_02 is
    // 1576x1024, Ground_01/03 are 1574x1025). The overlap then swallows the
    // sub-pixel remainder, because a gap of even a fraction of a unit shows the
    // layer behind as a vertical seam down the ground.
    private const float SeamOverlap = 0.02f;

    public bool IsPaused = false;

    private string scavengeWordToDisplay = "";
    private bool canDisplayWord = false;

    private int currentWaveIndex = 0;

    // Per-level layer tints (Phase 3). Runtime state, deliberately NOT
    // serialized -- the scene component predates them and they always come
    // from the BackdropProfile (white when no profile loads).
    private Color mainTint = Color.white;
    private Color foregroundTint = Color.white;
    private Color backgroundTint = Color.white;

    private void Start()
    {
        // Phase 3: per-level backdrop. Before Randomize, so the first three
        // planes of each layer already draw the level's variant set.
        ApplyBackdropProfile();

        NormalSpeed = MainPlanes[0].Speed;
        Randomize(MainPlanes, MainPlaneVariants, MainPlaneAdditions, mainTint);
        Randomize(ForegroundPlanes, ForegroundVariants, ForegroundAdditions, foregroundTint);
        Randomize(BackgroundPlanes, BackgroundVariants, null, backgroundTint);

        // listen to scavenge hunt events
        GameManager.instance.EventManager.AddListener<NextWaveEvent>(OnNextWave);
    }

    /// <summary>
    /// Swaps this scene's backdrop data for the current level's profile
    /// (Phase 3). Consults the same MsLevelSelect resolve as EnemySpawner --
    /// their Start order is undefined, and the single cached resolve is what
    /// keeps spawner and backdrop agreeing on the level. A missing profile
    /// (no asset, bad path) leaves the scene-serialized arrays untouched, so
    /// the game can never lose its backdrop to a data mistake.
    /// </summary>
    private void ApplyBackdropProfile()
    {
        var level = Determinism.MsLevelSelect.EffectiveLevel;
        var profile = Resources.Load<BackdropProfile>($"Backdrops/Level{level}");
        if (profile == null)
        {
            if (level != 1)
                Debug.LogWarning($"[BackgroundResolver] no backdrop profile for level {level}; keeping the default backdrop");
            return;
        }

        if (profile.MainPlaneVariants != null && profile.MainPlaneVariants.Length > 0)
            MainPlaneVariants = profile.MainPlaneVariants;
        if (profile.ForegroundVariants != null && profile.ForegroundVariants.Length > 0)
            ForegroundVariants = profile.ForegroundVariants;
        if (profile.BackgroundVariants != null && profile.BackgroundVariants.Length > 0)
            BackgroundVariants = profile.BackgroundVariants;

        mainTint = profile.MainPlaneTint;
        foregroundTint = profile.ForegroundTint;
        backgroundTint = profile.BackgroundTint;

        if (profile.OverrideDecals)
        {
            MainPlaneAdditions = profile.MainPlaneAdditions;
            ForegroundAdditions = profile.ForegroundAdditions;
        }

        SpawnAmbientParticles(profile);
    }

    /// <summary>
    /// Instantiates the profile's ambient particle prefab across the visible
    /// width. The FORGE3D prefabs are one-shot burst effects, so looping and
    /// the profile's emission/lifetime/tint/scale are forced through the
    /// ParticleSystem API here instead of duplicating prefab YAML.
    /// </summary>
    private void SpawnAmbientParticles(BackdropProfile profile)
    {
        if (profile.AmbientParticlePrefab == null)
            return;

        var cam = Camera.main;
        if (cam == null)
            return;

        var lowerLeft = cam.ViewportToWorldPoint(new Vector3(0f, 0f, 0f));
        var upperRight = cam.ViewportToWorldPoint(new Vector3(1f, 1f, 0f));
        var width = upperRight.x - lowerLeft.x;
        var midY = (lowerLeft.y + upperRight.y) / 2f;

        var count = Mathf.Max(1, profile.AmbientInstanceCount);
        for (var i = 0; i < count; i++)
        {
            var x = lowerLeft.x + width * (i + 0.5f) / count;
            var go = Instantiate(profile.AmbientParticlePrefab, new Vector3(x, midY, 0f), Quaternion.identity, transform);
            go.transform.localScale *= Mathf.Max(0.01f, profile.AmbientScale);

            foreach (var ps in go.GetComponentsInChildren<ParticleSystem>(true))
            {
                var main = ps.main;
                main.loop = true;
                main.startLifetime = profile.AmbientStartLifetime;
                main.startColor = profile.AmbientTint;

                var emission = ps.emission;
                emission.rateOverTime = profile.AmbientEmissionRate;
                // one-shot prefabs often drive everything through bursts
                emission.SetBursts(new ParticleSystem.Burst[0]);

                var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
                if (psRenderer != null && !string.IsNullOrEmpty(profile.AmbientSortingLayer))
                {
                    psRenderer.sortingLayerName = profile.AmbientSortingLayer;
                    psRenderer.sortingOrder = profile.AmbientSortingOrder;
                }

                ps.Clear();
                ps.Play();
            }
        }
    }

    private void OnNextWave(NextWaveEvent obj)
    {
        currentWaveIndex++;

        //foreach (var word in Words)
        //{
        //    var text = word.Text;
        //    var index = word.WaveIndex;

        //    if(index == currentWaveIndex)
        //    {
        //        scavengeWordToDisplay = text;
        //        canDisplayWord = true;
        //        break;
        //    }
        //}
    }

    void Update()
    {
        if (IsPaused)
            return;

        Resolve(MainPlanes, MainPlaneVariants, MainPlaneAdditions, middlePlanePrefab, mainTint);
        Resolve(ForegroundPlanes, ForegroundVariants, ForegroundAdditions, foregroundPrefab, foregroundTint);
        Resolve(BackgroundPlanes, BackgroundVariants, null, backgroundPrefab, backgroundTint);
    }

    internal static void Pause(bool pause)
    {
        GameManager.instance.Parallax.IsPaused = pause;
    }


    public void Resolve(List<ParallaxHolder> parallaxes, Sprite[] variants, DecalsProfile additions, GameObject prefab, Color tint)
    {
        if (GameManager.instance.IsGamePaused)
            return;

        // Move parallaxes
        foreach (var plane in parallaxes)
        {
            var pos = plane.Object.position;
            pos.x -= plane.Speed * GameConstants.Constants.GameSpeedMultiplier * Time.deltaTime;
            plane.Object.position = pos;
        }

        // Handle spawn of new sprites
        var firstPlane = parallaxes[0];
        if (firstPlane.Object.position.x < LeftBoundary.position.x)
        {
            var next = Instantiate(prefab, transform);
            var ph = GenerateParallaxHolder(firstPlane, next.transform);

            // Choose random sprite BEFORE positioning -- the offset depends on
            // how wide this tile actually is. Placing it on the OUTGOING tile's
            // width (the old behaviour) mismatched by up to 2px whenever the
            // variants differed, leaving the seam.
            ph.Renderer.sprite = variants.Random();
            ph.Renderer.color = tint;

            var lastPlane = parallaxes[parallaxes.Count - 1];
            ph.Object.position = lastPlane.Object.position
                + Vector3.right * (Halves(lastPlane, ph) - SeamOverlap);

            parallaxes.Add(ph);
            parallaxes.RemoveAt(0);

            Destroy(firstPlane.Object.gameObject, 0.2f);

            // Spawn additions if any
            if (additions != null)
            {
                CreateDecals(ph, additions);
            }
        }
    }

    public void Randomize(List<ParallaxHolder> parallaxes, Sprite[] variants, DecalsProfile additions = null, Color? tint = null)
    {
        foreach(var ph in parallaxes)
        {
            ph.Renderer.sprite = variants.Random();
            ph.Renderer.color = tint ?? Color.white;

            if(additions != null)
                CreateDecals(ph, additions);
        }

        // The scene authors these starting planes at a spacing that assumes one
        // shared variant width, so a mismatched draw seams the opening seconds
        // exactly like the scrolling ones did. Re-chain from plane 0 (left where
        // the scene put it) so every starting seam closes too.
        for (var i = 1; i < parallaxes.Count; i++)
        {
            var previous = parallaxes[i - 1];
            var current = parallaxes[i];
            var pos = current.Object.position;
            pos.x = previous.Object.position.x + Halves(previous, current) - SeamOverlap;
            current.Object.position = pos;
        }
    }

    /// <summary>Centre-to-centre distance that butts two tiles edge to edge.
    /// World-space bounds, so transform scale is already accounted for.</summary>
    private static float Halves(ParallaxHolder left, ParallaxHolder right)
    {
        return (left.Renderer.bounds.size.x + right.Renderer.bounds.size.x) * 0.5f;
    }

    private void CreateDecals(ParallaxHolder ph, DecalsProfile additions)
    {
        var next = ph.Object;
        
        // compute bounds
        var lowerLeft = next.position - new Vector3(ph.Renderer.bounds.size.x, ph.Renderer.bounds.size.y / 2f, 0);
        var upperRight = next.position + new Vector3(0, ph.Renderer.bounds.size.y / 2f - 2, 0); //-1 to not interfere with background

        var canSpawn = UnityEngine.Random.Range(0, 1f) < additions.SpawnProbability;
        var amount = canSpawn ? additions.AmountRange.Random() : 0;

        var spawnScavengeHunt = false;
        for (var i = 0; i < amount; i++)
        {
            Vector3 pos = Utils.GetRandomPointBetween(lowerLeft, upperRight);
            if(additions.VerticalPosition == VerticalPlacement.StageBottom)
            {
                Vector3 stageDimensions = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));

                // This runs mid-gameplay, and the decal it places is then parented to a scrolling
                // plane -- so a camera caught mid-shake would bake its displacement into the
                // decal permanently. Subtracting the live shake offset is the only place camera
                // shake can leak into world state.
                if (CameraShake.instance != null)
                    stageDimensions -= CameraShake.instance.CurrentOffset;

                pos.y = stageDimensions.y;

                // it is foreground prefab
                spawnScavengeHunt = true;
            }

            var sprRenderer = Instantiate(additions.Prefab, pos, Quaternion.identity).GetComponentInChildren<SpriteRenderer>();
            sprRenderer.sprite = additions.Sprites.Random();
            sprRenderer.transform.SetParent(ph.Renderer.transform);

            //if(canDisplayWord && spawnScavengeHunt && !sprRenderer.sprite.name.Contains("Crystal"))
            //{
            //    canDisplayWord = false;
            //    var scavengeHuntSign = Instantiate(WordHoldingSignPrefab, sprRenderer.transform);
            //    scavengeHuntSign.transform.localPosition = new Vector3(0, 1.4f, 0);
            //    scavengeHuntSign.GetComponentInChildren<TMPro.TextMeshPro>().text = scavengeWordToDisplay;
            //}
        }
    }

    private ParallaxHolder GenerateParallaxHolder(ParallaxHolder referenceParallax, Transform next)
    {
        var ph = new ParallaxHolder();
        ph.Object = next;
        ph.Renderer = next.GetComponentInChildren<SpriteRenderer>();
        ph.Speed = referenceParallax.Speed;

        return ph;
    }
}

[Serializable]
public class ParallaxHolder
{
    public Transform Object;
    public SpriteRenderer Renderer;
    public float Speed = 2;
    public float Length { get; set; }
}

[Serializable]
public class DecalsProfile
{
    public Vector2Int AmountRange;
    public Sprite[] Sprites;
    public GameObject Prefab;
    public VerticalPlacement VerticalPosition;

    [Range(0, 1f)]
    public float SpawnProbability;
}

public enum VerticalPlacement : byte
{
    StageTop,
    StageBottom,
    Randomized
}