using UnityEditor;
using UnityEngine;

public static class RetroDimensionsPackageExporter
{
    private const string PackageRoot =
        "Assets/RetroDimensions/PSXTextureCruncher";

    [MenuItem(
        "Tools/Retro Dimensions/Export PSX Texture Cruncher"
    )]
    public static void ExportPackage()
    {
        string outputPath =
            EditorUtility.SaveFilePanel(
                "Export PSX Texture Cruncher",
                "",
                "PSX_Texture_Cruncher_v1.0.0",
                "unitypackage"
            );

        if (string.IsNullOrEmpty(outputPath))
            return;

        AssetDatabase.ExportPackage(
            PackageRoot,
            outputPath,
            ExportPackageOptions.Recurse
        );

        EditorUtility.DisplayDialog(
            "Retro Dimensions",
            "PSX Texture Cruncher package exported successfully.",
            "OK"
        );
    }
}