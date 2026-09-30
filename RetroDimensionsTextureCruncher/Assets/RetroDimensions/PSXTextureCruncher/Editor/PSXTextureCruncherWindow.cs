using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class PSXTextureCruncherWindow : EditorWindow
{
    private enum CrunchPreset
    {
        PS1Standard,
        PS1Horror,
        PS1Colourful,
        ExtremelyLowFi,
        Custom
    }

    private enum PreviewMode
    {
        Original,
        Crunched
    }

    private CrunchPreset currentPreset =
        CrunchPreset.PS1Standard;

    private PreviewMode previewMode =
        PreviewMode.Crunched;

    // Processing
    private int maxTextureSize = 128;
    private int paletteColours = 128;

    private bool useDithering = true;
    private float ditherStrength = 0.45f;

    private float brightness = 0f;
    private float contrast = 1.05f;
    private float saturation = 0.95f;

    // Import
    private bool usePointFiltering = true;
    private bool disableMipmaps = true;
    private bool disableAnisotropicFiltering = true;
    private bool disableCompression = true;

    // Output
    private bool overwriteExistingOutputs = false;

    // Folder processing
    private DefaultAsset sourceFolder;
    private bool includeSubfolders = true;

    // Custom preset
    private PSXTextureCruncherPreset customPreset;

    // UI
    private Vector2 mainScroll;
    private Vector2 textureListScroll;

    private readonly List<Texture2D> selectedTextures =
        new List<Texture2D>();

    // Preview
    private Texture2D previewSource;
    private Texture2D readablePreviewSource;
    private Texture2D crunchedPreview;

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
            new Vector2(540, 800);
    }

    private void OnDisable()
    {
        ClearPreviewCache();
    }

    private void OnGUI()
    {
        DrawBranding();

        mainScroll =
            EditorGUILayout.BeginScrollView(
                mainScroll
            );

        GUILayout.Space(10);

        DrawTextureSection();

        GUILayout.Space(15);

        DrawFolderSection();

        GUILayout.Space(15);

        DrawPreviewSection();

        GUILayout.Space(20);

        DrawPresetSettings();

        GUILayout.Space(15);

        DrawCustomPresetSection();

        GUILayout.Space(15);

        EditorGUI.BeginChangeCheck();

        DrawResolutionSettings();

        GUILayout.Space(15);

        DrawColourSettings();

        GUILayout.Space(15);

        DrawImportSettings();

        GUILayout.Space(15);

        DrawOutputSettings();

        if (EditorGUI.EndChangeCheck())
        {
            currentPreset =
                CrunchPreset.Custom;

            RefreshPreview();
        }

        GUILayout.Space(20);

        DrawRestoreButton();

        GUILayout.Space(20);

        DrawCrunchButton();

        GUILayout.Space(25);

        EditorGUILayout.EndScrollView();
    }

    // =========================================================
    // BRANDING
    // =========================================================

    private void DrawBranding()
    {
        Rect headerRect =
            GUILayoutUtility.GetRect(
                0,
                82,
                GUILayout.ExpandWidth(true)
            );

        Color background =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.09f,
                    0.07f,
                    0.13f
                )
                : new Color(
                    0.18f,
                    0.14f,
                    0.23f
                );

        EditorGUI.DrawRect(
            headerRect,
            background
        );

        Rect accentRect =
            new Rect(
                headerRect.x,
                headerRect.yMax - 4,
                headerRect.width,
                4
            );

        EditorGUI.DrawRect(
            accentRect,
            new Color(
                0.68f,
                0.32f,
                0.9f
            )
        );

        GUIStyle brandStyle =
            new GUIStyle(
                EditorStyles.boldLabel
            );

        brandStyle.fontSize = 20;
        brandStyle.alignment =
            TextAnchor.MiddleCenter;

        brandStyle.normal.textColor =
            Color.white;

        GUI.Label(
            new Rect(
                headerRect.x,
                headerRect.y + 10,
                headerRect.width,
                28
            ),
            "RETRO DIMENSIONS",
            brandStyle
        );

        GUIStyle productStyle =
            new GUIStyle(
                EditorStyles.label
            );

        productStyle.fontSize = 12;
        productStyle.alignment =
            TextAnchor.MiddleCenter;

        productStyle.normal.textColor =
            new Color(
                0.82f,
                0.72f,
                0.95f
            );

        GUI.Label(
            new Rect(
                headerRect.x,
                headerRect.y + 40,
                headerRect.width,
                22
            ),
            "PSX TEXTURE CRUNCHER",
            productStyle
        );

        GUIStyle versionStyle =
            new GUIStyle(
                EditorStyles.miniLabel
            );

        versionStyle.alignment =
            TextAnchor.MiddleCenter;

        versionStyle.normal.textColor =
            new Color(
                0.65f,
                0.65f,
                0.65f
            );

        GUI.Label(
            new Rect(
                headerRect.x,
                headerRect.y + 60,
                headerRect.width,
                15
            ),
            "Texture Conversion Toolkit",
            versionStyle
        );
    }

    // =========================================================
    // TEXTURE INPUT
    // =========================================================

    private void DrawTextureSection()
    {
        EditorGUILayout.LabelField(
            "Individual Textures",
            EditorStyles.boldLabel
        );

        GUILayout.Space(5);

        DrawDropArea();

        GUILayout.Space(7);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "ADD PROJECT SELECTION",
            GUILayout.Height(26)))
        {
            AddSelectedTextures();
        }

        GUI.enabled =
            selectedTextures.Count > 0;

        if (GUILayout.Button(
            "CLEAR",
            GUILayout.Width(80),
            GUILayout.Height(26)))
        {
            ClearTextureList();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(8);

        EditorGUILayout.LabelField(
            "Loaded: " +
            selectedTextures.Count
        );

        if (selectedTextures.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Drag textures above or select textures in the Project window.",
                MessageType.None
            );

            return;
        }

        textureListScroll =
            EditorGUILayout.BeginScrollView(
                textureListScroll,
                GUILayout.Height(145)
            );

        for (int i = 0;
             i < selectedTextures.Count;
             i++)
        {
            DrawTextureRow(i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawDropArea()
    {
        Rect dropRect =
            GUILayoutUtility.GetRect(
                0,
                70,
                GUILayout.ExpandWidth(true)
            );

        Color boxColour =
            EditorGUIUtility.isProSkin
                ? new Color(
                    0.15f,
                    0.15f,
                    0.17f
                )
                : new Color(
                    0.78f,
                    0.78f,
                    0.8f
                );

        EditorGUI.DrawRect(
            dropRect,
            boxColour
        );

        EditorGUI.DrawRect(
            new Rect(
                dropRect.x,
                dropRect.y,
                dropRect.width,
                2
            ),
            new Color(
                0.68f,
                0.32f,
                0.9f
            )
        );

        GUIStyle dropStyle =
            new GUIStyle(
                EditorStyles.boldLabel
            );

        dropStyle.alignment =
            TextAnchor.MiddleCenter;

        dropStyle.fontSize = 12;

        GUI.Label(
            dropRect,
            "DRAG & DROP TEXTURES HERE",
            dropStyle
        );

        Event currentEvent =
            Event.current;

        if (!dropRect.Contains(
            currentEvent.mousePosition))
        {
            return;
        }

        if (currentEvent.type ==
            EventType.DragUpdated)
        {
            DragAndDrop.visualMode =
                HasDraggedTexture()
                    ? DragAndDropVisualMode.Copy
                    : DragAndDropVisualMode.Rejected;

            currentEvent.Use();
        }

        if (currentEvent.type ==
            EventType.DragPerform)
        {
            if (!HasDraggedTexture())
                return;

            DragAndDrop.AcceptDrag();

            foreach (
                Object draggedObject
                in DragAndDrop.objectReferences)
            {
                if (draggedObject is Texture2D texture)
                {
                    AddTexture(texture);
                }
            }

            currentEvent.Use();
        }
    }

    private bool HasDraggedTexture()
    {
        foreach (
            Object draggedObject
            in DragAndDrop.objectReferences)
        {
            if (draggedObject is Texture2D)
                return true;
        }

        return false;
    }

    private void DrawTextureRow(int index)
    {
        Texture2D texture =
            selectedTextures[index];

        EditorGUILayout.BeginHorizontal(
            EditorStyles.helpBox
        );

        Texture2D changedTexture =
            (Texture2D)
            EditorGUILayout.ObjectField(
                texture,
                typeof(Texture2D),
                false
            );

        if (changedTexture != texture)
        {
            selectedTextures[index] =
                changedTexture;

            if (previewSource == texture)
            {
                SetPreviewTexture(
                    changedTexture
                );
            }
        }

        bool isPreview =
            texture != null &&
            texture == previewSource;

        GUI.enabled =
            texture != null &&
            !isPreview;

        if (GUILayout.Button(
            isPreview
                ? "VIEWING"
                : "PREVIEW",
            GUILayout.Width(75)))
        {
            SetPreviewTexture(texture);
        }

        GUI.enabled = true;

        if (GUILayout.Button(
            "X",
            GUILayout.Width(25)))
        {
            RemoveTexture(index);
        }

        EditorGUILayout.EndHorizontal();
    }

    private void AddSelectedTextures()
    {
        foreach (
            Object selectedObject
            in Selection.objects)
        {
            if (selectedObject is Texture2D texture)
            {
                AddTexture(texture);
            }
        }
    }

    private void AddTexture(
        Texture2D texture)
    {
        if (texture == null)
            return;

        if (IsGeneratedPSXTexture(texture))
            return;

        if (!selectedTextures.Contains(texture))
        {
            selectedTextures.Add(texture);
        }

        if (previewSource == null)
        {
            SetPreviewTexture(texture);
        }
    }

    private bool IsGeneratedPSXTexture(
        Texture2D texture)
    {
        return texture.name.EndsWith(
            "_PSX"
        );
    }

    private void RemoveTexture(
        int index)
    {
        if (index < 0 ||
            index >= selectedTextures.Count)
        {
            return;
        }

        Texture2D removed =
            selectedTextures[index];

        selectedTextures.RemoveAt(
            index
        );

        if (removed == previewSource)
        {
            if (selectedTextures.Count > 0)
            {
                SetPreviewTexture(
                    selectedTextures[0]
                );
            }
            else
            {
                SetPreviewTexture(null);
            }
        }
    }

    private void ClearTextureList()
    {
        selectedTextures.Clear();

        SetPreviewTexture(null);
    }

    // =========================================================
    // FOLDER PROCESSING
    // =========================================================

    private void DrawFolderSection()
    {
        EditorGUILayout.LabelField(
            "Folder Processing",
            EditorStyles.boldLabel
        );

        sourceFolder =
            (DefaultAsset)
            EditorGUILayout.ObjectField(
                "Source Folder",
                sourceFolder,
                typeof(DefaultAsset),
                false
            );

        includeSubfolders =
            EditorGUILayout.Toggle(
                "Include Subfolders",
                includeSubfolders
            );

        GUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        GUI.enabled =
            IsSourceFolderValid();

        if (GUILayout.Button(
            "ADD FOLDER TO LIST",
            GUILayout.Height(30)))
        {
            AddFolderTexturesToList();
        }

        if (GUILayout.Button(
            "CRUNCH FOLDER NOW",
            GUILayout.Height(30)))
        {
            CrunchFolderDirectly();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.HelpBox(
            "Generated files ending in _PSX are automatically ignored.",
            MessageType.None
        );
    }

    private bool IsSourceFolderValid()
    {
        if (sourceFolder == null)
            return false;

        string path =
            AssetDatabase.GetAssetPath(
                sourceFolder
            );

        return AssetDatabase.IsValidFolder(
            path
        );
    }

    private List<Texture2D> GetFolderTextures()
    {
        List<Texture2D> textures =
            new List<Texture2D>();

        if (!IsSourceFolderValid())
            return textures;

        string folderPath =
            AssetDatabase.GetAssetPath(
                sourceFolder
            );

        string[] guids =
            AssetDatabase.FindAssets(
                "t:Texture2D",
                new[]
                {
                    folderPath
                }
            );

        foreach (string guid in guids)
        {
            string assetPath =
                AssetDatabase.GUIDToAssetPath(
                    guid
                );

            if (!includeSubfolders)
            {
                string textureFolder =
                    Path.GetDirectoryName(
                        assetPath
                    );

                if (textureFolder != null)
                {
                    textureFolder =
                        textureFolder.Replace(
                            "\\",
                            "/"
                        );
                }

                if (textureFolder != folderPath)
                    continue;
            }

            Texture2D texture =
                AssetDatabase
                    .LoadAssetAtPath<Texture2D>(
                        assetPath
                    );

            if (texture == null)
                continue;

            if (IsGeneratedPSXTexture(texture))
                continue;

            textures.Add(texture);
        }

        return textures;
    }

    private void AddFolderTexturesToList()
    {
        List<Texture2D> textures =
            GetFolderTextures();

        foreach (Texture2D texture in textures)
        {
            AddTexture(texture);
        }

        EditorUtility.DisplayDialog(
            "PSX Texture Cruncher",
            textures.Count +
            " texture(s) found in folder.",
            "OK"
        );
    }

    private void CrunchFolderDirectly()
    {
        List<Texture2D> textures =
            GetFolderTextures();

        if (textures.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "PSX Texture Cruncher",
                "No textures were found in the selected folder.",
                "OK"
            );

            return;
        }

        bool confirmed =
            EditorUtility.DisplayDialog(
                "Crunch Folder",
                "Process " +
                textures.Count +
                " texture(s)?\n\nOriginal textures will not be overwritten.",
                "Crunch",
                "Cancel"
            );

        if (!confirmed)
            return;

        CrunchTextureList(textures);
    }

    // =========================================================
    // PREVIEW
    // =========================================================

    private void DrawPreviewSection()
    {
        EditorGUILayout.LabelField(
            "Preview",
            EditorStyles.boldLabel
        );

        if (previewSource == null)
        {
            EditorGUILayout.HelpBox(
                "Add a texture to display the before and after preview.",
                MessageType.Info
            );

            return;
        }

        GUILayout.Space(5);

        previewMode =
            (PreviewMode)
            GUILayout.Toolbar(
                (int)previewMode,
                new[]
                {
                    "ORIGINAL",
                    "CRUNCHED"
                },
                GUILayout.Height(28)
            );

        GUILayout.Space(8);

        Texture2D displayedTexture =
            previewMode ==
            PreviewMode.Original
                ? previewSource
                : crunchedPreview;

        float previewSize =
            Mathf.Clamp(
                position.width - 55f,
                250f,
                430f
            );

        Rect previewRect =
            GUILayoutUtility.GetRect(
                previewSize,
                previewSize,
                GUILayout.ExpandWidth(false)
            );

        previewRect.x =
            (
                position.width -
                previewRect.width
            ) / 2f;

        EditorGUI.DrawRect(
            previewRect,
            new Color(
                0.08f,
                0.08f,
                0.08f
            )
        );

        if (displayedTexture != null)
        {
            EditorGUI.DrawPreviewTexture(
                previewRect,
                displayedTexture,
                null,
                ScaleMode.ScaleToFit
            );
        }

        GUILayout.Space(6);

        GUIStyle centreStyle =
            new GUIStyle(
                EditorStyles
                    .centeredGreyMiniLabel
            );

        if (previewMode ==
            PreviewMode.Original)
        {
            EditorGUILayout.LabelField(
                previewSource.name +
                "  •  " +
                previewSource.width +
                " × " +
                previewSource.height,
                centreStyle
            );
        }
        else if (crunchedPreview != null)
        {
            EditorGUILayout.LabelField(
                previewSource.name +
                "_PSX  •  " +
                crunchedPreview.width +
                " × " +
                crunchedPreview.height,
                centreStyle
            );
        }
    }

    private void SetPreviewTexture(
        Texture2D texture)
    {
        if (previewSource == texture)
            return;

        ClearPreviewCache();

        previewSource = texture;

        if (previewSource == null)
        {
            Repaint();
            return;
        }

        readablePreviewSource =
            PSXTextureProcessor
                .CreateReadableCopy(
                    previewSource
                );

        RefreshPreview();

        previewMode =
            PreviewMode.Crunched;

        Repaint();
    }

    private void RefreshPreview()
    {
        if (readablePreviewSource == null)
            return;

        if (crunchedPreview != null)
        {
            DestroyImmediate(
                crunchedPreview
            );

            crunchedPreview = null;
        }

        crunchedPreview =
            PSXTextureProcessor
                .GeneratePreview(
                    readablePreviewSource,
                    maxTextureSize,
                    paletteColours,
                    useDithering,
                    ditherStrength,
                    brightness,
                    contrast,
                    saturation
                );

        Repaint();
    }

    private void ClearPreviewCache()
    {
        if (readablePreviewSource != null)
        {
            DestroyImmediate(
                readablePreviewSource
            );

            readablePreviewSource = null;
        }

        if (crunchedPreview != null)
        {
            DestroyImmediate(
                crunchedPreview
            );

            crunchedPreview = null;
        }
    }

    // =========================================================
    // BUILT-IN PRESETS
    // =========================================================

    private void DrawPresetSettings()
    {
        EditorGUILayout.LabelField(
            "Built-In Preset",
            EditorStyles.boldLabel
        );

        CrunchPreset newPreset =
            (CrunchPreset)
            EditorGUILayout.EnumPopup(
                "PSX Preset",
                currentPreset
            );

        if (newPreset != currentPreset)
        {
            currentPreset =
                newPreset;

            if (currentPreset !=
                CrunchPreset.Custom)
            {
                ApplyPreset(
                    currentPreset
                );

                RefreshPreview();
            }
        }

        GUILayout.Space(5);

        EditorGUILayout.HelpBox(
            GetPresetDescription(),
            MessageType.None
        );
    }

    private void ApplyPreset(
        CrunchPreset preset)
    {
        switch (preset)
        {
            case CrunchPreset.PS1Standard:

                maxTextureSize = 128;
                paletteColours = 128;

                useDithering = true;
                ditherStrength = 0.45f;

                brightness = 0f;
                contrast = 1.05f;
                saturation = 0.95f;

                break;

            case CrunchPreset.PS1Horror:

                maxTextureSize = 128;
                paletteColours = 64;

                useDithering = true;
                ditherStrength = 0.7f;

                brightness = -0.08f;
                contrast = 1.2f;
                saturation = 0.65f;

                break;

            case CrunchPreset.PS1Colourful:

                maxTextureSize = 128;
                paletteColours = 256;

                useDithering = true;
                ditherStrength = 0.3f;

                brightness = 0.03f;
                contrast = 1.08f;
                saturation = 1.35f;

                break;

            case CrunchPreset.ExtremelyLowFi:

                maxTextureSize = 64;
                paletteColours = 32;

                useDithering = true;
                ditherStrength = 0.9f;

                brightness = -0.03f;
                contrast = 1.25f;
                saturation = 0.8f;

                break;
        }

        usePointFiltering = true;
        disableMipmaps = true;
        disableAnisotropicFiltering = true;
        disableCompression = true;
    }

    private string GetPresetDescription()
    {
        switch (currentPreset)
        {
            case CrunchPreset.PS1Standard:

                return
                    "Balanced PS1-style processing suitable for most textures.";

            case CrunchPreset.PS1Horror:

                return
                    "Darker tones, reduced saturation and stronger dithering for gritty environments.";

            case CrunchPreset.PS1Colourful:

                return
                    "Higher colour range and stronger saturation for colourful retro games.";

            case CrunchPreset.ExtremelyLowFi:

                return
                    "Very low resolution, restricted colour and heavy dithering.";

            case CrunchPreset.Custom:

                return
                    "Custom processing settings.";

            default:

                return "";
        }
    }

    // =========================================================
    // CUSTOM PRESETS
    // =========================================================

    private void DrawCustomPresetSection()
    {
        EditorGUILayout.LabelField(
            "Custom Presets",
            EditorStyles.boldLabel
        );

        EditorGUI.BeginChangeCheck();

        customPreset =
            (PSXTextureCruncherPreset)
            EditorGUILayout.ObjectField(
                "Preset Asset",
                customPreset,
                typeof(
                    PSXTextureCruncherPreset
                ),
                false
            );

        if (EditorGUI.EndChangeCheck() &&
            customPreset != null)
        {
            LoadCustomPreset();
        }

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button(
            "SAVE CURRENT AS PRESET"))
        {
            SaveCustomPreset();
        }

        GUI.enabled =
            customPreset != null;

        if (GUILayout.Button(
            "APPLY PRESET"))
        {
            LoadCustomPreset();
        }

        GUI.enabled = true;

        EditorGUILayout.EndHorizontal();
    }

    private void SaveCustomPreset()
    {
        string path =
            EditorUtility.SaveFilePanelInProject(
                "Save PSX Texture Preset",
                "My PSX Preset",
                "asset",
                "Choose where to save the preset."
            );

        if (string.IsNullOrEmpty(path))
            return;

        path =
            AssetDatabase
                .GenerateUniqueAssetPath(
                    path
                );

        PSXTextureCruncherPreset preset =
            CreateInstance<
                PSXTextureCruncherPreset
            >();

        CopySettingsToPreset(
            preset
        );

        AssetDatabase.CreateAsset(
            preset,
            path
        );

        AssetDatabase.SaveAssets();

        customPreset = preset;

        currentPreset =
            CrunchPreset.Custom;

        Selection.activeObject =
            preset;

        EditorGUIUtility.PingObject(
            preset
        );
    }

    private void CopySettingsToPreset(
        PSXTextureCruncherPreset preset)
    {
        preset.maxTextureSize =
            maxTextureSize;

        preset.paletteColours =
            paletteColours;

        preset.useDithering =
            useDithering;

        preset.ditherStrength =
            ditherStrength;

        preset.brightness =
            brightness;

        preset.contrast =
            contrast;

        preset.saturation =
            saturation;

        preset.usePointFiltering =
            usePointFiltering;

        preset.disableMipmaps =
            disableMipmaps;

        preset.disableAnisotropicFiltering =
            disableAnisotropicFiltering;

        preset.disableCompression =
            disableCompression;
    }

    private void LoadCustomPreset()
    {
        if (customPreset == null)
            return;

        maxTextureSize =
            customPreset.maxTextureSize;

        paletteColours =
            customPreset.paletteColours;

        useDithering =
            customPreset.useDithering;

        ditherStrength =
            customPreset.ditherStrength;

        brightness =
            customPreset.brightness;

        contrast =
            customPreset.contrast;

        saturation =
            customPreset.saturation;

        usePointFiltering =
            customPreset.usePointFiltering;

        disableMipmaps =
            customPreset.disableMipmaps;

        disableAnisotropicFiltering =
            customPreset
                .disableAnisotropicFiltering;

        disableCompression =
            customPreset.disableCompression;

        currentPreset =
            CrunchPreset.Custom;

        RefreshPreview();
    }

    // =========================================================
    // SETTINGS
    // =========================================================

    private void DrawResolutionSettings()
    {
        EditorGUILayout.LabelField(
            "Resolution",
            EditorStyles.boldLabel
        );

        maxTextureSize =
            EditorGUILayout.IntPopup(
                "Maximum Texture Size",
                maxTextureSize,
                new[]
                {
                    "32",
                    "64",
                    "128",
                    "256",
                    "512"
                },
                new[]
                {
                    32,
                    64,
                    128,
                    256,
                    512
                }
            );
    }

    private void DrawColourSettings()
    {
        EditorGUILayout.LabelField(
            "PSX Colour Processing",
            EditorStyles.boldLabel
        );

        paletteColours =
            EditorGUILayout.IntPopup(
                "Colour Palette",
                paletteColours,
                new[]
                {
                    "16 Colours",
                    "32 Colours",
                    "64 Colours",
                    "128 Colours",
                    "256 Colours"
                },
                new[]
                {
                    16,
                    32,
                    64,
                    128,
                    256
                }
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

    private void DrawOutputSettings()
    {
        EditorGUILayout.LabelField(
            "Output",
            EditorStyles.boldLabel
        );

        overwriteExistingOutputs =
            EditorGUILayout.Toggle(
                "Overwrite Existing Copies",
                overwriteExistingOutputs
            );

        EditorGUILayout.HelpBox(
            overwriteExistingOutputs
                ? "Existing _PSX files may be replaced."
                : "Existing _PSX files will be skipped.",
            overwriteExistingOutputs
                ? MessageType.Warning
                : MessageType.Info
        );

        EditorGUILayout.LabelField(
            "Output Location",
            "Beside original texture"
        );

        EditorGUILayout.LabelField(
            "Filename",
            "OriginalName_PSX.png"
        );
    }

    // =========================================================
    // RESTORE
    // =========================================================

    private void DrawRestoreButton()
    {
        if (GUILayout.Button(
            "RESTORE DEFAULT SETTINGS",
            GUILayout.Height(28)))
        {
            bool restore =
                EditorUtility.DisplayDialog(
                    "Restore Defaults",
                    "Restore PSX Texture Cruncher settings to PS1 Standard?",
                    "Restore",
                    "Cancel"
                );

            if (restore)
            {
                currentPreset =
                    CrunchPreset.PS1Standard;

                ApplyPreset(
                    CrunchPreset.PS1Standard
                );

                overwriteExistingOutputs =
                    false;

                RefreshPreview();
            }
        }
    }

    // =========================================================
    // CRUNCH
    // =========================================================

    private void DrawCrunchButton()
    {
        GUI.enabled =
            selectedTextures.Count > 0;

        Color oldColour =
            GUI.backgroundColor;

        GUI.backgroundColor =
            new Color(
                0.62f,
                0.32f,
                0.82f
            );

        if (GUILayout.Button(
            "CRUNCH TO PSX COPY",
            GUILayout.Height(50)))
        {
            CrunchTextureList(
                selectedTextures
            );
        }

        GUI.backgroundColor =
            oldColour;

        GUI.enabled = true;

        GUILayout.Space(5);

        GUIStyle infoStyle =
            new GUIStyle(
                EditorStyles
                    .centeredGreyMiniLabel
            );

        EditorGUILayout.LabelField(
            "Original texture files are preserved.",
            infoStyle
        );
    }

    private void CrunchTextureList(
        IList<Texture2D> textures)
    {
        int processedCount = 0;
        int skippedCount = 0;

        try
        {
            for (int i = 0;
                 i < textures.Count;
                 i++)
            {
                Texture2D texture =
                    textures[i];

                if (texture == null)
                    continue;

                if (IsGeneratedPSXTexture(texture))
                {
                    skippedCount++;
                    continue;
                }

                string outputPath =
                    GetExpectedOutputPath(
                        texture
                    );

                bool outputExists =
                    AssetDatabase
                        .LoadAssetAtPath<Texture2D>(
                            outputPath
                        ) != null;

                if (outputExists &&
                    !overwriteExistingOutputs)
                {
                    skippedCount++;
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    "PSX Texture Cruncher",
                    "Processing " +
                    texture.name,
                    (float)i /
                    textures.Count
                );

                Texture2D output =
                    PSXTextureProcessor
                        .ProcessAndSave(
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
            " texture(s) processed.\n" +
            skippedCount +
            " texture(s) skipped.",
            "OK"
        );
    }

    private string GetExpectedOutputPath(
        Texture2D texture)
    {
        string sourcePath =
            AssetDatabase.GetAssetPath(
                texture
            );

        string directory =
            Path.GetDirectoryName(
                sourcePath
            );

        if (directory != null)
        {
            directory =
                directory.Replace(
                    "\\",
                    "/"
                );
        }

        string fileName =
            Path.GetFileNameWithoutExtension(
                sourcePath
            );

        return directory +
               "/" +
               fileName +
               "_PSX.png";
    }
}