using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using Sirenix.OdinInspector;
using System.Linq;

public class DatabaseManager : OdinMenuEditorWindow
{
    [MenuItem("Utils/Database Manager")]
    private static void OpenWindow()
    {
        GetWindow<DatabaseManager>().Show();
    }

    //[MenuItem("Utils/Recalculate ID")]
    public static void RecalculateIds()
    {
        var curId = 0;
        foreach (var obj in Selection.GetFiltered<PerkBehaviour>(SelectionMode.Assets))
        {
            obj.Id = curId++;
        }
    }

    protected override OdinMenuTree BuildMenuTree()
    {
        var tree = new OdinMenuTree();
        tree.Selection.SupportsMultiSelect = false;

        tree.Add("Perk Database", new PerkEditor());
        tree.Add("Powerup Database", new PowerupEditor());
        tree.Add("Drop Database", new DropDatabase());
        tree.Add("Damage Calculator", new DamageCalculator());
        return tree;
    }
}

public class DropDatabase : PopulatableArray<DropDatabaseAsset>
{
    [HorizontalGroup, Button("Add Drop", ButtonSizes.Medium)]
    public override void AddItem()
    {
        Drops.Add(new Drop());
    }

    public List<Drop> Drops => LoadedAsset.Drops;
}

public class PerkEditor : PopulatableArray<PerkDatabaseAsset>
{
    [HorizontalGroup, Button("Recalculate ID", ButtonSizes.Medium)]
    public void RecalculateId()
    {
        /*DatabaseManager.RecalculateIds();*/
        var currId = 0;
        foreach(var perk in Perks)
        {
            var asset = perk.Behaviour;
            if (asset)
            {
                EditorUtility.SetDirty(asset);

                asset.Id = currId++;
            }
        }
        EditorUtility.DisplayDialog("ID Recaluclated", "IDs of perks in DB were recalculated.", "Ok");
    }

    [HorizontalGroup, Button("Add Perk", ButtonSizes.Medium)]
    public override void AddItem()
    {
        Perks.Add(new Perk());
    }

    public List<Perk> Perks => LoadedAsset.Perks;
}

public class PowerupEditor : PopulatableArray<PowerupDatabaseAsset>
{
    [HorizontalGroup, Button("Add Powerup", ButtonSizes.Medium)]
    public override void AddItem()
    {
        Powerups.Add(new Powerup());
    }

    public List<Powerup> Powerups => LoadedAsset.Powerups;
}

public abstract class PopulatableArray<T> : AssetLoader<T> where T: ScriptableObject
{
    public abstract void AddItem();
}