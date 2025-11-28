using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class AssetLoader<T> where T : ScriptableObject
{
    [InlineEditor]
    public T LoadedAsset;

    public AssetLoader()
    {
        var dest = $"Assets/Data/{typeof(T).Name}.asset";
        LoadedAsset = AssetDatabase.LoadAssetAtPath<T>(dest);

        if (!LoadedAsset)
        {
            var pda = ScriptableObject.CreateInstance<T>();
            dest = AssetDatabase.GenerateUniqueAssetPath(dest);
            AssetDatabase.CreateAsset(pda, dest);
            AssetDatabase.Refresh();
            Selection.activeObject = pda;
        }
    }
}