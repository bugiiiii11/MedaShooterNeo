using CodeStage.AntiCheat.ObscuredTypes;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AbilityShotAnimator
{
    public string AbilityName;
    public RuntimeAnimatorController Controller;
}

public class AbilitySwitcher : MonoBehaviour
{
    public List<OhShitButtonActivator> Buttons;
    public List<AbilityShotAnimator> AbilityControllers;

    public WeaponButtonActivator WeaponButton;

    public RectTransform ContentForButtons;

    private OhShitButtonActivator activeAbility = null;

    private void Start()
    {
        var ppi = PlayerProfileInfo.instance;
        if (ppi)
        {
            var isStaker = ppi.IsUserStaker; // || ppi.EquippedHero != null;
            if (isStaker)
            {
                var shield = Buttons.FirstOrDefault(x => x.AbilityConfig.AbilityName == "Shield");
                shield.gameObject.SetActive(true);
                shield.transform.SetParent(ContentForButtons);
            }

            if (ppi.EquippedHero != null)
            {
                var equippedAbility = ppi.NftHandler.GetAbilityForFraction(((NftHero)ppi.EquippedHero).Fraction);
                if (equippedAbility != null)
                {
                    var ability = Buttons.FirstOrDefault(x => x.AbilityConfig.AbilityName == equippedAbility.AbilityName);
                    ability.gameObject.SetActive(true);
                    ability.transform.SetParent(ContentForButtons);
                    activeAbility = ability;
                }
            }
        }

        if (ppi && ppi.EquippedWeapon != null)
        {
            if (WeaponButton)
            {
                WeaponButton.gameObject.SetActive(true);
                WeaponButton.weaponName = ppi.EquippedWeapon.Name;
                WeaponButton.SetIcon(WeaponButton.weaponName);
                WeaponButton.transform.SetParent(ContentForButtons);
            }
        }
    }

    internal void SetAbilityCooldownFactor(float cooldownReductionFactor)
    {
        activeAbility.AbilityConfig.Cooldown = Mathf.RoundToInt(cooldownReductionFactor * activeAbility.AbilityConfig.Cooldown);
    }
}
