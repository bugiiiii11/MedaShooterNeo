using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SnapAbility : AbilityConfig
{
    public override void Activate()
    {
        if (GameManager.instance.EnemySpawner.IsBossActive())
            return;

        GameManager.instance.GameStats.AbilityUseCount++;
        currentCooldown = ComputedCooldown;

        OneShotAudioPool.SpawnOneShot(activate, 1f);

        Owner.GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        Owner.InvokeRepeating(nameof(CooldownCounter), 0, 1);

        var enemies = new List<BasicEnemy>();
        foreach (Transform e in GameManager.instance.EnemySpawner.AllEnemies)
        {
            var basic = e.GetComponent<BasicEnemy>();

            if(!basic.IsDead)
                enemies.Add(basic);
        }
        enemies.Shuffle();
        var counter = 0;
        var max = (enemies.Count+1) / 2;
        foreach (var enemy in enemies)
        {
            // enemy.ExplodeOnMelee();
            GameEffectsPool.SpawnSnapMuted(enemy.transform.position, 1.5f);
            enemy.ExplodeOnSnap();

            counter++;
            if (counter >= max)
                break;
        }
    }

    public override bool IsKeyPressed()
    {
        return Input.GetKeyUp(KeyCode.E) || Input.GetKeyUp(KeyCode.RightControl) || Input.GetKeyUp(KeyCode.Joystick1Button5) || Input.GetKeyUp(KeyCode.Joystick1Button14);
    }

    protected IEnumerator DeactivateAfter(float time)
    {
        yield break;
    }

    public override void CooldownCounter()
    {
        currentCooldown--;
        CountingText.text = currentCooldown.ToString();

        if (currentCooldown <= 0)
        {
            Owner.CancelInvoke(nameof(CooldownCounter));
            CountingText.gameObject.SetActive(false);
            Owner.GetComponent<CanvasGroup>().interactable = true;
            currentCooldown = ComputedCooldown;
        }
    }
}
