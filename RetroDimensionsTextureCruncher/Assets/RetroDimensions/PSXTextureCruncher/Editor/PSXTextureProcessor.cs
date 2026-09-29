using System.IO;
using UnityEditor;
using UnityEngine;

public static class PSXTextureProcessor
{
    private static readonly int[,] BayerMatrix =
    {
        {  0,  8,  2, 10 },
        { 12,  4, 14,  6 },
        {  3, 11,  1,  9 },
        { 15,  7, 13,  5 }
    };

    public static Texture2D ProcessAndSave(
        Texture2D source,
        int maxTextureSize,
        int paletteColours,
        bool useDithering,
        float ditherStrength,
        float brightness,
        float contrast,
        float saturation,
        bool usePointFiltering,
        bool disableMipmaps,
        bool disableAnisotropicFiltering,
        bool disableCompression)
    {
        if (source == null)
            return null;

        string sourcePath = AssetDatabase.GetAssetPath(source);

        if (string.IsNullOrEmpty(sourcePath) ||
            !sourcePath.StartsWith("Assets/"))
        {
            Debug.LogWarning(
                "PSX Texture Cruncher: Texture must be inside the Assets folder."
            );

            return null;
        }

        TextureImporter sourceImporter =
            AssetImporter.GetAtPath(sourcePath) as TextureImporter;

        if (sourceImporter == null)
            return null;

        bool originalReadable = sourceImporter.isReadable;

        try
        {
            // Temporarily enable Read/Write on source.
            if (!originalReadable)
            {
                sourceImporter.isReadable = true;
                sourceImporter.SaveAndReimport();

                source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            }

            Color32[] sourcePixels = source.GetPixels32();

            CalculateOutputSize(
                source.width,
                source.height,
                maxTextureSize,
                out int outputWidth,
                out int outputHeight
            );

            Color32[] resizedPixels = ResizeNearestNeighbour(
                sourcePixels,
                source.width,
                source.height,
                outputWidth,
                outputHeight
            );

            Color32[] processedPixels = ProcessPixels(
                resizedPixels,
                outputWidth,
                outputHeight,
                paletteColours,
                useDithering,
                ditherStrength,
                brightness,
                contrast,
                saturation
            );

            Texture2D processedTexture = new Texture2D(
                outputWidth,
                outputHeight,
                TextureFormat.RGBA32,
                false
            );

            processedTexture.SetPixels32(processedPixels);
            processedTexture.Apply(false, false);

            string outputPath = GetOutputPath(sourcePath);

            byte[] pngData = processedTexture.EncodeToPNG();

            string absoluteOutputPath = Path.GetFullPath(outputPath);

            File.WriteAllBytes(
                absoluteOutputPath,
                pngData
            );

            Object.DestroyImmediate(processedTexture);

            AssetDatabase.ImportAsset(
                outputPath,
                ImportAssetOptions.ForceUpdate
            );

            ApplyOutputImportSettings(
                outputPath,
                maxTextureSize,
                usePointFiltering,
                disableMipmaps,
                disableAnisotropicFiltering,
                disableCompression
            );

            return AssetDatabase.LoadAssetAtPath<Texture2D>(outputPath);
        }
        finally
        {
            // Put the original texture back exactly as it was.
            TextureImporter restoreImporter =
                AssetImporter.GetAtPath(sourcePath) as TextureImporter;

            if (restoreImporter != null &&
                restoreImporter.isReadable != originalReadable)
            {
                restoreImporter.isReadable = originalReadable;
                restoreImporter.SaveAndReimport();
            }
        }
    }

    private static void CalculateOutputSize(
        int width,
        int height,
        int maxSize,
        out int outputWidth,
        out int outputHeight)
    {
        if (width <= maxSize && height <= maxSize)
        {
            outputWidth = width;
            outputHeight = height;
            return;
        }

        float scale = Mathf.Min(
            (float)maxSize / width,
            (float)maxSize / height
        );

        outputWidth = Mathf.Max(
            1,
            Mathf.RoundToInt(width * scale)
        );

        outputHeight = Mathf.Max(
            1,
            Mathf.RoundToInt(height * scale)
        );
    }

    private static Color32[] ResizeNearestNeighbour(
        Color32[] sourcePixels,
        int sourceWidth,
        int sourceHeight,
        int targetWidth,
        int targetHeight)
    {
        Color32[] output =
            new Color32[targetWidth * targetHeight];

        for (int y = 0; y < targetHeight; y++)
        {
            int sourceY = Mathf.Min(
                sourceHeight - 1,
                Mathf.FloorToInt(
                    (float)y / targetHeight * sourceHeight
                )
            );

            for (int x = 0; x < targetWidth; x++)
            {
                int sourceX = Mathf.Min(
                    sourceWidth - 1,
                    Mathf.FloorToInt(
                        (float)x / targetWidth * sourceWidth
                    )
                );

                output[y * targetWidth + x] =
                    sourcePixels[sourceY * sourceWidth + sourceX];
            }
        }

        return output;
    }

