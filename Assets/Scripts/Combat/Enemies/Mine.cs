using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Mine : BoundedBehaviour
{
    private void Start()
    {
        _transform = transform;
    }

    protected override void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        var speed = GameManager.instance.GlobalSpeed;
        _transform.position += speed * Time.deltaTime * Vector3.left;

        base.Update();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Explode();

            // 30% of max hp
            GameManager.instance.Player.ReceiveDamage(Mathf.RoundToInt(GameManager.instance.Player.PlayerStats.MaxHp * 0.3f));
        }
        else if (other.CompareTag("Shield"))
        {
            Explode();
        }
    }

    public void Explode()
    {
        GameEffectsPool.SpawnElectricExplosion(transform.position, 1.5f);
        Destroy(gameObject);
    }

    public void SilentExplosion()
    {
        Destroy(gameObject);
    }
}
