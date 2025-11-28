using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialControls : MonoBehaviour
{
    private void Start()
    {
        if (PlayerPrefs.HasKey("showTutorial"))
        {
            PlayerPrefs.DeleteKey("showTutorial");
        }

        if (PlayerPrefs.HasKey("showTutorial1"))
        {
            gameObject.SetActive(false);
        }
        else
        {
            GameManager.instance.PauseGame(true);

            var cgroup = GetComponent<CanvasGroup>();
            cgroup.alpha = 1;
            cgroup.blocksRaycasts = true;
            PlayerPrefs.SetInt("showTutorial1", 0);
        }
    }
}
