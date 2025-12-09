using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DifficultyScaling : Singleton<DifficultyScaling>
{
    public static float CurrentDynamicFactor => instance.currentDynamicFactor;
    public static float CurrentCriticalDynamicFactor => instance.currentCriticalDynamicFactor;

    public static float CurrentEnemyDynamicFactor => instance.currentEnemyDynamicFactor;

    private float currentDynamicFactor = 1;
    private float currentEnemyDynamicFactor = 1;
    private float currentCriticalDynamicFactor = 1;
    private int currentIndex = 1;

    public override void Awake()
    {
        base.Awake();
        GameManager.instance.EventManager.AddListener<NextWaveEvent>(OnNextWaveSpawned);
    }

    public void OnNextWaveSpawned(NextWaveEvent wave)
    {
        if (wave.IsSilent)
            return;

        // recalculate dynamic factor
        currentIndex++;
        currentDynamicFactor = PlayerDamageFactor(currentIndex); //Mathf.Pow(currentIndex, 3 / 2.3f);

        // recalculate crits
        currentCriticalDynamicFactor = PlayerCritFactor(currentIndex); //Mathf.Pow(currentIndex, 3 / 2.5f);

        // calc for enemy
        currentEnemyDynamicFactor = EnemyDamageFactor(currentIndex);
    }

    public static float PlayerDamageFactor(int wave) => Mathf.Pow(wave, 3 / 2.3f);

    public static float EnemyDamageFactor(int wave)
    {
        float baseFactor = Mathf.Pow(wave, 3 / 2.02f);

        // After wave 30, enemies get progressively stronger
        if (wave > 30)
        {
            // Additional 5% increase per wave after 30
            float bonusMultiplier = 1f + (wave - 30) * 0.05f;
            return baseFactor * bonusMultiplier;
        }

        return baseFactor;
    }

    public static float PlayerCritFactor(int wave) => Mathf.Pow(wave, 3 / 2.4f);
}
