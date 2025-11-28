using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TimeVariable
{
    [SerializeField]
    protected float cooldown;

    public virtual float Cooldown
    {
        get
        {
            return cooldown;
        }
        set
        {
            cooldown = value;
        }
    }

    [NonSerialized]
    public float LastTime;

    public bool IsOver()
    {
        return Time.time - LastTime >= Cooldown;
    }

    public void Reset()
    {
        LastTime = Time.time;
    }

    internal void Disable()
    {
        LastTime = float.MaxValue;
    }
}

[Serializable]
public class RandomTimeVariable : TimeVariable
{
    [GUIColor(0.3f, 0.8f, 0.8f, 1f)]
    public Vector2 CooldownRange;
    public override float Cooldown => CooldownRange.Random();
}
