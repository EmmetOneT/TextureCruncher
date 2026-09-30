using UnityEngine;

[CreateAssetMenu(
    fileName = "PSX Texture Preset",
    menuName = "Retro Dimensions/PSX Texture Cruncher Preset"
)]
public class PSXTextureCruncherPreset : ScriptableObject
{
    [Header("Resolution")]
    public int maxTextureSize = 128;

    [Header("Colour")]
    public int paletteColours = 128;

    public bool useDithering = true;

    [Range(0f, 1f)]
    public float ditherStrength = 0.45f;

    [Range(-0.5f, 0.5f)]
    public float brightness = 0f;

    [Range(0.5f, 1.5f)]
    public float contrast = 1.05f;

    [Range(0f, 2f)]
    public float saturation = 0.95f;

    [Header("Import Settings")]
    public bool usePointFiltering = true;
    public bool disableMipmaps = true;
    public bool disableAnisotropicFiltering = true;
    public bool disableCompression = true;
}