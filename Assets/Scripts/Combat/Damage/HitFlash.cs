using UnityEngine;

/// <summary>
/// Per-enemy hit flash (GDD 3.2 item 1). Added at runtime by <see cref="DamageReceiver"/>; the
/// precedent for that is DamageReceiver.ActivateDot, which AddComponents its DoT handler the
/// same way. Nothing needs to be wired into a prefab or a scene.
///
/// It replaces the F3DCharacterAvatar.TweenColor call that used to live inline in
/// DamageReceiver.ReceiveDamage, for three reasons:
///
///  1. TweenColor captured `var defColor = Head.color` BEFORE resetting to white, so a second
///     bullet landing mid-flash captured the flash colour as the rest colour and the enemy
///     stayed permanently pink. With auto-fire that happened constantly.
///  2. It started 9 tweens each of which chained a second on completion, and every ElRaccoone
///     tween is an AddComponent plus a Destroy -- 36 managed object churn events per hit, on a
///     WebGL target, at auto-fire rates.
///  3. There was no way to cancel it, so a flash in flight fought the death fade that
///     BasicEnemy.Kill starts (TweenAlpha to alpha 0) and could pull the corpse back to opaque.
///
/// Ceiling worth knowing: enemies render with Trooper.mat on the built-in Sprites/Default
/// shader, so SpriteRenderer.color is a vertex tint that MULTIPLIES. This can tint and darken
/// but physically cannot brighten toward white. A true white hit flash needs a custom sprite
/// shader plus an Always Included Shaders entry (a Graphics-settings edit), which is out of
/// scope for Phase 1.
/// </summary>
[DisallowMultipleComponent]
public class HitFlash : MonoBehaviour
{
    private SpriteRenderer[] _renderers;
    private Color[] _baseColors;

    private F3DCharacterAvatar _avatar;
    private WeaponController _weaponController;
    private Weapon _cachedWeapon;

    private float _elapsed;
    private bool _playing;

    private void Awake()
    {
        _avatar = GetComponent<F3DCharacterAvatar>();
        _weaponController = GetComponent<WeaponController>();
        enabled = false;
    }

    /// <summary>Starts (or restarts) the flash. Safe to call every frame.</summary>
    public void Play()
    {
        if (!JuiceSettings.HitFlashEnabled || _avatar == null)
            return;

        if (!EnsureRenderers())
            return;

        _elapsed = 0f;
        _playing = true;
        enabled = true;
        Apply(0f);
    }

    /// <summary>
    /// Restores the rest colours and goes idle. BasicEnemy.Kill calls this before starting the
    /// death fade so the two never write SpriteRenderer.color in the same frame.
    /// </summary>
    public void Stop()
    {
        if (_playing)
            Apply(0f);

        _playing = false;
        enabled = false;
    }

    private void Update()
    {
        if (!_playing)
        {
            enabled = false;
            return;
        }

        // Unscaled on purpose: a hit-stop slows the world, and the flash is the feedback that
        // explains why. Freezing it with the world would read as a dropped frame.
        _elapsed += Time.unscaledDeltaTime;

        var attack = JuiceSettings.HitFlashInSeconds;
        var release = JuiceSettings.HitFlashOutSeconds;
        var total = attack + release;

        if (_elapsed >= total)
        {
            Stop();
            return;
        }

        var k = _elapsed <= attack
            ? Mathf.Clamp01(_elapsed / Mathf.Max(0.0001f, attack))
            : 1f - Mathf.Clamp01((_elapsed - attack) / Mathf.Max(0.0001f, release));

        Apply(k);
    }

    private void Apply(float k)
    {
        if (_renderers == null)
            return;

        var flash = JuiceSettings.HitFlashColor;

        for (var i = 0; i < _renderers.Length; i++)
        {
            var renderer = _renderers[i];
            if (renderer == null)
                continue;

            var baseColor = _baseColors[i];
            // Multiply rather than replace so any per-enemy tint survives the flash.
            renderer.color = k <= 0f ? baseColor : Color.Lerp(baseColor, baseColor * flash, k);
        }
    }

    /// <summary>
    /// (Re)builds the renderer cache. The three weapon renderers come from the currently
    /// equipped Weapon, and WeaponController.ActivateWeapon SetActive-swaps whole weapon
    /// GameObjects, so a stale cache would be writing colour to a disabled object.
    /// </summary>
    private bool EnsureRenderers()
    {
        var weapon = _weaponController != null ? _weaponController.GetCurrentWeapon() : null;

        if (_renderers != null && weapon == _cachedWeapon)
            return true;

        // Put the outgoing renderers back to rest before dropping them. Rebuilding mid-flash
        // would otherwise capture a flashed colour as the new rest colour -- the exact bug that
        // made F3DCharacterAvatar.TweenColor leave enemies permanently pink.
        Apply(0f);

        _cachedWeapon = weapon;

        var body = new[]
        {
            _avatar.Head, _avatar.Body,
            _avatar.LegL, _avatar.LegR,
            _avatar.LegTopL, _avatar.LegTopR
        };

        var count = 0;
        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] != null)
                count++;
        }

        var weaponRenderers = weapon != null
            ? new[] { weapon.LeftHand, weapon.RightHand, weapon.WeaponRenderer }
            : null;

        if (weaponRenderers != null)
        {
            for (var i = 0; i < weaponRenderers.Length; i++)
            {
                if (weaponRenderers[i] != null)
                    count++;
            }
        }

        if (count == 0)
        {
            _renderers = null;
            return false;
        }

        _renderers = new SpriteRenderer[count];
        _baseColors = new Color[count];

        var next = 0;
        for (var i = 0; i < body.Length; i++)
        {
            if (body[i] == null)
                continue;

            _renderers[next] = body[i];
            _baseColors[next] = body[i].color;
            next++;
        }

        if (weaponRenderers != null)
        {
            for (var i = 0; i < weaponRenderers.Length; i++)
            {
                if (weaponRenderers[i] == null)
                    continue;

                _renderers[next] = weaponRenderers[i];
                _baseColors[next] = weaponRenderers[i].color;
                next++;
            }
        }

        return true;
    }
}
