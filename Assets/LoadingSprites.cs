using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class LoadingSprites : MonoBehaviour
{
    public float LoadingSpeed = 0.4f;
    public Color LoadingActiveColor, LoadingInactiveColor;

    private int currentIndex = 0;
    public List<SpriteRenderer> renderers;

    private void Start()
    {
        InvokeRepeating(nameof(ChangeColor), 0, LoadingSpeed);
    }

    private void ChangeColor()
    {
        var b = renderers[currentIndex];

        b.TweenSpriteRendererColor(LoadingInactiveColor, LoadingSpeed);

        currentIndex = (currentIndex + 1) % renderers.Count;

        var r = renderers[currentIndex];

        r.TweenSpriteRendererColor(LoadingActiveColor, LoadingSpeed);
    }
}
