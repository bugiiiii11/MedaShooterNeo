using System.Collections;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

public abstract class IngamePowerup : BoundedBehaviour
{
    // Handled by powerup manager
    [ReadOnly]
    public Powerup PowerupReference;
    public AudioClip PlayOnCollected;

    private void Start()
    {
        var position = Mathf.RoundToInt(transform.position.y * 100);
        var sr = GetComponentInChildren<SpriteRenderer>();
        sr.sortingOrder -= position;
    }

    private void OnTriggerEnter2D(Collider2D other) 
    {
        if(other.CompareTag("Player"))
        {
            GameManager.instance.EventManager.Dispatch(new PowerupCollectedEvent(this));
            Destroy(gameObject);

            if(PlayOnCollected)
                OneShotAudioPool.SpawnOneShot(PlayOnCollected, 0.75f, secondsAlive: 1);
        }    
    }

    public abstract void ApplyPowerup(PlayerStatsModule stats);
}
