
using Sirenix.OdinInspector;
using System.Linq;
using UnityEngine;

public class DamageCalculator
{
    public Weapon Weapon;

    [Range(0, 100)]
    public int Wave;

    [Button("Calculate", ButtonSizes.Large)]
    public void Calculate()
    {
        if (!Weapon)
            return;

        var difficulty = GameObject.FindObjectOfType<DifficultyScaling>();
        var player = Weapon.transform.root.GetComponent<PlayerMovement>();
        var weaponController = player.GetComponent<WeaponController>();

        var baseWeaponDamage = Weapon.DamageRange;
        var baseWeaponCrit = Weapon.Data.AdditionalData.CricitalChanceBonusDamage;
        var baseWeaponCritChance = Weapon.Data.AdditionalData.CriticalChance;
        var fireRate = Weapon.FireRate;
        var damageUpgradeFactor = DifficultyScaling.PlayerDamageFactor(Wave);
        var damageCritFactor = DifficultyScaling.PlayerCritFactor(Wave);

        var weaponRelatedUpgrades = player.PlayerUpgrades.AllUpgrades.ToList().Find(x => x.WeaponType == Weapon.TypeOfWeapon);

        var weaponRelatedDamageUpgrade = weaponRelatedUpgrades.DamageIncreasePerWave;
        var weaponRelatedCritUpgrade = weaponRelatedUpgrades.CriticalDamageIncreasePerWave;

        var baseDamageUpgraded = new Vector2(baseWeaponDamage.x + damageUpgradeFactor * weaponRelatedDamageUpgrade,
                                             baseWeaponDamage.y + damageUpgradeFactor * weaponRelatedDamageUpgrade);

        var critDamageUpgraded = new Vector2(baseWeaponCrit.x + damageCritFactor * weaponRelatedCritUpgrade,
                                             baseWeaponCrit.y + damageCritFactor * weaponRelatedCritUpgrade);

        // simulate dps
        var iterations = 1500;
        var randomizedDps = 0f;
        for (var iter = 0; iter < iterations; iter++)
        {
            randomizedDps += baseDamageUpgraded.Random();
            if (Random.Range(0, 1f) < baseWeaponCritChance)
            {
                randomizedDps += critDamageUpgraded.Random();
            }
        }

        randomizedDps /= iterations;
        randomizedDps /= fireRate;

        Result = $"Weapon Stats:\nBase damage = {baseWeaponDamage}\nBase Crit = {baseWeaponCrit}\nCrit chance = {baseWeaponCritChance}\n\nUpgraded:\nBase damage = {baseDamageUpgraded}\nBase Crit = {critDamageUpgraded}\n\nSimulation:\nDPS = {randomizedDps}";

        /*
        var damage = Weapon.DamageRange;
        var player = Weapon.transform.root.GetComponent<PlayerMovement>();
        var critBonus = new Vector2Int();
        var critX = Weapon.Data.AdditionalData.CricitalChanceBonusDamage.x + player.PlayerStats.Modifiers.CriticalChanceIncreaseFromPerks;
        var critY = Weapon.Data.AdditionalData.CricitalChanceBonusDamage.y + player.PlayerStats.Modifiers.CriticalChanceIncreaseFromPerks;
        critBonus.x = critX;
        critBonus.y = critY;

        var upgrades = player.PlayerUpgrades;

        var currentDynamicFactor = Mathf.Pow(Wave, 3 / 2.3f);
        var currentCriticalDynamicFactor = Mathf.Pow(Wave, 3 / 2.5f);

        //var upgradedDamage = new Vector2(damage.x + upgrades.DamageIncreasePerWave * currentDynamicFactor, damage.y + upgrades.DamageIncreasePerWave * currentDynamicFactor);
        // var upgradedCrit = new Vector2(critBonus.x + upgrades.CriticalDamageBonus * currentCriticalDynamicFactor, critBonus.y + upgrades.CriticalDamageBonus * currentCriticalDynamicFactor);
        var upgradedModule = UpgradeModule.Scale(upgrades, currentDynamicFactor, currentCriticalDynamicFactor);
        var upgradedDamage = new Vector2(damage.x + upgradedModule.DamageIncreasePerWave, damage.y + upgradedModule.DamageIncreasePerWave);
        var upgradedCrit = new Vector2(critBonus.x + upgrades.CriticalDamageIncreasePerWave, critBonus.y + upgradedModule.CriticalDamageIncreasePerWave);

        float fullDamage = 0, fullCritDamage = 0, rndDamage=0;
        var averageIterations = 500;
        for (int i = 0; i < averageIterations; i++)
        {
            var c = (upgradedDamage.Random() + upgradedCrit.Random());
            var d = upgradedDamage.Random();

            fullDamage += d;
            fullCritDamage += c;

            if (Random.Range(0, 1f) < Weapon.Data.AdditionalData.CriticalChance)
                rndDamage += c;
            else rndDamage += d;
        }

        fullDamage /= averageIterations;
        fullCritDamage /= averageIterations;
        rndDamage /= averageIterations;

        Result = $"BD={upgradedDamage}\nCritBonus={upgradedCrit}\nNormalAttackRnd={upgradedDamage.Random()}\nCritAttackRnd={fullDamage}\nDPS={fullDamage/Weapon.FireRate}\nDPS crit={fullCritDamage / Weapon.FireRate}\nsimulated dps: {rndDamage/Weapon.FireRate}";
  */
    }

    [Multiline(14), DisplayAsString(false)]
    public string Result;
}
