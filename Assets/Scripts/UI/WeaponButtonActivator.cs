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
            // T3 (build v24)
            "uchigatana" => "ryoshi katana",
            "protector's blade" => "blessed blade",
            "knightsfall claymore" => "tactician's claymore",
            "vicious greatsword" => "gladiator's greatsword",
            "venom" => "viper",
            "swarmhunter's sniper rifle" => "sandcrawler's sniper rifle",
            "commander's repeater" => "adept's repeater",
            "bloodsoaked meda-gun" => "underdog meda-gun",
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
            // T3 (build v24)
            "uchigatana" => ("ryoshi katana","T3"),
            "protector's blade" => ("blessed blade","T3"),
            "knightsfall claymore" => ("tactician's claymore","T3"),
            "vicious greatsword" => ("gladiator's greatsword","T3"),
            "venom" => ("viper","T3"),
            "swarmhunter's sniper rifle" => ("sandcrawler's sniper rifle","T3"),
            "commander's repeater" => ("adept's repeater","T3"),
            "bloodsoaked meda-gun" => ("underdog meda-gun","T3"),
            _ => (weaponName,"T1"),
        };
    }

    /// <summary>
    /// True when the name (any tier) resolves to one of the eight weapons the game can equip.
    /// A name the build does not know has no icon and no in-run weapon, so the inventory skips it.
    /// </summary>
    public static bool IsKnownWeapon(string weaponName)
    {
        if (string.IsNullOrEmpty(weaponName))
            return false;

        return GetDetieredWeaponName(weaponName) switch
        {
            "ryoshi katana" => true,
            "blessed blade" => true,
            "tactician's claymore" => true,
            "gladiator's greatsword" => true,
            "viper" => true,
            "sandcrawler's sniper rifle" => true,
            "adept's repeater" => true,
            "underdog meda-gun" => true,
            _ => false,
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
        // T1/T2 sit in the weaponIcons atlas, T3 are single sprites in weaponIconsT3 (v24).
        // A missing icon must never throw: this runs inside AbilitySwitcher.Start, and an
        // exception there leaves the F slot without a weapon for the whole run.
        var weapIcon = Resources.LoadAll<Sprite>("NFTIcons/weaponIcons").FirstOrDefault(x => string.Equals(x.name, weaponName, StringComparison.OrdinalIgnoreCase));
        if (weapIcon == null)
            weapIcon = Resources.LoadAll<Sprite>("NFTIcons/weaponIconsT3").FirstOrDefault(x => string.Equals(x.name, weaponName, StringComparison.OrdinalIgnoreCase));
        if (weapIcon == null)
            weapIcon = Resources.LoadAll<Sprite>("NFTIcons/weaponIcons").FirstOrDefault(x => string.Equals(x.name, GetDetieredWeaponName(weaponName ?? ""), StringComparison.OrdinalIgnoreCase));

        if (weapIcon != null && WeaponIcon != null)
            WeaponIcon.sprite = weapIcon;
        else
            Debug.LogWarning($"[WeaponButtonActivator] no icon for weapon '{weaponName}'");
    }
}
