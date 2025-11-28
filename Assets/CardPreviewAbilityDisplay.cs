using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CardPreviewAbilityDisplay : MonoBehaviour
{
    public RectTransform HintRect;
    public CardPreviewAbility[] Abilities;
    public CardPreviewAbility[] WeaponAbilities;
    public PlayerBaseStats PlayerBaseStats;

    public void SetHintFor(Transform tr)
    {
        if (!tr)
            HintRect.gameObject.SetActive(false);
        else
        {
            HintRect.position = tr.position;
            HintRect.gameObject.SetActive(true);

            HintRect.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = tr.GetComponent<CardPreviewAbility>().HintTooltip;
        }
    }

    public void SetAbilitiesFor(NftHero nft)
    {
        foreach (var ab in WeaponAbilities)
            ab.SetActive(false);
        foreach (var ab in Abilities)
            ab.SetActive(true);

        if (nft.Attributes.Power == 0 && nft.Attributes.Innovation == 0 && nft.Attributes.Security == 0)
        {
            foreach (var ab in Abilities)
            {
                ab.incrementText.text = "<color=grey>-</color>";

                if (ab.Name == "perk" || ab.Name == "health")
                {
                    if (nft.Attributes.IsRevolution || nft.Fraction == NftFraction.Neutral)
                        ab.SetActive(true);
                    else
                        ab.SetActive(false);
                }
                
            }
        }
        else
        {
            var stats = nft.ConvertToBoostedStats();

            // Community and revolutions
            if(stats.EpicPerkDropChanceAddition != 0)
            {
                var perk = Get("perk");
                perk.SetActive(true);
                perk.incrementText.text = $"+{System.Math.Round(stats.EpicPerkDropChanceAddition,2)*100}%";
            }
            else
            {
                Get("perk").SetActive(false);
            }

            if (stats.MaxHealthFactor != 1)
            {
                var perk = Get("health");
                perk.SetActive(true);
                perk.incrementText.text = $"+{Mathf.RoundToInt(stats.MaxHealthFactor*PlayerBaseStats.Health- PlayerBaseStats.Health)}";
            }
            else
            {
                Get("health").SetActive(false);
            }
            //################################################################################################

            var a = Get("speed");
            a.incrementText.text = $"+{System.Math.Round((stats.PlayerSpeedFactor * PlayerBaseStats.MovementSpeed) - PlayerBaseStats.MovementSpeed, 2)}";
            a.SetActive(true);

            a = Get("armor");
            a.incrementText.text = $"+{stats.ShieldAddition}";
            a.SetActive(true);

            var heroAbility = PlayerProfileInfo.instance.NftHandler.GetAbilityForFraction(nft.Fraction);
            a = Get("cooldown");
            a.incrementText.text = $"{heroAbility.Cooldown - Mathf.RoundToInt(stats.CooldownReductionFactor * heroAbility.Cooldown)}s";
            a.SetActive(true);
        }

        var activeAbilities = Abilities.Count(x => x.gameObject.activeSelf);
        var newSize = 1.3f;
        if (activeAbilities > 3)
            newSize = 1.1624f;

        foreach (var ability in Abilities)
        {
            ability.gameObject.transform.localScale = Vector3.one * newSize;
        }
    }

    public void SetAbilitiesFor(NftWeapon nft)
    {
        foreach (var ab in Abilities)
            ab.SetActive(false);
        foreach (var ab in WeaponAbilities)
            ab.SetActive(true);

        if (nft.Power == 0 && nft.Innovation == 0 && nft.Security == 0)
        {
            foreach (var ab in WeaponAbilities)
            {
                ab.incrementText.text = "<color=grey>-</color>";
            }
        }
        else
        {
            var stats = nft.ConvertToBoostedStats();

            // Get("speed").incrementText.text = $"+{System.Math.Round((stats.PlayerSpeedFactor * PlayerBaseStats.MovementSpeed) - PlayerBaseStats.MovementSpeed, 2)}";
            var a = GetW("critchance");
            a.incrementText.text = $"+{Mathf.CeilToInt(stats.CriticalChanceIncrease * 100)}%";
            a.SetActive(true);

            a = GetW("damage");
            a.incrementText.text = $"+{Mathf.CeilToInt(PlayerBaseStats.Damage.y * 1.4f * stats.DamageFactor)}";
            a.SetActive(true);

            a = GetW("firerate");
            a.incrementText.text = $"+{Mathf.RoundToInt(60f / (PlayerBaseStats.FireRatePerc - stats.FireRateIncrease)) - Mathf.RoundToInt(60f / PlayerBaseStats.FireRatePerc) }";
            a.SetActive(true);
        }

        var activeAbilities = WeaponAbilities.Count(x => x.gameObject.activeSelf);
        var newSize = 1.3f;
        if (activeAbilities > 3)
            newSize = 1.1624f;

        foreach (var ability in WeaponAbilities)
        {
            ability.gameObject.transform.localScale = Vector3.one * newSize;
        }
    }

    private CardPreviewAbility Get(string n) => Abilities.First(x => x.Name == n);
    private CardPreviewAbility GetW(string n) => WeaponAbilities.First(x => x.Name == n);
}
