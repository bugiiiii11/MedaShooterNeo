using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DropSpawner : Singleton<DropSpawner>, ISpawner
{
    public DropDatabaseAsset Profile;

    public void Start()
    {
        // listen to enemy kills
        GameManager.instance.EventManager.AddListener<EnemyDropSpawnEvent>(EnemyDropSpawnedEvent);
    }

    private void EnemyDropSpawnedEvent(EnemyDropSpawnEvent obj)
    {
        SpawnDrop(obj.Enemy);
    }

    public static void SpawnDrop(ILocalizable location)
    {
        instance.Spawn(location);
    }

    public void Spawn(ILocalizable location)
    {
        var drop = Profile.GetByProbability();

        // Skip empty drops
        if (drop.AssetPrefab == null)
            return;

        // if coins, spawn according to current multiplier
        var count = GameManager.instance.GameConstants.CoinDropAmountMultiplier;
        if (count > 1 && drop.AssetPrefab.GetComponent<Droppable>() is CoinDroppable)
        {
            for (var i = 0; i < count; i++)
            {
                var offsetPos = UnityEngine.Random.insideUnitCircle * 1.2f;
                
                var dropInstance = (GameObject)Instantiate(drop.AssetPrefab, location._transform.position + (Vector3)offsetPos, Quaternion.identity);
                dropInstance.GetComponent<Droppable>().Item = drop;
            }
        }
        else
        {
            var dropInstance = (GameObject)Instantiate(drop.AssetPrefab, location._transform.position, Quaternion.identity);
            dropInstance.GetComponent<Droppable>().Item = drop;
        }
    }
}
