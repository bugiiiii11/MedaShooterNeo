using ElRaccoone.Tweens;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OhShitAbility : AbilityConfig
{
    public override void Activate()
    {
        GameManager.instance.GameStats.AbilityUseCount++;
        currentCooldown = ComputedCooldown;
        ShieldObject.TweenCancelAll();
        ShieldObject.TweenSpriteRendererAlpha(1, 0.3f).SetFrom(0);
        ShieldObject.SetActive(true);

        if (ShotObject)
        {
            ShotObject.SetActive(true);
            ShotObject.transform.parent.gameObject.SetActive(true);
        }

        OneShotAudioPool.SpawnOneShot(activate, 0.75f);

        ShieldCounter.StartCount(ActivatedTime);
        Owner.StartCoroutine(DeactivateAfter(ActivatedTime));

        Owner.GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        Owner.InvokeRepeating(nameof(CooldownCounter), 0, 1);

        // set player immune to damage
        GameManager.instance.Player.IsImmuneToDamage = true;
    }

    public override bool IsKeyPressed()
    {
        return Input.GetKeyUp(KeyCode.Space) || Input.GetKeyUp(KeyCode.Return) || Input.GetKeyUp(KeyCode.Joystick1Button0) || Input.GetKeyUp(KeyCode.Joystick1Button16);
    }

    public override void CooldownCounter()
    {
        currentCooldown--;
        CountingText.text = currentCooldown.ToString();

        if (currentCooldown <= 0)
        {
            Owner.CancelInvoke(nameof(CooldownCounter));
            CountingText.gameObject.SetActive(false);

            if (ShotObject)
            {
                ShotObject.transform.parent.gameObject.SetActive(false);
                ShotObject.SetActive(false);
            }
            Owner.GetComponent<CanvasGroup>().interactable = true;
            currentCooldown = ComputedCooldown;
        }
    }

    protected virtual IEnumerator DeactivateAfter(float time)
    {
        yield return new WaitForSeconds(time);

        if (GameManager.instance.Player.isDead)
            yield break;

       // ShieldObject.TweenSpriteRendererAlpha(0, 0.3f).SetFrom(1).SetOnComplete(() =>
        //{
            ShieldObject.SetActive(false);
            OneShotAudioPool.SpawnOneShot(deactivate, 0.9f);
            GameManager.instance.Player.IsImmuneToDamage = false;
        //});
    }
}
