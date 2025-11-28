using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class SnapButtonActivation : OhShitButtonActivator
{
   /* public override void Activate()
    {
        GameManager.instance.GameStats.AbilityUseCount++;
        currentCooldown = ComputedCooldown;

        OneShotAudioPool.SpawnOneShot(activate, 1f);

        GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        InvokeRepeating(nameof(CooldownCounter), 0, 1);

        var enemies = new List<BasicEnemy>();
        foreach(Transform e in GameManager.instance.EnemySpawner.AllEnemies)
        {
            enemies.Add(e.GetComponent<BasicEnemy>());
        }
        enemies.Shuffle();
        var counter = 0;
        var max = enemies.Count / 2;
        foreach(var enemy in enemies)
        {
            // enemy.ExplodeOnMelee();
            enemy.ExplodeOnMeleeWithoutSound();
            GameEffectsPool.SpawnNormalExplosionMuted(enemy.transform.position, 1.5f);

            counter++;
            if (counter >= max )
                break;
        }
    }

    protected override bool IsKeyPressed()
    {
        return Input.GetKey(KeyCode.R);
    }

    protected override IEnumerator DeactivateAfter(float time)
    {
        yield break;
    }*/
}
