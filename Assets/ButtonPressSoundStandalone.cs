using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ButtonPressSoundStandalone : ButtonPressSound
{
    public bool UseEnter = true;
    public bool NonAttached = false;
    public AudioClip press, enter;

    protected override void PointerEnter()
    {
        if (!UseEnter)
            return;

        var source = GetComponent<AudioSource>();
        source.clip = enter;
        source.Play();
    }

    protected override void Clicked()
    {
        if(NonAttached)
        {
            OneShotAudioPool.SpawnOneShot(press, 0.727f);
            return;
        }

        var source = GetComponent<AudioSource>();
        source.clip = press;
        source.Play();
    }
}
