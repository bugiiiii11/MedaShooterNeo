using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PersistantUtils : Singleton<PersistantUtils>
{
    public List<Action> Actions = new();

    private void Start()
    {
        SceneManager.sceneLoaded += SceneManager_sceneLoaded;
    }

    private void SceneManager_sceneLoaded(Scene arg0, LoadSceneMode arg1)
    {
        if (Actions.Count > 0)
        {
            foreach (var action in Actions)
            {
                action.Invoke();
            }
        }

        Actions.Clear();
    }

    public static void ReloadSceneAndInvoke(Action action)
    {
        instance.Actions.Add(action);

        //reload
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
