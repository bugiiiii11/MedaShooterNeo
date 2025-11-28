using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PowerupSpawner : TimeCompute, ISpawner
{
    public static PowerupSpawner Instance;
    public PowerupDatabaseAsset Powerups;
    public Vector2 TimeBetweenPowerupsRange;
    private float currentPowerupTimer = 0;

    public bool IsActive = true;

    private void Start() 
    {
        Instance = this;
        GameManager.instance.EventManager.AddListener<NextWaveEvent>(OnNextWaveSpawned);
        currentPowerupTimer = TimeBetweenPowerupsRange.Random();
        ComputeTime();
    }

    public void OnNextWaveSpawned(NextWaveEvent wave)
    {
        // upgrade powerups
    }

    private void Update() 
    {
        if (!IsActive)
            return;

        if(base.TimeSinceLastCompute > currentPowerupTimer)
        {
            ComputeTime();
            currentPowerupTimer = TimeBetweenPowerupsRange.Random();

            Spawn(new DummyLocation(LevelInfo.instance.SpawnPositions.Random()));
        }
    }

    public void Spawn(ILocalizable location)
    {
        var pu = Powerups.GetPowerupRandomByProbability();
        var spawned = Instantiate(pu.AssetPrefab, location._transform.position, Quaternion.identity);
    }

    public static void Spawn(Powerup powerup, ILocalizable location)
    {
        var spawned = Instantiate(powerup.AssetPrefab, location._transform.position, Quaternion.identity);
    }

    public static void Spawn(Powerup powerup, Vector3 location)
    {
        var spawned = Instantiate(powerup.AssetPrefab, location, Quaternion.identity);
    }
}
