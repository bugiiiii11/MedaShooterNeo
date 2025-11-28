using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

public class CardSpriteGenerator
{
    [MenuItem("Utils/Generate SpriteAtlas")]
    public static void CreateAtlasForSelectedSprites()
    {
        var isSelected = Selection.objects.Length > 0;
        var path = "Assets";
        var sel = Selection.objects[0];
        if (isSelected && sel)
        {
            path = AssetDatabase.GetAssetPath(sel.GetInstanceID());
            var split = path.Split('/');
            var without = split.Take(split.Length - 1);
            path = without.Aggregate("", (a,b) => a + "/" + b);
            path = path.Remove(0, 1);
            path = path[0].ToString().ToUpper() + path.Substring(1);
        }

        var selName = sel ? sel.name.Split('_')[0] : "sample";

        SpriteAtlas sa = new SpriteAtlas();
        AssetDatabase.CreateAsset(sa, $"{path}/{selName}_Atlas.spriteatlas");
        foreach (var obj in Selection.objects)
        {
            Object o = obj as Texture2D;
            if (o != null)
                SpriteAtlasExtensions.Add(sa, new Object[] { o });
        }
        AssetDatabase.SaveAssets();

        // now generate animator
        var controller = UnityEditor.Animations.AnimatorController.CreateAnimatorControllerAtPath($"{path}/{selName}_Controller.controller");
    }
}
