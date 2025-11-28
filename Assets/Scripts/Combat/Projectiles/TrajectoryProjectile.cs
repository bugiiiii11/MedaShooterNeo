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
