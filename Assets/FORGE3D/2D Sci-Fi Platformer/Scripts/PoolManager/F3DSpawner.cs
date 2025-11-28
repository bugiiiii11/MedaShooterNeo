using UnityEngine;
using System.Collections;

public class F3DSpawner : MonoBehaviour 
{
    public static Transform Spawn(Transform prefab, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;
        var spawnedObject = (Transform)Instantiate(prefab, position, rotation, parent);
//        spawnedObject.localScale = scale;

        return spawnedObject;
    }

    public static void Despawn(Transform obj)
    {
        if(obj != null)
            GameObject.Destroy(obj.gameObject);
    }

    public static void Despawn(GameObject obj)
    {
        if (obj != null)
            GameObject.Destroy(obj);
    }

    public static void Despawn(Transform obj, float time)
    {
        if (obj != null)
            GameObject.Destroy(obj.gameObject, time);
    }
}
