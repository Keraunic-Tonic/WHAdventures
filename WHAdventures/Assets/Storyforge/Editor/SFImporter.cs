using System.IO;

using UnityEngine;

[UnityEditor.AssetImporters.ScriptedImporter(0 /* Version number. Increment when script is changed. */,
                  new[] { "sfp", "sfc", "sfs" } /* Filename extensions */)]
public class SFImporter : UnityEditor.AssetImporters.ScriptedImporter
{
    public override void OnImportAsset(UnityEditor.AssetImporters.AssetImportContext ctx)
    {
        var asset = new TextAsset(File.ReadAllText(ctx.assetPath));
        ctx.AddObjectToAsset("Main Object", asset);
        ctx.SetMainObject(asset);
    }
}
