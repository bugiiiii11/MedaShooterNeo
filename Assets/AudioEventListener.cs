using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public class SourceEvent: UnityEvent<AudioSource>
{ }

public class AudioEventListener : MonoBehaviour
{
    public AudioSource source;
    private void Start()
    {
        GameManager.instance.EventManager.AddListener<SoundAndMusicSettingsEvent>(OnEvent);
    }

    private void OnEvent(SoundAndMusicSettingsEvent obj)
    {
        if (obj.MusicEnabled)
            source.volume = 1;
        else
            source.volume = 0;
    }
}
