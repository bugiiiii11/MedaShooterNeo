using System;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
	public bool logStatus;
	public Transform root;

	private Dictionary<GameObject, ObjectPool<GameObject>> prefabLookup;
	private Dictionary<GameObject, ObjectPool<GameObject>> instanceLookup;

#if UNITY_EDITOR
	private bool dirty = false;
#endif
	public override void Awake () 
	{
		base.Awake();
		prefabLookup = new Dictionary<GameObject, ObjectPool<GameObject>>();
		instanceLookup = new Dictionary<GameObject, ObjectPool<GameObject>>();
	}

#if UNITY_EDITOR
	void Update()
	{
		if(logStatus && dirty)
		{
			PrintStatus();
			dirty = false;
		}
	}
#endif

	public void warmPool(GameObject prefab, int size)
	{
		if(prefabLookup.ContainsKey(prefab))
		{
			throw new Exception("Pool for prefab " + prefab.name + " has already been created");
		}
		var pool = new ObjectPool<GameObject>(() => { return InstantiatePrefab(prefab); }, size);
		prefabLookup[prefab] = pool;

#if UNITY_EDITOR
		dirty = true;
#endif
	}

	public GameObject spawnObject(GameObject prefab)
	{
		return spawnObject(prefab, Vector3.zero, Quaternion.identity);
	}

	public GameObject spawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		if (!prefabLookup.ContainsKey(prefab))
		{
			WarmPool(prefab, 1);
		}

		var pool = prefabLookup[prefab];

		var clone = pool.GetItem();
		clone.transform.SetPositionAndRotation(position, rotation);
		clone.SetActive(true);

		instanceLookup.Add(clone, pool);
#if UNITY_EDITOR
		dirty = true;
#endif
		return clone;
	}

	public void releaseObject(GameObject clone)
	{
		clone.SetActive(false);

		if(instanceLookup.ContainsKey(clone))
		{
			instanceLookup[clone].ReleaseItem(clone);
			instanceLookup.Remove(clone);
#if UNITY_EDITOR
			dirty = true;
#endif

		}
		else
		{
			Debug.LogWarning("No pool contains the object: " + clone.name);
		}
	}


	private GameObject InstantiatePrefab(GameObject prefab)
	{
		var go = Instantiate(prefab) as GameObject;
		if (root != null)
		{
			go.transform.SetParent(root);
		}
		return go;
	}

	public void PrintStatus()
	{
		foreach (KeyValuePair<GameObject, ObjectPool<GameObject>> keyVal in prefabLookup)
		{
			Debug.Log(string.Format("Object Pool for Prefab: {0} In Use: {1} Total {2}", keyVal.Key.name, keyVal.Value.CountUsedItems, keyVal.Value.Count));
		}
	}

	#region Static API

	public static void WarmPool(GameObject prefab, int size)
	{
		instance.warmPool(prefab, size);
	}

	public static GameObject SpawnObject(GameObject prefab)
	{
		return instance.spawnObject(prefab);
	}

	public static GameObject SpawnObject(GameObject prefab, Vector3 position, Quaternion rotation)
	{
		return instance.spawnObject(prefab, position, rotation);
	}

	public static void ReleaseObject(GameObject clone)
	{
		instance.releaseObject(clone);
	}

	#endregion
}

