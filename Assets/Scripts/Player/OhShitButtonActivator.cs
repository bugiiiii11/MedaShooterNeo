using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ElRaccoone.Tweens;
using CodeStage.AntiCheat.ObscuredTypes;

public class OhShitButtonActivator : MonoBehaviour, IAbility
{
    public GameObject ShieldObject;
    public ShieldCounter ShieldCounter;
    public TMPro.TextMeshProUGUI CountingText;
    public AbilityConfig AbilityConfig;
    public GameObject ShotObject;

    /*public GameObject ShieldObject;
    public ShieldCounter ShieldCounter;
    public TMPro.TextMeshProUGUI CountingText;


    public AudioClip activate, deactivate;

    public ObscuredFloat Cooldown = 60, ActivatedTime = 7;
    public float ComputedCooldown => Cooldown - GameConstants.Constants.UltimateCooldownReduction;
    internal ObscuredFloat currentCooldown = 60;*/

    private void Awake()
    {
        AbilityConfig = Instantiate(AbilityConfig);
        AbilityConfig.Setup(this);
        AbilityConfig.ShieldObject = ShieldObject;
        AbilityConfig.ShotObject = ShotObject;
        AbilityConfig.CountingText = CountingText;
        AbilityConfig.ShieldCounter = ShieldCounter;
    }

    public virtual void Activate()
    {
        AbilityConfig.Activate();
    }

    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        if (UIPerkManager.IsVisible)
            return;

        if (IsKeyPressed() && GetComponent<CanvasGroup>().interactable)
        {
            Activate();
        }
    }

    protected virtual bool IsKeyPressed()
    {
        return AbilityConfig.IsKeyPressed();
    }

    public void CooldownCounter()
    {
        AbilityConfig.CooldownCounter();
    }

    /*protected void CooldownCounter()
    {
        currentCooldown--;
        CountingText.text = currentCooldown.ToString();

        if(currentCooldown <= 0)
        {
            CancelInvoke(nameof(CooldownCounter));
            CountingText.gameObject.SetActive(false);
            GetComponent<CanvasGroup>().interactable = true;
            currentCooldown = ComputedCooldown;
        }
    }*/

    /*protected virtual IEnumerator DeactivateAfter(float time)
    {
        yield return new WaitForSeconds(time);

        if (GameManager.instance.Player.isDead)
            yield break;

        ShieldObject.TweenSpriteRendererAlpha(0, 0.3f).SetFrom(1).SetOnComplete(() =>
        {
            ShieldObject.SetActive(false);
            OneShotAudioPool.SpawnOneShot(deactivate, 0.9f);
            GameManager.instance.Player.IsImmuneToDamage = false;
        });
    }*/
}