    private static Color32[] ProcessPixels(
        Color32[] pixels,
        int width,
        int height,
        int paletteColours,
        bool useDithering,
        float ditherStrength,
        float brightness,
        float contrast,
        float saturation)
    {
        Color32[] output = new Color32[pixels.Length];

        GetPaletteBits(
            paletteColours,
            out int redBits,
            out int greenBits,
            out int blueBits
        );

        int redLevels = 1 << redBits;
        int greenLevels = 1 << greenBits;
        int blueLevels = 1 << blueBits;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;

                Color colour = pixels[index];

                float originalAlpha = colour.a;

                colour = ApplyColourAdjustments(
                    colour,
                    brightness,
                    contrast,
                    saturation
                );

                if (useDithering)
                {
                    float bayer =
                        BayerMatrix[y % 4, x % 4];

                    float dither =
                        (bayer / 15f) - 0.5f;

                    colour.r +=
                        ditherStrength *
                        dither /
                        Mathf.Max(1, redLevels - 1);

                    colour.g +=
                        ditherStrength *
                        dither /
                        Mathf.Max(1, greenLevels - 1);

                    colour.b +=
                        ditherStrength *
                        dither /
                        Mathf.Max(1, blueLevels - 1);
                }

                colour.r = QuantizeChannel(
                    colour.r,
                    redLevels
                );

                colour.g = QuantizeChannel(
                    colour.g,
                    greenLevels
                );

                colour.b = QuantizeChannel(
                    colour.b,
                    blueLevels
                );

                colour.a = originalAlpha;

                output[index] = colour;
            }
        }

        return output;
    }

    private static Color ApplyColourAdjustments(
        Color colour,
        float brightness,
        float contrast,
        float saturation)
    {
        // Brightness
        colour.r += brightness;
        colour.g += brightness;
        colour.b += brightness;

        // Contrast
        colour.r =
            (colour.r - 0.5f) * contrast + 0.5f;

        colour.g =
            (colour.g - 0.5f) * contrast + 0.5f;

        colour.b =
            (colour.b - 0.5f) * contrast + 0.5f;

        // Saturation
        float luminance =
            colour.r * 0.2126f +
            colour.g * 0.7152f +
            colour.b * 0.0722f;

        Color grey = new Color(
            luminance,
            luminance,
            luminance,
            colour.a
        );

        colour = Color.LerpUnclamped(
            grey,
            colour,
            saturation
        );

        colour.r = Mathf.Clamp01(colour.r);
        colour.g = Mathf.Clamp01(colour.g);
        colour.b = Mathf.Clamp01(colour.b);

        return colour;
    }

    private static float QuantizeChannel(
        float value,
        int levels)
    {
        value = Mathf.Clamp01(value);

        return Mathf.Round(
            value * (levels - 1)
        ) / (levels - 1);
    }

    private static void GetPaletteBits(
        int paletteColours,
        out int redBits,
        out int greenBits,
        out int blueBits)
    {
        switch (paletteColours)
        {
            case 16:
                redBits = 2;
                greenBits = 1;
                blueBits = 1;
                break;

            case 32:
                redBits = 2;
                greenBits = 2;
                blueBits = 1;
                break;

            case 64:
                redBits = 2;
                greenBits = 2;
                blueBits = 2;
                break;

            case 128:
                redBits = 3;
                greenBits = 2;
                blueBits = 2;
                break;

            default:
                // 256 colours
                redBits = 3;
                greenBits = 3;
                blueBits = 2;
                break;
        }
    }

    private static string GetOutputPath(
        string sourcePath)
    {
        string directory =
            Path.GetDirectoryName(sourcePath);

        if (directory != null)
        {
            directory =
                directory.Replace("\\", "/");
        }

        string fileName =
            Path.GetFileNameWithoutExtension(sourcePath);

        return directory + "/" +
               fileName +
               "_PSX.png";
    }

    private static void ApplyOutputImportSettings(
        string outputPath,
        int maxTextureSize,
        bool usePointFiltering,
        bool disableMipmaps,
        bool disableAnisotropicFiltering,
        bool disableCompression)
    {
        TextureImporter importer =
            AssetImporter.GetAtPath(outputPath)
            as TextureImporter;

        if (importer == null)
            return;

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
    }
}