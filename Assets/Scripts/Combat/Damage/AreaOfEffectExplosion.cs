using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AreaOfEffectExplosion : MonoBehaviour
{
    private List<Collider2D> colliders = new();

    [Range(0,1)]
    public float DamageAmountInMaxHpPercentage = 0.2f;
    private void Start()
    {
        Destroy(gameObject, 0.3f);
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Enemy") && !colliders.Contains(other))
        {
            colliders.Add(other);
            var dr = other.GetComponent<DamageReceiver>();
            var damage = new DamageInfo(Mathf.CeilToInt(dr.HitPoints * DamageAmountInMaxHpPercentage), false);
            dr.ReceiveDamage(damage);
        }
    }
}
