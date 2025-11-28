using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeartlessPerkSpawner : MonoBehaviour
{
    public int AmountOfHearts = 4;
    public Vector2 TimeBetweenRange;

    IEnumerator Start()
    {
        yield return new WaitForSeconds(1);
        for(var i = 0; i < AmountOfHearts; i++)
        {
            var wait = TimeBetweenRange.Random();
            PowerupSpawner.Spawn(PowerupSpawner.Instance.Powerups.Powerups.Find(x => x.Title == "Hp Refill"), new DummyLocation(LevelInfo.instance.SpawnPositions.Random()));
            yield return new WaitForSeconds(wait);
        }

        Destroy(gameObject);
    }
}
