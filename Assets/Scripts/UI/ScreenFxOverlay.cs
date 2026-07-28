using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Full-screen visual feedback: a static vignette plus a short colour flash on damage.
///
/// This is Phase 1's answer to GDD 3.2 item 4 ("post-processing: bloom + vignette + subtle
/// chromatic aberration"), and it is a deliberate deviation -- see docs/ms2-gdd.md 3.2. Real
/// post-processing was assessed and rejected for this project, not for effort but because it
/// lands on three separate landmines at once:
///
///  - the project is Built-in pipeline in GAMMA colour space, where bloom thresholding is
///    non-physical. The dead PPv2 profile still in the repo needed intensity 9 at threshold 0.98
///    to look acceptable, which is the signature of fighting the colour space;
///  - WebGL graphics API selection is Automatic with no WebGL entry in m_BuildTargetGraphicsAPIs,
///    so WebGL 1.0 is still a live fallback and a bloom mip chain can silently degrade there;
///  - the gameplay camera renders straight to the backbuffer today (m_ForceIntoRT 0,
///    m_TargetTexture 0, m_AllowMSAA 0). Any post effect forces an intermediate full-screen
///    render target plus a blit chain -- a pure fill-rate tax on a 2D side-scroller whose
///    first-class perf target is mobile WebGL.
///
/// The substitute costs two transparent quads and no render target. "Bloom" is delivered where
/// it belongs for a 2D game: as additive glow sprites on the emitters themselves, which the
/// FORGE3D library already provides and GameEffectsPool now spawns.
///
/// Created from GameManager.Start so it lives and dies with the gameplay scene -- a
/// DontDestroyOnLoad overlay would follow the player into the menu and inventory scenes.
/// </summary>
public class ScreenFxOverlay : MonoBehaviour
{
    private const int VignetteResolution = 128;

    /// <summary>Where the vignette starts fading in, as a fraction of centre-to-corner distance.</summary>
    private const float VignetteInnerRadius = 0.45f;

    private static Sprite _vignetteSprite;

    public static ScreenFxOverlay Instance { get; private set; }

    private Image _tint;
    private float _tintRemaining;
    private float _tintDuration;
    private Color _tintColor;

    public static ScreenFxOverlay Create()
    {
        if (Instance != null)
            return Instance;

        if (!JuiceSettings.ScreenFxEnabled)
            return null;

        var go = new GameObject("[ScreenFx]");
        return go.AddComponent<ScreenFxOverlay>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        var canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // The highest sorting order among the scene's six existing overlay canvases is 7, so this
        // sits above all of them without competing.
        canvas.sortingOrder = 1000;

        BuildVignette();
        _tint = BuildFullScreenImage("Tint", Color.clear, null);

        enabled = false;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>Short full-screen colour wash. Used for player damage.</summary>
    public static void Flash(Color color, float alpha, float seconds)
    {
        if (!JuiceSettings.ScreenFxEnabled || Instance == null)
            return;

        Instance.BeginFlash(color, alpha, seconds);
    }

    private void BeginFlash(Color color, float alpha, float seconds)
    {
        if (_tint == null || seconds <= 0f)
            return;

        color.a = Mathf.Clamp01(alpha);

        // Take the stronger of an in-flight flash and the new one so rapid hits read as one
        // sustained wash rather than a strobe.
        if (_tintRemaining > 0f && _tintColor.a > color.a)
            color.a = _tintColor.a;

        _tintColor = color;
        _tintDuration = seconds;
        _tintRemaining = seconds;
        _tint.color = color;

        enabled = true;
    }

    private void Update()
    {
        if (_tintRemaining <= 0f)
        {
            if (_tint != null)
                _tint.color = Color.clear;

            enabled = false;
            return;
        }

        // Unscaled so the wash still resolves during a hit-stop or a pause.
        _tintRemaining -= Time.unscaledDeltaTime;

        var k = Mathf.Clamp01(_tintRemaining / Mathf.Max(0.0001f, _tintDuration));
        var color = _tintColor;
        color.a = _tintColor.a * k;
        _tint.color = color;
    }

    private void BuildVignette()
    {
        if (_vignetteSprite == null)
            _vignetteSprite = BuildVignetteSprite();

        var image = BuildFullScreenImage("Vignette", new Color(0f, 0f, 0f, JuiceSettings.VignetteAlpha), _vignetteSprite);
        image.type = Image.Type.Simple;
    }

    private Image BuildFullScreenImage(string name, Color color, Sprite sprite)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(transform, false);

        var rect = (RectTransform)go.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;

        // Mandatory. The gameplay scene already has six overlay canvases; a full-screen Image
        // left as a raycast target swallows every pointer and touch event above them.
        image.raycastTarget = false;

        return image;
    }

    /// <summary>
    /// Builds the vignette gradient in code rather than shipping a PNG, so the effect adds no
    /// asset, no import settings and no meta file. 128x128 bilinear is plenty for a soft
    /// gradient stretched to the full screen.
    /// </summary>
    private static Sprite BuildVignetteSprite()
    {
        var texture = new Texture2D(VignetteResolution, VignetteResolution, TextureFormat.ARGB32, false)
        {
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            name = "VignetteGradient"
        };

        var pixels = new Color32[VignetteResolution * VignetteResolution];
        var last = VignetteResolution - 1;
        var cornerDistance = Mathf.Sqrt(2f);

        for (var y = 0; y < VignetteResolution; y++)
        {
            var dy = (y / (float)last) * 2f - 1f;

            for (var x = 0; x < VignetteResolution; x++)
            {
                var dx = (x / (float)last) * 2f - 1f;
                var distance = Mathf.Sqrt(dx * dx + dy * dy) / cornerDistance;
                var alpha = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(VignetteInnerRadius, 1f, distance));

                pixels[y * VignetteResolution + x] = new Color32(255, 255, 255, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, true);

        return Sprite.Create(
            texture,
            new Rect(0f, 0f, VignetteResolution, VignetteResolution),
            new Vector2(0.5f, 0.5f),
            100f,
            0,
            SpriteMeshType.FullRect);
    }
}
