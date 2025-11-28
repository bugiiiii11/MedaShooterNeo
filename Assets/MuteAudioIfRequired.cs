using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MuteAudioIfRequired : MonoBehaviour
{
    private float defaultVolume = 1;
    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        defaultVolume = source.volume;
    }

    private void OnEnable()
    {
        if(!GameManager.instance.AreSoundEffectsAllowed)
        {
            source.volume = 0;
        }
        else
        {
            source.volume = defaultVolume;
        }
    }
}
