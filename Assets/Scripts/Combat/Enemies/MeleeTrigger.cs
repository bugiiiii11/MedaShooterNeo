using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeTrigger : MonoBehaviour
{
    public bool AllowShield = true;
    private void Start()
    {
        gameObject.tag = "Melee";
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if(other.CompareTag("Player"))
        {
            // explode!
            var be = GetComponentInParent<BasicEnemy>();
            if (be)
                be.ExplodeOnMelee();

            // damage player, take 30% of maxhp
            GameManager.instance.Player.ReceiveDamage(Mathf.RoundToInt(GameManager.instance.Player.PlayerStats.MaxHp * 0.3f), isMelee: true);
        }

        else if(AllowShield && other.CompareTag("Shield"))
        {
            GetComponentInParent<BasicEnemy>().ExplodeOnMelee();
        }
    }
}
