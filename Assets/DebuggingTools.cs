using Cryptomeda.Minigames.BackendComs;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DebuggingTools : MonoBehaviour
{
    public TextMeshProUGUI WaveSliderText;
    private int currentWaveSkip = 0;

    public TMP_Dropdown actionDropown, skinDropdown;

    public void OnWaveSliderChanged(float value)
    {
        currentWaveSkip = Mathf.RoundToInt(value);
        WaveSliderText.text = currentWaveSkip.ToString();
    }

    public void OnSkipWaveButtonPressed()
    {
        PersistantUtils.ReloadSceneAndInvoke(() =>
        {
            GameManager.instance.EnemySpawner.Headstart(currentWaveSkip);
        });
    }

    public void OnChangeSkinActionSelected(int index)
    {
        var text = skinDropdown.options[index].text;
        var playerAvatar = GameManager.instance.Player.GetComponent<F3DCharacterAvatar>();
        playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Sniper;

        if (text == "Default Player")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Cryptomeda;
        }
        else if(text == "Sniper")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Sniper;
        }
        else if (text == "Basic Enemy")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Basic;
        }
        else if (text == "Zombiechad")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Zombiechad;
        }
        else if (text == "Boss")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.FlailBoss;
        }
        else if (text == "Tank")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Tank;
        }
        else if (text == "Heavy")
        {
            playerAvatar.SkinUsed = F3DCharacterAvatar.SkinName.Multishooter;
        }
        playerAvatar.SwitchToChar();
    }

    public void OnActionDropdownButtonPressed(int index)
    {
        //var index = actionDropown.value;
        var text = actionDropown.options[index].text;
        
        if(text == "Spawn Assault Rifle")
        {
            var powerupSpawner = FindObjectOfType<PowerupSpawner>();
            var myPowerup = powerupSpawner.Powerups.Powerups.Find(x => x.Title == "Assault Rifle");
            var pos = Vector3.Lerp(LevelInfo.instance.BoundLowerLeft.position, LevelInfo.instance.BoundUpperRight.position, 0.5f);
            pos.z = 0;
            Instantiate(myPowerup.AssetPrefab, pos, Quaternion.identity);
        }
        else if (text == "Spawn Tesla Gun")
        {
            var powerupSpawner = FindObjectOfType<PowerupSpawner>();
            var myPowerup = powerupSpawner.Powerups.Powerups.Find(x => x.Title == "Tesla Gun");
            var pos = Vector3.Lerp(LevelInfo.instance.BoundLowerLeft.position, LevelInfo.instance.BoundUpperRight.position, 0.5f);
            pos.z = 0;
            Instantiate(myPowerup.AssetPrefab, pos, Quaternion.identity);
        }
        else if (text == "Spawn Shield Powerup")
        {
            var powerupSpawner = FindObjectOfType<PowerupSpawner>();
            var myPowerup = powerupSpawner.Powerups.Powerups.Find(x => x.Title == "Shield Refill");
            var pos = Vector3.Lerp(LevelInfo.instance.BoundLowerLeft.position, LevelInfo.instance.BoundUpperRight.position, 0.5f);
            pos.z = 0;
            Instantiate(myPowerup.AssetPrefab, pos, Quaternion.identity);
        }
        else if (text == "Select random perks")
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3));
        }
        else if (text == "Select rare+ perks")
        {
            GameManager.instance.EventManager.Dispatch(new PerkSelectionEvent(3, new Rarity[] { Rarity.Epic, Rarity.Rare }));
        }

        actionDropown.value = 0;
    }

    public void SetDebuggingTools(bool active)
    {
        if (active)
            GetComponent<Animation>().Play("debuggintools_show");
        else
            GetComponent<Animation>().Play("debuggintools_hide");
    }

    public void SendPostRequest(TextMeshProUGUI textResult)
    {
        var score = Random.Range(1000, 3000);
        var encryptedScore = DataEncryption.EncryptScore((uint)score);
        var m = Asymmetric.RSA.Encrypt(encryptedScore.ToString(), DataEncryption.PUBLIC_KEY);

        Debug.Log($"Sending score {score} (hashed={encryptedScore}, rsa={m}) to backend.");
        textResult.text = $"Sending score {score} (hashed={encryptedScore}, rsa={m}) to backend.";

        RestfulManager.Post(RestfulEndpoint.ScoreAndAddress, "{\"hash\":\"" + m + "\",\"address\":\"0x3d7329DD6FA5a982ce13cD5aB70362Ed93be146B\"}", (str) =>
        {
            Debug.Log($"[POST Result] {str}");
            textResult.text += "\n\n" + str;
        });
    }

    public void GivePerk(string perkName)
    {
        var allPerks = UIPerkManager.instance.PerkDatabase.Perks;
        var toGive = allPerks.Find(x => string.Equals(x.Title, perkName, System.StringComparison.OrdinalIgnoreCase));
        UIPerkManager.instance.ApplyPerk(toGive);
    }
}
