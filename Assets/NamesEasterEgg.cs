using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NamesEasterEgg : Singleton<NamesEasterEgg>
{
    public bool IsActive = false;
    public string[] Names;
    public GameObject Prefab;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKey(KeyCode.LeftControl))
            {
                if (Input.GetKeyDown(KeyCode.F))
                {
                    IsActive = !IsActive;
                }
            }
        }
    }
}
