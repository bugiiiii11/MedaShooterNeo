using CodeStage.AntiCheat.ObscuredTypes;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class AbilityConfig : ScriptableObject
{
    public string AbilityName, AbilityDescription;

    [NonSerialized]
    public GameObject ShieldObject, ShotObject;
    [NonSerialized]
    public ShieldCounter ShieldCounter;
    [NonSerialized]
    public TMPro.TextMeshProUGUI CountingText;
    public Sprite Icon;
    public AudioClip activate, deactivate;

    public ObscuredFloat Cooldown = 60, ActivatedTime = 7;
    public bool UseCooldownReduction = true;
    public float ComputedCooldown => Cooldown - (UseCooldownReduction ? GameConstants.Constants.UltimateCooldownReduction : 0);
    internal ObscuredFloat currentCooldown = 60;

    public abstract void Activate();

    public MonoBehaviour Owner { get; protected set; }

    public void Setup(MonoBehaviour activator)
    {
        Owner = activator;
    }

    public abstract bool IsKeyPressed();

    public abstract void CooldownCounter();
}
