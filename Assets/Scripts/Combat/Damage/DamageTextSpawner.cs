using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class DamageTextSpawner : Singleton<DamageTextSpawner>
{
    public GameObject DamageTextPrefab;
    public Color CritColor, HealColor, NormalColor = new Color32(255, 179, 0, 255);

    private void Start()
    {
        PoolManager.WarmPool(DamageTextPrefab, 10);
    }

    public static void Spawn(string textStr, Vector2 position)
    {
        var obj = PoolManager.SpawnObject(instance.DamageTextPrefab, position, Quaternion.identity);//Instantiate(instance.DamageTextPrefab, position, Quaternion.identity).GetComponent<TMPro.TextMeshPro>();
        var text = obj.GetComponent<TMPro.TextMeshPro>();
        text.text = textStr;
        text.color = instance.NormalColor;
        var scale = text.transform.localScale;

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
        var scale = text.transform.localScale;
        var duration = 1f;
        if (info.IsCritical)
        {
            text.color = instance.CritColor;
            text.outlineWidth = 0.15f;
            scale *= 1 + info.CriticalSize;
            duration = 2.2f;
        }

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
        var scale = text.transform.localScale;

        if (info.IsCritical)
        {
            text.color = instance.CritColor;
            scale *= 1 + info.CriticalSize;
        }

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
