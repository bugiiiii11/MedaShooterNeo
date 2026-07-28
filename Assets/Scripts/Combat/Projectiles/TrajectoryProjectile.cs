using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public class TrajectoryProjectile : Projectile
{
    private TrailRenderer _trailRenderer;
    private Material _trailMaterial;

    private float _trailFadeTime;

    [MinMaxSlider(0,5)]
    public Vector2 TrailLifeTime;

    [MinMaxSlider(0,5)]
    public Vector2 TrailEndWidth;
    private Color _trailColor;

    protected override void Awake()
    {
        base.Awake();

        _trailRenderer = GetComponentInChildren<TrailRenderer>();
        if (_trailRenderer)
        {
            // Randomize Trail
            _trailMaterial = _trailRenderer.material;
            var texOffset = _trailMaterial.mainTextureOffset;
            texOffset.x -= Random.Range(-25f, 25f);
            _trailMaterial.mainTextureOffset = texOffset;
            _trailRenderer.time = TrailLifeTime.Random();
            _trailRenderer.endWidth = TrailEndWidth.Random();
            _trailColor = _trailMaterial.GetColor("_TintColor");
            _trailColor *= Random.Range(0.5f, 1f);
            _trailFadeTime = Random.Range(1f, 2f);
        }
    }
    
    /// <summary>
    /// Tints the trail by which weapon fired it (GDD 3.2 item 5).
    ///
    /// This has to be Start, not Awake: Weapon assigns FiredType on the line AFTER Instantiate
    /// returns, so during Awake every projectile still reads WeaponType.Unknown. Start runs
    /// before the first Update, by which point that assignment has happened synchronously.
    ///
    /// It also has to write _trailColor rather than the renderer's startColor/endColor, because
    /// Update pushes _trailColor into the material's _TintColor every frame and would overwrite
    /// anything set elsewhere. Bounced (chain gun) and mirrored projectiles inherit the right
    /// colour for free -- both copy FiredType when they spawn.
    /// </summary>
    private void Start()
    {
        if (!_trailRenderer)
            return;

        var tint = TintFor(FiredType);

        _trailColor.r *= tint.r;
        _trailColor.g *= tint.g;
        _trailColor.b *= tint.b;
    }

    /// <summary>
    /// Multiplicative tints, deliberately gentle: the trail material is shared art tuned for the
    /// existing look, and the point is to make weapons legible at a glance, not to recolour the
    /// game.
    /// </summary>
    private static Color TintFor(WeaponType type)
    {
        switch (type)
        {
            case WeaponType.Pistol: return new Color(1f, 0.94f, 0.80f);
            case WeaponType.Assault: return new Color(1f, 0.78f, 0.48f);
            case WeaponType.AssaultPlasma: return new Color(0.86f, 0.62f, 1f);
            case WeaponType.AssaultLaser: return new Color(0.58f, 0.95f, 1f);
            case WeaponType.ShotgunLaser: return new Color(1f, 0.86f, 0.55f);
            case WeaponType.Sniper: return new Color(0.72f, 1f, 0.86f);
            default: return Color.white;
        }
    }

    private void Update()
    {
        if (_trailRenderer)
        {
            // Fade Trail
            _trailColor = Color.Lerp(_trailColor, Color.clear, Time.deltaTime * _trailFadeTime);
            _trailMaterial.SetColor("_TintColor", _trailColor);
        }
    }
}
