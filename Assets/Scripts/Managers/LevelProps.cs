using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelProps : Singleton<LevelProps>
{
    public GameObject EnemyExplosionNormal;
    public GameObject EnemyDeathNormal, EnemyDeathElectric;
    public GameObject TntExplosion;
    public GameObject SnapExplosion;

    public GameObject MinePrefab;
    public DotBase DeepWoundScriptable;

    [Header("Hit FX")]
    public GameObject HitPistol;
    public GameObject HitAssaultNormal;
    public GameObject HitAssaultPlasma;
    public GameObject HitSniper;
    public GameObject OneShotClip;
    public GameObject ShieldAbsorbEffect;

    [Header("Sounds")]
    public AudioClip HitShot1;
    public AudioClip HitShot2;
    public AudioClip DeathPlayerSound, GameOverSound;
    public AudioClip KillingSpree, KillingSpreeOver;
    public AudioClip SelectionClip;
    public AudioClip Confirm, Accept;
    public AudioClip HitShield;

    [Header("Colors")]
    public Color BasicColor;
    public Color RareColor;
    public Color EpicColor;
}
