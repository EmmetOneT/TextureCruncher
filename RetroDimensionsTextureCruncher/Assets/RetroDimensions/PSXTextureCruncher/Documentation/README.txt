RETRO DIMENSIONS  
PSX TEXTURE CRUNCHER

Unity Editor texture-processing toolkit for creating low-resolution PSX-style textures.

INSTALLATION

Copy the RetroDimensions folder into the Assets folder of your Unity project.

Open the tool from:

Tools > Retro Dimensions > PSX Texture Cruncher

QUICK START

1. Drag one or more textures into the texture area.
2. Select a built-in preset or adjust the settings manually.
3. Use the Original / Crunched preview to inspect the result.
4. Press CRUNCH TO PSX COPY.
5. The processed texture will be created beside the original using the "_PSX" suffix.

Example:

BrickWall.png  
BrickWall_PSX.png

The original texture is preserved.

FOLDER PROCESSING

Assign a project folder under Source Folder.

Enable Include Subfolders if textures inside child folders should also be processed.

Use ADD FOLDER TO LIST to add the discovered textures to the normal processing list.

Use CRUNCH FOLDER NOW to process the folder directly.

Files ending in "_PSX" are ignored automatically.

PRESETS

Included presets:

PS1 Standard  
PS1 Horror  
PS1 Colourful  
Extremely Low-Fi

Changing any processing setting manually switches the tool to Custom mode.

CUSTOM PRESETS

Configure the Texture Cruncher as desired and select SAVE CURRENT AS PRESET.

Custom presets are saved as Unity asset files and can be reused later or moved between projects.

OUTPUT SAFETY

The Texture Cruncher creates new PNG files and does not permanently modify the source texture.

Overwrite Existing Copies is disabled by default.

When disabled, an existing "_PSX" output will be skipped.

Enable Overwrite Existing Copies only when you intentionally want to replace an existing processed texture.

SETTINGS

Maximum Texture Size controls the maximum output resolution.

Colour Palette reduces the available RGB colour levels.

Ordered Dithering introduces a Bayer-style dithering pattern.

Dither Strength controls the strength of that pattern.

Brightness adjusts overall image brightness.

Contrast increases or reduces separation between dark and light areas.

Saturation controls colour intensity.

Point Filtering disables smooth texture filtering.

Disable Mipmaps prevents Unity from generating lower-resolution mip levels.

Disable Anisotropic Filtering disables anisotropic texture filtering.

Disable Compression imports the generated texture without Unity texture compression.

RESTORE DEFAULTS

RESTORE DEFAULT SETTINGS returns the tool to the PS1 Standard configuration.

REQUIREMENTS

Designed for Unity 6.

The tool runs entirely inside the Unity Editor and does not add runtime components to your game.

RETRO DIMENSIONS