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

    public bool IsPaused = false;

    private string scavengeWordToDisplay = "";
    private bool canDisplayWord = false;

    private int currentWaveIndex = 0;

    private void Start()
    {
        NormalSpeed = MainPlanes[0].Speed;
        Randomize(MainPlanes, MainPlaneVariants, MainPlaneAdditions);
        Randomize(ForegroundPlanes, ForegroundVariants, ForegroundAdditions);
        Randomize(BackgroundPlanes, BackgroundVariants, null);

        // listen to scavenge hunt events
        GameManager.instance.EventManager.AddListener<NextWaveEvent>(OnNextWave);
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

        Resolve(MainPlanes, MainPlaneVariants, MainPlaneAdditions, middlePlanePrefab);
        Resolve(ForegroundPlanes, ForegroundVariants, ForegroundAdditions, foregroundPrefab);
        Resolve(BackgroundPlanes, BackgroundVariants, null, backgroundPrefab);
    }

    internal static void Pause(bool pause)
    {
        GameManager.instance.Parallax.IsPaused = pause;
    }


    public void Resolve(List<ParallaxHolder> parallaxes, Sprite[] variants, DecalsProfile additions, GameObject prefab)
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

            // Compute offset
            ph.Object.position = parallaxes[parallaxes.Count - 1].Object.position + Vector3.right * firstPlane.Renderer.bounds.size.x;

            // Choose random sprite
            ph.Renderer.sprite = variants.Random();

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

    public void Randomize(List<ParallaxHolder> parallaxes, Sprite[] variants, DecalsProfile additions = null)
    {
        foreach(var ph in parallaxes)
        {
            ph.Renderer.sprite = variants.Random();

            if(additions != null)
                CreateDecals(ph, additions);
        }
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