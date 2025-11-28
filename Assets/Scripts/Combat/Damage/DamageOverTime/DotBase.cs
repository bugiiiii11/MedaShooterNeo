using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DotBase : ScriptableObject
{
    public string Name;
    [Multiline]
    public string Description;
    public float RepeatTime;
    public int Duration;
    public Vector2 DamageRange;
    public bool DamageRangeInPercentage = false;
    public Sprite Icon;
}
