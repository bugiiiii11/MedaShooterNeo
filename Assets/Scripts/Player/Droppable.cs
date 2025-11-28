using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Droppable : BoundedBehaviour
{
    public Drop Item { get; internal set; }

    public AudioClip PlayOnCollected;
    private void Start()
    {
        var position = Mathf.RoundToInt(transform.position.y * 100);
        var sr = GetComponentInChildren<SpriteRenderer>();
        sr.sortingOrder -= position;
    }
    protected override void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        _transform.position += Vector3.left * GameManager.instance.GlobalSpeed * Time.deltaTime;

        base.Update();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            GameManager.instance.EventManager.Dispatch(new DroppableCollectedEvent(this));
            Destroy(gameObject);

            if (PlayOnCollected)
                OneShotAudioPool.SpawnOneShot(PlayOnCollected, 0.75f, secondsAlive: 1);
        }
    }

    public abstract void ApplyDroppable(PlayerStatsModule playerStats);
}
