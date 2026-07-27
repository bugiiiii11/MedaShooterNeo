using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot Phase 0 cleanup (MS 2.0, S199).
///
/// The ReneVerse ad SDK was removed at source: its package, the VideoAdUi script and
/// the "Video Ad Surface" prefab are gone. What remains is the prefab INSTANCE inside
/// develop_overhaul.unity -- a RawImage on a RenderTexture nothing ever writes to, which
/// is why it painted a black rectangle over the give-up confirm popup. The shipped v4
/// data file worked around that by deactivating the object post-build; this removes it
/// properly so the patch does not have to be reapplied on every build.
///
/// Scene surgery goes through the editor rather than hand-edited YAML so Unity does the
/// re-serialization (prefab instance blocks, stripped transforms, parent m_Children).
///
/// Run once, verify, then this file can be deleted:
///   Unity.exe -batchmode -quit -projectPath . -executeMethod Ms2Cleanup.RemoveReneVerseAdObjects
/// </summary>
public static class Ms2Cleanup
{
    private static readonly string[] AdNamePrefixes = { "Video Ad Surface", "Video Ad" };

    [MenuItem("Build/Phase 0 - Remove ReneVerse Ad Objects")]
    public static void RemoveReneVerseAdObjects()
    {
        var scenePaths = EditorBuildSettings.scenes
            .Where(s => s.enabled)
            .Select(s => s.path)
            .ToArray();

        var removedTotal = 0;

        foreach (var path in scenePaths)
        {
            var scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            var removed = RemoveAdObjectsInScene(scene);

            if (removed > 0)
            {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"[Ms2Cleanup] {path}: removed {removed} ad object(s), scene saved.");
            }
            else
            {
                Debug.Log($"[Ms2Cleanup] {path}: no ad objects found.");
            }

            removedTotal += removed;
        }

        Debug.Log($"[Ms2Cleanup] DONE. Removed {removedTotal} ad object(s) across {scenePaths.Length} build scene(s).");

        if (Application.isBatchMode)
        {
            EditorApplication.Exit(0);
        }
    }

    private static int RemoveAdObjectsInScene(Scene scene)
    {
        // Collect first, destroy after: destroying while walking the hierarchy
        // invalidates the roots enumeration.
        var doomed = new List<GameObject>();

        foreach (var root in scene.GetRootGameObjects())
        {
            CollectAdObjects(root.transform, doomed);
        }

        foreach (var go in doomed)
        {
            Debug.Log($"[Ms2Cleanup] Destroying '{GetPath(go.transform)}'");
            Object.DestroyImmediate(go);
        }

        return doomed.Count;
    }

    private static void CollectAdObjects(Transform t, List<GameObject> doomed)
    {
        if (IsAdObject(t.gameObject))
        {
            // Whole subtree goes with it -- do not descend.
            doomed.Add(t.gameObject);
            return;
        }

        for (var i = 0; i < t.childCount; i++)
        {
            CollectAdObjects(t.GetChild(i), doomed);
        }
    }

    private static bool IsAdObject(GameObject go)
    {
        // With the prefab asset deleted, Unity keeps the instance as a placeholder that
        // still carries the original name -- match on that.
        return AdNamePrefixes.Any(prefix => go.name.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase));
    }

    private static string GetPath(Transform t)
    {
        var path = t.name;
        while (t.parent != null)
        {
            t = t.parent;
            path = t.name + "/" + path;
        }
        return path;
    }
}
