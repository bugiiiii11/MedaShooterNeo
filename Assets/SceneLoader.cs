using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadLevel(int level)
    {
        SceneManager.LoadScene(level);
    }

    public void LoadLevel2(int level)
    {
        PlayerPrefs.SetInt("IsLevel2", 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(level);
    }
}
