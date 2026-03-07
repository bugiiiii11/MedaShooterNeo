using UnityEngine;

public class Singleton<Instance> : MonoBehaviour where Instance : Singleton<Instance>
{
    public static Instance instance;

    [SerializeField]
    [Tooltip("If true, this object will persist between scene loads using DontDestroyOnLoad")]
    public bool isPersistant = false;

    public virtual void Awake()
    {
        if (isPersistant)
        {
            if (!instance)
            {
                instance = this as Instance;
            }
            else
            {
                Destroy(gameObject);
            }
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            instance = this as Instance;
        }
    }
}
