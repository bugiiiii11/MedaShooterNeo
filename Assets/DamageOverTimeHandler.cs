using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DotElement
{
    public DotBase DotBase { get; set; }
    public DotIcon DotInstance { get; set; }
    public Action<int> Action { get; set; }

    public float currentCooldown = 0, activatedDuration;
}

public class DamageOverTimeHandler : MonoBehaviour
{
    public UIDotBar Bar;

    private readonly Dictionary<string, DotElement> dotDictionary = new Dictionary<string, DotElement>();
    private DamageReceiver receiver;

    public void InitializeTick(DotBase dot, DamageReceiver damageReceiver, Action<int> damageCallback)
    {
        if (dotDictionary.ContainsKey(dot.Name))
        {
            return;
        }
        receiver = damageReceiver;

        if (Bar && dot != null)
        {
            var elem = new DotElement();
            elem.DotBase = dot;
            elem.Action = damageCallback;
            elem.activatedDuration = Time.time;
            elem.DotInstance = Bar.InitializeDotIcon(dot);
            elem.DotInstance.SetSprite(dot.Icon);
            dotDictionary[dot.Name] = elem;
        }
    }

    private List<string> toRemove = new List<string>();
    private void Update()
    {
        if (dotDictionary.Count > 0)
        {
            foreach (var key in dotDictionary.Keys)
            {
                var val = dotDictionary[key];
                var dotBase = val.DotBase;
                val.currentCooldown += Time.deltaTime;

                if (val.currentCooldown >= dotBase.RepeatTime)
                {
                    int damageValue;
                    if (dotBase.DamageRangeInPercentage)
                        damageValue = Mathf.RoundToInt(dotBase.DamageRange.x * receiver.HitPoints);
                    else
                        damageValue = Mathf.RoundToInt(dotBase.DamageRange.Random());

                    val.Action?.Invoke(damageValue);
                    val.currentCooldown = 0;
                }

                if (Time.time - val.activatedDuration > dotBase.Duration)
                {
                    toRemove.Add(key);
                    Destroy(val.DotInstance);
                }
            }

            if (toRemove.Count > 0)
            {
                foreach (var key in toRemove)
                {
                    dotDictionary.Remove(key);
                    Bar.Remove(key);
                }
                toRemove.Clear();
            }
        }
    }
}
