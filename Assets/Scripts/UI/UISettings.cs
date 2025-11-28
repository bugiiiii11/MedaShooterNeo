using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;


public class SoundAndMusicSettingsEvent
{
    public bool MusicEnabled, SoundEnabled, CountdownsEnabled;

    public SoundAndMusicSettingsEvent(bool musicEnabled, bool soundEnabled, bool countdowns)
    {
        MusicEnabled = musicEnabled;
        SoundEnabled = soundEnabled;
        CountdownsEnabled = countdowns;
    }
}

public class UISettings : MonoBehaviour
{
    private bool _soundEnabled = true, _musicEnabled = true, _countdownsEnabled = true;

    public bool SoundEnabled
    {
        get
        {
            return _soundEnabled;
        }
        set
        {
            _soundEnabled = value;
            PlayerPrefs.SetInt("soundEnabled", value ? 1 : 0);

            if(GameManager.instance)
                GameManager.instance.EventManager.Dispatch(new SoundAndMusicSettingsEvent(_musicEnabled, _soundEnabled, _countdownsEnabled));
        }
    }

    public bool MusicEnabled
    {
        get
        {
            return _musicEnabled;
        }
        set
        {
            _musicEnabled = value;
            PlayerPrefs.SetInt("musicEnabled", value ? 1 : 0);

            if(GameManager.instance)
                GameManager.instance.EventManager.Dispatch(new SoundAndMusicSettingsEvent(_musicEnabled, _soundEnabled, _countdownsEnabled));
        }
    }

    public bool CountdownsEnabled
    {
        get
        {
            return _countdownsEnabled;
        }
        set
        {
            _countdownsEnabled = value;
            PlayerPrefs.SetInt("countdownsEnabled", value ? 1 : 0);

            if(GameManager.instance)
                GameManager.instance.EventManager.Dispatch(new SoundAndMusicSettingsEvent(_musicEnabled, _soundEnabled, _countdownsEnabled));
        }
    }

    public static bool IsVisible { get; internal set; }

    [SerializeField]
    private Image musicButtonImage, soundButtonImage;

    [SerializeField]
    private Sprite soundIconEnabled, soundIconDisabled;
    [SerializeField]
    private Sprite musicIconEnabled, musicIconDisabled;


    [SerializeField]
    private Image countdownCheckmark;

    public void ShowSettings()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;
        canvasGroup.blocksRaycasts = true;
        IsVisible = true;
    }

    public void HideSettings()
    {
        var canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 0;
        canvasGroup.blocksRaycasts = false;
        IsVisible = false;
    }

    private void Start()
    {
        if (PlayerPrefs.HasKey("soundEnabled"))
        {
            SoundEnabled = PlayerPrefs.GetInt("soundEnabled") == 1;
        }
        else
        {
            PlayerPrefs.SetInt("soundEnabled", 1);
        }

        if (PlayerPrefs.HasKey("musicEnabled"))
        {
            MusicEnabled = PlayerPrefs.GetInt("musicEnabled") == 1;
        }
        else
        {
            PlayerPrefs.SetInt("musicEnabled", 1);
        }

        if (PlayerPrefs.HasKey("countdownsEnabled"))
        {
            CountdownsEnabled = PlayerPrefs.GetInt("countdownsEnabled") == 1;
        }
        else
        {
            PlayerPrefs.SetInt("countdownsEnabled", 1);
        }

        AdjustButtonSprites();

        IsVisible = false;
    }

    private void AdjustButtonSprites()
    {
        musicButtonImage.sprite = MusicEnabled ? musicIconEnabled : musicIconDisabled;
        soundButtonImage.sprite = SoundEnabled ? soundIconEnabled : soundIconDisabled;

        if(countdownCheckmark)
            countdownCheckmark.gameObject.SetActive(CountdownsEnabled);
    }

    public void ToggleSound()
    {
        SoundEnabled = !SoundEnabled;
        AdjustButtonSprites();
    }

    public void ToggleMusic()
    {
        MusicEnabled = !MusicEnabled;
        AdjustButtonSprites();
    }

    public void ToggleCountdowns()
    {
        CountdownsEnabled = !CountdownsEnabled;
        AdjustButtonSprites();
    }
}
