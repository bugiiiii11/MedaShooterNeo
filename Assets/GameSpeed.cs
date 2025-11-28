using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameSpeed : MonoBehaviour
{
    public float MaxGameSpeed = 1.75f;
    private void Update()
    {
        //increment speed by 0.01% each frame
        if (!GameManager.instance.IsGamePaused && !GameManager.instance.EnemySpawner.IsBossActive())
        {
            var value = GameConstants.Constants.GameSpeedMultiplier;
            if (value < MaxGameSpeed)
            {
                value += Time.deltaTime * 0.0016f;
                GameConstants.Constants.GameSpeedMultiplier = Mathf.Clamp(value, 1, 1.8f);
            }
        }
    }
}
