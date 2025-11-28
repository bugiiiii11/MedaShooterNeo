using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;

public class DeepWoundButtonActivation : OhShitButtonActivator
{
    /*public override void Activate()
    {
        GameManager.instance.GameStats.AbilityUseCount++;
        currentCooldown = ComputedCooldown;
        ShieldObject.TweenCancelAll();
        ShieldObject.TweenSpriteRendererAlpha(1, 0.3f).SetFrom(0);
        ShieldObject.SetActive(true);

        OneShotAudioPool.SpawnOneShot(activate, 0.75f);

        ShieldCounter.StartCount(ActivatedTime);
        StartCoroutine(DeactivateAfter(ActivatedTime));

        GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        InvokeRepeating(nameof(CooldownCounter), 0, 1);

        GameManager.instance.Player.IsDeepWoundActive = true;
    }

    protected override bool IsKeyPressed()
    {
        return Input.GetKey(KeyCode.Q);
    }

    protected override IEnumerator DeactivateAfter(float time)
    {
        yield return new WaitForSeconds(time);

        if (GameManager.instance.Player.isDead)
            yield break;

        ShieldObject.TweenSpriteRendererAlpha(0, 0.3f).SetFrom(1).SetOnComplete(() =>
        {
            ShieldObject.SetActive(false);
            OneShotAudioPool.SpawnOneShot(deactivate, 0.9f);
            GameManager.instance.Player.IsDeepWoundActive = false;
        });
    }*/
}
