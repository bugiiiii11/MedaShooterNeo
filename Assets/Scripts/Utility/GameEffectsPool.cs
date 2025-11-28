using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameEffectsPool : Singleton<GameEffectsPool>
{
    public LevelProps props;

    void Start()
    {
        PoolManager.WarmPool(props.EnemyDeathNormal, 3);
        PoolManager.WarmPool(props.EnemyDeathElectric, 2);
        PoolManager.WarmPool(props.ShieldAbsorbEffect, 2);
        PoolManager.WarmPool(props.SnapExplosion, 3);
    }

    private IEnumerator ReturnToPoolAfterTime(float time, GameObject obj)
    {
        yield return new WaitForSeconds(time);
        PoolManager.ReleaseObject(obj);
    }

    public static void SpawnShieldAbsorb(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.ShieldAbsorbEffect, pos, Quaternion.identity);
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(aliveSeconds, explosion));
    }

    public static void SpawnNormalExplosion(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathNormal, pos, Quaternion.identity);
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(aliveSeconds, explosion));
    }

    public static void SpawnNormalExplosionMuted(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathNormal, pos, Quaternion.identity);
        explosion.GetComponent<AudioSource>().volume = 0;
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(aliveSeconds, explosion));
    }

    public static void SpawnSnapMuted(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.SnapExplosion, pos, Quaternion.identity);
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(aliveSeconds, explosion));
    }

    public static void SpawnElectricExplosion(Vector3 pos, float aliveSeconds)
    {
        var explosion = PoolManager.SpawnObject(instance.props.EnemyDeathElectric, pos, Quaternion.identity);
        instance.StartCoroutine(instance.ReturnToPoolAfterTime(aliveSeconds, explosion));
    }
}
