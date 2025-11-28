using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using Events;
using CodeStage.AntiCheat.ObscuredTypes;

public class GameManager : Singleton<GameManager>
{
    //public GameObject PerksUi;
    public PlayerMovement Player;
    public bool IsGamePaused = false;
    public BackgroundResolver Parallax;
    public EventManager EventManager;
    public EnemySpawner EnemySpawner;
    public GameConstants GameConstants = new GameConstants();
    public GameStats GameStats = new GameStats();

    public bool AreSoundEffectsAllowed = true;
    public float GlobalSpeed => BackgroundResolver.NormalSpeed * GameConstants.GameSpeedMultiplier;

    public bool IsParallaxPaused => Parallax.IsPaused;

    public override void Awake()
    {
        base.Awake();
        EventManager = new EventManager();
        EnemySpawner = FindObjectOfType<EnemySpawner>();
        EventManager.AddListener<SoundAndMusicSettingsEvent>(OnSoundSettingsChanged);
    }

    private void OnSoundSettingsChanged(SoundAndMusicSettingsEvent obj)
    {
        AreSoundEffectsAllowed = obj.SoundEnabled;
    }

    void Start()
    {
        Application.targetFrameRate = 60;
    }

   
    public void PauseGame(bool pause, bool setTimeScale = true)
    {
        IsGamePaused = pause;

        if (setTimeScale)
        {
            if (pause)
            {
                Time.timeScale = 0;
            }
            else
            {
                Time.timeScale = 1;
            }
        }
        EventManager.Dispatch(new GamePauseEvent(pause));
    }

    public void PauseGame(bool pause)
    {
        PauseGame(pause, true);
    }
}
