using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OneShotAudioPool : Singleton<OneShotAudioPool>
{
    void Start()
    {
        PoolManager.WarmPool(LevelProps.instance.OneShotClip, 4);
    }

    private IEnumerator ReturnToPoolAfterTime(float time, GameObject obj)
    {
        yield return new WaitForSeconds(time);
        PoolManager.ReleaseObject(obj);
    }

    public static void SpawnOneShot(AudioClip clip, float volume = 0.7f, float pitch = 1, float secondsAlive = 1.5f)
    {
        if (!GameManager.instance.AreSoundEffectsAllowed)
            return;

        var shot = PoolManager.SpawnObject(LevelProps.instance.OneShotClip, Vector3.zero, Quaternion.identity);
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(secondsAlive, shot));

        var source = shot.GetComponent<AudioSource>();
        source.volume = volume;
        source.pitch = pitch;
        source.clip = clip;
        source.Play();
    }
}
