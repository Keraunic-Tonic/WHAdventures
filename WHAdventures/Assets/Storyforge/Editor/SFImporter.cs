using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

[ScriptedImporter(0 /* Version number. Increment when script is changed. */,
                  new[] { "sfp", "sfc", "sfs" } /* Filename extensions */)]
public class SFImporter : ScriptedImporter
{
    public override void OnImportAsset(AssetImportContext ctx)
    {
        var asset = new TextAsset(File.ReadAllText(ctx.assetPath));
        ctx.AddObjectToAsset("Main Object", asset);
        ctx.SetMainObject(asset);
    }
}
