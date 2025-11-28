using ElRaccoone.Tweens;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainButtonAbility : AbilityConfig
{
    public override void Activate()
    {
        GameManager.instance.GameStats.AbilityUseCount++;
        currentCooldown = ComputedCooldown;
        ShieldObject.TweenCancelAll();
        ShieldObject.TweenSpriteRendererAlpha(1, 0.3f).SetFrom(0);
        ShieldObject.SetActive(true);
        ShotObject.SetActive(true);
        ShotObject.transform.parent.gameObject.SetActive(true);

        OneShotAudioPool.SpawnOneShot(activate, 0.75f);

        ShieldCounter.StartCount(ActivatedTime);
        Owner.StartCoroutine(DeactivateAfter(ActivatedTime));

        Owner.GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        Owner.InvokeRepeating(nameof(this.CooldownCounter), 0, 1);

        GameManager.instance.Player.IsChainGunActive = true;
    }

    public override bool IsKeyPressed()
    {
        return Input.GetKeyUp(KeyCode.E) || Input.GetKeyUp(KeyCode.RightControl) || Input.GetKeyUp(KeyCode.Joystick1Button5) || Input.GetKeyUp(KeyCode.Joystick1Button14);
    }

    protected IEnumerator DeactivateAfter(float time)
    {
        yield return new WaitForSeconds(time);

        if (GameManager.instance.Player.isDead)
            yield break;

       // ShieldObject.GetComponentInChildren<SpriteRenderer>().TweenSpriteRendererAlpha(0, 0.3f).SetFrom(1).SetOnComplete(() =>
        {
            ShieldObject.SetActive(false);
            ShotObject.SetActive(false);
            ShotObject.transform.parent.gameObject.SetActive(false);

            OneShotAudioPool.SpawnOneShot(deactivate, 0.9f);
            GameManager.instance.Player.IsChainGunActive = false;
        }//);
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
