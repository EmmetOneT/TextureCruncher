using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PSXTextureCruncherWindow : EditorWindow
{
    private int maxTextureSize = 128;
    private int paletteColours = 64;

    private bool useDithering = true;
    private float ditherStrength = 0.65f;

    private float brightness = 0f;
    private float contrast = 1f;
    private float saturation = 1f;

    private bool usePointFiltering = true;
    private bool disableMipmaps = true;
    private bool disableAnisotropicFiltering = true;
    private bool disableCompression = true;

    private Vector2 scrollPosition;

    private readonly List<Texture2D> selectedTextures =
        new List<Texture2D>();

    [MenuItem(
        "Tools/Retro Dimensions/PSX Texture Cruncher"
    )]
    public static void ShowWindow()
    {
        PSXTextureCruncherWindow window =
            GetWindow<PSXTextureCruncherWindow>(
                "PSX Texture Cruncher"
            );

        window.minSize =
            new Vector2(430, 650);
    }

    private void OnGUI()
    {
        GUILayout.Space(10);

        EditorGUILayout.LabelField(
            "PSX Texture Cruncher",
            EditorStyles.boldLabel
        );

        EditorGUILayout.LabelField(
            "Convert modern textures into low-resolution PSX-style copies."
        );

        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "Original textures are preserved. Processed textures are saved beside the original with _PSX added to the filename.",
            MessageType.Info
        );

        GUILayout.Space(10);

        DrawTextureSelection();

        GUILayout.Space(15);

        DrawResolutionSettings();

        GUILayout.Space(15);

        DrawColourSettings();

        GUILayout.Space(15);

        DrawImportSettings();

        GUILayout.Space(20);

        GUI.enabled =
            selectedTextures.Count > 0;

        if (GUILayout.Button(
            "CRUNCH TO PSX COPY",
            GUILayout.Height(45)))
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

        if (GUILayout.Button(
            "Load Selected Textures"))
        {
            LoadSelectedTextures();
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField(
            "Textures Loaded: " +
            selectedTextures.Count
        );

        scrollPosition =
            EditorGUILayout.BeginScrollView(
                scrollPosition,
                GUILayout.Height(130)
            );

        for (int i = 0;
             i < selectedTextures.Count;
             i++)
        {
            EditorGUILayout.BeginHorizontal();

            selectedTextures[i] =
                (Texture2D)
                EditorGUILayout.ObjectField(
                    selectedTextures[i],
                    typeof(Texture2D),
                    false
                );

            if (GUILayout.Button(
                "X",
                GUILayout.Width(25)))
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

    private void DrawResolutionSettings()
    {
        EditorGUILayout.LabelField(
            "Resolution",
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

        maxTextureSize =
            EditorGUILayout.IntPopup(
                "Maximum Texture Size",
                maxTextureSize,
                sizes,
                sizeValues
            );
    }

    private void DrawColourSettings()
    {
        EditorGUILayout.LabelField(
            "PSX Colour Processing",
            EditorStyles.boldLabel
        );

        string[] paletteNames =
        {
            "16 Colours",
            "32 Colours",
            "64 Colours",
            "128 Colours",
            "256 Colours"
        };

        int[] paletteValues =
        {
            16,
            32,
            64,
            128,
            256
        };

        paletteColours =
            EditorGUILayout.IntPopup(
                "Colour Palette",
                paletteColours,
                paletteNames,
                paletteValues
            );

        GUILayout.Space(5);

        useDithering =
            EditorGUILayout.Toggle(
                "Ordered Dithering",
                useDithering
            );

        if (useDithering)
        {
            ditherStrength =
                EditorGUILayout.Slider(
                    "Dither Strength",
                    ditherStrength,
                    0f,
                    1f
                );
        }

        GUILayout.Space(8);

        brightness =
            EditorGUILayout.Slider(
                "Brightness",
                brightness,
                -0.5f,
                0.5f
            );

        contrast =
            EditorGUILayout.Slider(
                "Contrast",
                contrast,
                0.5f,
                1.5f
            );

        saturation =
            EditorGUILayout.Slider(
                "Saturation",
                saturation,
                0f,
                2f
            );
    }

    private void DrawImportSettings()
    {
        EditorGUILayout.LabelField(
            "Import Settings",
            EditorStyles.boldLabel
        );

        usePointFiltering =
            EditorGUILayout.Toggle(
                "Point Filtering",
                usePointFiltering
            );

        disableMipmaps =
            EditorGUILayout.Toggle(
                "Disable Mipmaps",
                disableMipmaps
            );

        disableAnisotropicFiltering =
            EditorGUILayout.Toggle(
                "Disable Anisotropic Filtering",
                disableAnisotropicFiltering
            );

        disableCompression =
            EditorGUILayout.Toggle(
                "Disable Compression",
                disableCompression
            );
    }

    private void LoadSelectedTextures()
    {
        selectedTextures.Clear();

        UnityEngine.Object[] selection =
            Selection.objects;

        foreach (
            UnityEngine.Object selectedObject
            in selection)
        {
            Texture2D texture =
                selectedObject as Texture2D;

            if (texture != null &&
                !selectedTextures.Contains(texture))
            {
                selectedTextures.Add(texture);
            }
        }
    }

    private void CrunchTextures()
    {
        int processedCount = 0;

        try
        {
            for (int i = 0;
                 i < selectedTextures.Count;
                 i++)
            {
                Texture2D texture =
                    selectedTextures[i];

                if (texture == null)
                    continue;

                EditorUtility.DisplayProgressBar(
                    "PSX Texture Cruncher",
                    "Processing " + texture.name,
                    (float)i /
                    selectedTextures.Count
                );

                Texture2D output =
                    PSXTextureProcessor.ProcessAndSave(
                        texture,
                        maxTextureSize,
                        paletteColours,
                        useDithering,
                        ditherStrength,
                        brightness,
                        contrast,
                        saturation,
                        usePointFiltering,
                        disableMipmaps,
                        disableAnisotropicFiltering,
                        disableCompression
                    );

                if (output != null)
                {
                    processedCount++;
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "PSX Texture Cruncher",
            processedCount +
            " texture(s) processed successfully.",
            "OK"
        );
    }
}