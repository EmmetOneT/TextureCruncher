using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PSXTextureCruncherWindow : EditorWindow
{
    private int maxTextureSize = 128;

    private bool usePointFiltering = true;
    private bool disableMipmaps = true;
    private bool disableAnisotropicFiltering = true;
    private bool disableCompression = true;

    private Vector2 scrollPosition;

    private readonly List<Texture2D> selectedTextures = new List<Texture2D>();

    [MenuItem("Tools/Retro Dimensions/PSX Texture Cruncher")]
    public static void ShowWindow()
    {
        PSXTextureCruncherWindow window =
            GetWindow<PSXTextureCruncherWindow>("PSX Texture Cruncher");

        window.minSize = new Vector2(420, 480);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "PSX Texture Cruncher",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Convert modern texture import settings into a retro PSX-style setup."
        );

        GUILayout.Space(15);

        DrawTextureSelection();

        GUILayout.Space(15);

        DrawCrunchSettings();

        GUILayout.Space(20);

        GUI.enabled = selectedTextures.Count > 0;

        if (GUILayout.Button("CRUNCH SELECTED", GUILayout.Height(40)))
        {
            CrunchTextures();
        }

        GUI.enabled = true;
    }

    private void DrawTextureSelection()
    {
        EditorGUILayout.LabelField(
            "Textures",
            EditorStyles.boldLabel
        );

        if (GUILayout.Button("Load Selected Textures"))
        {
            LoadSelectedTextures();
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Textures Loaded: " + selectedTextures.Count
        );

        scrollPosition = EditorGUILayout.BeginScrollView(
            scrollPosition,
            GUILayout.Height(150)
        );

        for (int i = 0; i < selectedTextures.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();

            selectedTextures[i] = (Texture2D)EditorGUILayout.ObjectField(
                selectedTextures[i],
                typeof(Texture2D),
                false
            );

            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                selectedTextures.RemoveAt(i);
                i--;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        if (selectedTextures.Count > 0)
        {
            if (GUILayout.Button("Clear List"))
            {
                selectedTextures.Clear();
            }
        }
    }

    private void DrawCrunchSettings()
    {
        EditorGUILayout.LabelField(
            "Crunch Settings",
            EditorStyles.boldLabel
        );

        string[] sizes =
        {
            "32",
            "64",
            "128",
            "256",
            "512"
        };

        int[] sizeValues =
        {
            32,
            64,
            128,
            256,
            512
        };

        maxTextureSize = EditorGUILayout.IntPopup(
            "Maximum Texture Size",
            maxTextureSize,
            sizes,
            sizeValues
        );

        GUILayout.Space(5);

        usePointFiltering = EditorGUILayout.Toggle(
            "Point Filtering",
            usePointFiltering
        );

        disableMipmaps = EditorGUILayout.Toggle(
            "Disable Mipmaps",
            disableMipmaps
        );

        disableAnisotropicFiltering = EditorGUILayout.Toggle(
            "Disable Anisotropic Filtering",
            disableAnisotropicFiltering
        );

        disableCompression = EditorGUILayout.Toggle(
            "Disable Compression",
            disableCompression
        );
    }

    private void LoadSelectedTextures()
    {
        selectedTextures.Clear();

        Object[] selection = Selection.objects;

        foreach (Object selectedObject in selection)
        {
            Texture2D texture = selectedObject as Texture2D;

            if (texture != null && !selectedTextures.Contains(texture))
            {
                selectedTextures.Add(texture);
            }
        }
    }

    private void CrunchTextures()
    {
        int processedTextures = 0;

        foreach (Texture2D texture in selectedTextures)
        {
            if (texture == null)
                continue;

            string assetPath = AssetDatabase.GetAssetPath(texture);

            TextureImporter importer =
                AssetImporter.GetAtPath(assetPath) as TextureImporter;

            if (importer == null)
                continue;

            importer.maxTextureSize = maxTextureSize;

            if (usePointFiltering)
            {
                importer.filterMode = FilterMode.Point;
            }

            if (disableMipmaps)
            {
                importer.mipmapEnabled = false;
            }

            if (disableAnisotropicFiltering)
            {
                importer.anisoLevel = 0;
            }

            if (disableCompression)
            {
                importer.textureCompression =
                    TextureImporterCompression.Uncompressed;

                importer.crunchedCompression = false;
            }

            importer.SaveAndReimport();

            processedTextures++;
        }

        EditorUtility.DisplayDialog(
            "PSX Texture Cruncher",
            processedTextures + " texture(s) crunched successfully.",
            "OK"
        );
    }
}