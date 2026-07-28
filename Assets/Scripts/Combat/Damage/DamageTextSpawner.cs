using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class DamageTextSpawner : Singleton<DamageTextSpawner>
{
    public GameObject DamageTextPrefab;
    public Color CritColor, HealColor, NormalColor = new Color32(255, 179, 0, 255);

    /// <summary>
    /// Rest font size, captured from the prefab once. Damage-number objects come from a pool and
    /// are reused, so anything a crit changes has to be written back on every spawn or the next
    /// normal hit inherits it.
    /// </summary>
    private float baseFontSize = 4f;

    private void Start()
    {
        PoolManager.WarmPool(DamageTextPrefab, 10);

        if (DamageTextPrefab != null && DamageTextPrefab.TryGetComponent<TMPro.TextMeshPro>(out var prefabText))
            baseFontSize = prefabText.fontSize;
    }

    /// <summary>
    /// Makes crits visibly bigger.
    ///
    /// All three Spawn overloads used to compute `scale *= 1 + info.CriticalSize` and then never
    /// assign it back, so crit numbers only ever differed in colour. Restoring the intent through
    /// transform.localScale is not possible either: the DamageText prefab's legacy Animation
    /// component drives m_LocalScale.x/y/z for the pop-in and overwrites any assignment every
    /// frame. Font size is the channel the animation does not touch.
    /// </summary>
    private static void ApplyFontSize(TMPro.TextMeshPro text, bool isCritical, float criticalSize)
    {
        var size = instance.baseFontSize;

        if (isCritical)
        {
            // CriticalSize is (rolled - min) * 1.3 / max, so it can land outside [0,1] and can be
            // NaN when a weapon ships a zero bonus range.
            var normalized = float.IsNaN(criticalSize) ? 0f : Mathf.Clamp(criticalSize, 0f, 1.3f);
            size *= 1f + normalized * 0.35f;
        }

        text.fontSize = size;
    }

    public static void Spawn(string textStr, Vector2 position)
    {
        var obj = PoolManager.SpawnObject(instance.DamageTextPrefab, position, Quaternion.identity);//Instantiate(instance.DamageTextPrefab, position, Quaternion.identity).GetComponent<TMPro.TextMeshPro>();
        var text = obj.GetComponent<TMPro.TextMeshPro>();
        text.text = textStr;
        text.color = instance.NormalColor;
        ApplyFontSize(text, false, 0f);

        obj.GetComponent<Animation>().Play();
        var start = new Vector3(Random.Range(0.2f, 0.7f), Random.Range(0.2f, 0.7f), 0);
        text.TweenPosition(text.transform.position + start + new Vector3(Random.Range(0.45f, 0.6f), Random.Range(0.4f, 0.6f), 0), 1f)
            .SetFrom(text.transform.position + start)
            .SetOnComplete(
            () => {
                PoolManager.ReleaseObject(obj);
            });
    }

    public static void Spawn(DamageInfo info, Vector2 position)
    {
        var obj = PoolManager.SpawnObject(instance.DamageTextPrefab, position, Quaternion.identity);//Instantiate(instance.DamageTextPrefab, position, Quaternion.identity).GetComponent<TMPro.TextMeshPro>();
        var text = obj.GetComponent<TMPro.TextMeshPro>();
        text.text = info.DamageValue.ToString();
        text.color = instance.NormalColor;
        text.fontStyle = TMPro.FontStyles.Normal;
        text.outlineWidth = 0;
        var duration = 1f;
        if (info.IsCritical)
        {
            text.color = instance.CritColor;
            text.outlineWidth = 0.15f;
            duration = 2.2f;
        }

        ApplyFontSize(text, info.IsCritical, info.CriticalSize);

        obj.GetComponent<Animation>().Play();
        var start = new Vector3(Random.Range(0.2f, 0.7f), Random.Range(0.2f, 0.7f), 0);
        text.TweenPosition(text.transform.position + start + new Vector3(Random.Range(0.5f, 0.6f), Random.Range(0.5f, 0.6f), 0), duration)
            .SetFrom(text.transform.position + start)
            .SetOnComplete(
            () => PoolManager.ReleaseObject(obj));
    }

    public static void Spawn(HealInfo info, Vector2 position)
    {
        var obj = PoolManager.SpawnObject(instance.DamageTextPrefab, position, Quaternion.identity);//Instantiate(instance.DamageTextPrefab, position, Quaternion.identity).GetComponent<TMPro.TextMeshPro>();
        var text = obj.GetComponent<TMPro.TextMeshPro>();
        text.text = info.HealValue.ToString();
        text.color = instance.HealColor;
        text.fontStyle = TMPro.FontStyles.Normal;
        text.outlineWidth = 0;

        if (info.IsCritical)
        {
            text.color = instance.CritColor;
        }

        ApplyFontSize(text, info.IsCritical, info.CriticalSize);

        obj.GetComponent<Animation>().Play();
        var start = new Vector3(Random.Range(0.4f, 0.6f), Random.Range(0.4f, 0.6f), 0);
        text.TweenPosition(text.transform.position + start + new Vector3(Random.Range(0.45f, 0.6f), Random.Range(0.4f, 0.6f), 0), 1.4f)
            .SetFrom(text.transform.position + start)
            .SetOnComplete(
            () =>
            {
                PoolManager.ReleaseObject(obj);
            }
            );

        //var text = Instantiate(instance.DamageTextPrefab, position, Quaternion.identity).GetComponent<TMPro.TextMeshPro>();
        //text.text = info.HealValue.ToString();

        //var scale = text.transform.localScale;
        //text.color = instance.HealColor;

        //if(info.IsCritical)
        //{
        //    scale *= 1 + info.CriticalSize;
        //}

        //text.TweenLocalScale(scale, 0.4f).SetFrom(Vector3.zero).SetOnComplete(
        //    () => {
        //        Destroy(text.gameObject,1);
        //    }
        //);

        //text.TweenPosition(text.transform.position + new Vector3(Random.Range(0.2f, 0.4f),Random.Range(0.2f, 0.4f),0), 1.4f);
    }
}
