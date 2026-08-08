using System.Collections;
using System.Collections.Generic;
using Determinism;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    public void LoadLevel(int level)
    {
        SceneManager.LoadScene(level);
    }

    /// <summary>
    /// Legacy shim (Phase 3). The hidden startgame_level2 button in
    /// inventory.unity carries a serialized PersistentCall to this method, so
    /// it must keep existing -- but the old one-shot IsLevel2 pref protocol is
    /// gone (Retry used to silently drop the selection back to Level 1).
    /// Routing it through the sticky selection makes the button strictly
    /// better than it was.
    /// </summary>
    public void LoadLevel2(int level)
    {
        MsLevelSelect.SetSelectedLevel(2);
        LoadLevel(level);
    }

    /// <summary>Selector, not a launcher: sets the sticky level and stays on
    /// the current scene. The START button launches whatever is selected.</summary>
    public void SelectLevel(int level)
    {
        MsLevelSelect.SetSelectedLevel(level);
    }

    /// <summary>Launches today's daily-challenge run (one-shot flag; the
    /// gameplay scene consumes it, so Retry falls back to a normal run).</summary>
    public void LoadDaily(int scene)
    {
        MsLevelSelect.ArmDaily();
        SceneManager.LoadScene(scene);
    }
}
