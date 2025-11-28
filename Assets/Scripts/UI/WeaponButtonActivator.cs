using CodeStage.AntiCheat.ObscuredTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class WeaponButtonActivator : MonoBehaviour
{
    public ObscuredInt KeepTime = 10;
    public ObscuredInt CooldownTime = 10;
    public TMPro.TextMeshProUGUI CountingText;
    public string weaponName;
    public Image WeaponIcon;

    public AudioClip activate, deactivate;

    public ObscuredFloat Cooldown = 60;
    public float ComputedCooldown => Cooldown;// - GameConstants.Constants.UltimateCooldownReduction;
    internal ObscuredFloat currentCooldown = 60;

    public void Activate()
    {
        var wc = GameManager.instance.Player.GetComponent<WeaponController>();

        // bring order of tiers down so we equip the correct weapon!
        var correctName = GetDetieredWeaponName(weaponName);

        wc.SetWeapon(correctName, KeepTime);
        wc.WeaponCountdown.OnPowerupWeaponCollected(new WeaponPowerupTimedEvent(KeepTime));

        currentCooldown = ComputedCooldown;

        if(activate)
            OneShotAudioPool.SpawnOneShot(activate, 0.75f);

        GetComponent<CanvasGroup>().interactable = false;
        CountingText.gameObject.SetActive(true);
        InvokeRepeating(nameof(CooldownCounter), 0, 1);

    }

    public static string GetDetieredWeaponName(string weaponName)
    {
        weaponName = weaponName.ToLower();

        return weaponName switch
        {
            "tadashi katana" => "ryoshi katana",
            "moon blade" => "blessed blade",
            "righteous claymore" => "tactician's claymore",
            "merciless greatsword" => "gladiator's greatsword",
            "mercilless greatsword" => "gladiator's greatsword",
            "mercilles's greatsword" => "gladiator's greatsword",
            "serpent's bite" => "viper",
            "tundrastalker's sniper rifle" => "sandcrawler's sniper rifle",
            "soldier's repeater" => "adept's repeater",
            "victim's meda-gun" => "underdog meda-gun",
            _ => weaponName,
        };
    }

    public static (string n, string tier) GetDetieredWeaponNameWithTier(string weaponName)
    {
        weaponName = weaponName.ToLower();

        return weaponName switch
        {
            "tadashi katana" => ("ryoshi katana","T2"),
            "moon blade" => ("blessed blade","T2"),
            "righteous claymore" => ("tactician's claymore","T2"),
            "merciless greatsword" => ("gladiator's greatsword","T2"),
            "mercilless greatsword" => ("gladiator's greatsword","T2"),
            "mercilles's greatsword" => ("gladiator's greatsword","T2"),
            "serpent's bite" => ("viper","T2"),
            "tundrastalker's sniper rifle" => ("sandcrawler's sniper rifle","T2"),
            "soldier's repeater" => ("adept's repeater","T2"),
            "victim's meda-gun" => ("underdog meda-gun","T2"),
            _ => (weaponName,"T1"),
        };
    }

    public void CooldownCounter()
    {
        currentCooldown--;
        CountingText.text = currentCooldown.ToString();

        if (currentCooldown <= 0)
        {
            CancelInvoke(nameof(CooldownCounter));
            CountingText.gameObject.SetActive(false);
            GetComponent<CanvasGroup>().interactable = true;
            currentCooldown = ComputedCooldown;
        }
    }

    protected virtual bool IsKeyPressed()
    {
        return Input.GetKeyUp(KeyCode.F) || Input.GetKeyUp(KeyCode.RightShift) || Input.GetKeyUp(KeyCode.Joystick1Button3) || Input.GetKeyUp(KeyCode.Joystick1Button19);

    }

    private void Update()
    {
        if (GameManager.instance.IsGamePaused)
            return;

        if (IsKeyPressed())
        {
            if (GetComponent<CanvasGroup>().interactable)
                Activate();
        }
    }

    internal void SetIcon(string weaponName)
    {
        var weapIcon = Resources.LoadAll<Sprite>("NFTIcons/weaponIcons").First(x => string.Equals(x.name, weaponName, StringComparison.OrdinalIgnoreCase));
        WeaponIcon.sprite = weapIcon;
    }
}
