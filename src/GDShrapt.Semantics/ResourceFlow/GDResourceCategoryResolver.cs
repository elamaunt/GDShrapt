using System;

namespace GDShrapt.Semantics;

internal static class GDResourceCategoryResolver
{
    public static string TypeNameFromExtension(string path)
    {
        if (string.IsNullOrEmpty(path))
            return "Resource";

        if (path.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".webp", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
            return "Texture2D";

        if (path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".ogg", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
            return "AudioStream";

        if (path.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".otf", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".woff2", StringComparison.OrdinalIgnoreCase))
            return "Font";

        if (path.EndsWith(".gdshader", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".shader", StringComparison.OrdinalIgnoreCase))
            return "Shader";

        if (path.EndsWith(".material", StringComparison.OrdinalIgnoreCase))
            return "ShaderMaterial";

        if (path.EndsWith(".theme", StringComparison.OrdinalIgnoreCase))
            return "Theme";

        if (path.EndsWith(".mesh", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".obj", StringComparison.OrdinalIgnoreCase))
            return "Mesh";

        if (path.EndsWith(".tscn", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".scn", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".glb", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".gltf", StringComparison.OrdinalIgnoreCase))
            return "PackedScene";

        if (path.EndsWith(".gd", StringComparison.OrdinalIgnoreCase))
            return "GDScript";

        if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return "JSON";

        return "Resource";
    }

    public static GDResourceCategory CategoryFromExtension(string path)
    {
        var typeName = TypeNameFromExtension(path);
        if (typeName != "Resource" && typeName != "JSON")
            return CategoryFromTypeName(typeName);

        if (path.EndsWith(".tres", StringComparison.OrdinalIgnoreCase) ||
            path.EndsWith(".res", StringComparison.OrdinalIgnoreCase))
            return GDResourceCategory.Other;

        return GDResourceCategory.Other;
    }

    public static GDResourceCategory CategoryFromTypeName(string typeName)
    {
        return typeName switch
        {
            "Texture2D" or "CompressedTexture2D" or "AtlasTexture" or
            "ImageTexture" or "GradientTexture1D" or "GradientTexture2D" or
            "NoiseTexture2D" or "CurveTexture" or "PortableCompressedTexture2D" => GDResourceCategory.Texture,

            "AudioStream" or "AudioStreamWAV" or "AudioStreamOggVorbis" or
            "AudioStreamMP3" or "AudioStreamRandomizer" or "AudioStreamPolyphonic" => GDResourceCategory.Audio,

            "Font" or "FontFile" or "FontVariation" or "SystemFont" => GDResourceCategory.Font,

            "Material" or "ShaderMaterial" or "StandardMaterial3D" or
            "ORMMaterial3D" or "CanvasItemMaterial" or "ParticleProcessMaterial" => GDResourceCategory.Material,

            "Shader" or "VisualShader" => GDResourceCategory.Shader,

            "Theme" => GDResourceCategory.Theme,

            "StyleBox" or "StyleBoxFlat" or "StyleBoxTexture" or
            "StyleBoxLine" or "StyleBoxEmpty" => GDResourceCategory.StyleBox,

            "SpriteFrames" => GDResourceCategory.SpriteFrames,

            "Animation" or "AnimationLibrary" => GDResourceCategory.Animation,

            "TileSet" => GDResourceCategory.TileSet,

            "Mesh" or "ArrayMesh" or "BoxMesh" or "CapsuleMesh" or
            "CylinderMesh" or "PlaneMesh" or "SphereMesh" or "QuadMesh" => GDResourceCategory.Mesh,

            "PackedScene" => GDResourceCategory.PackedScene,

            "GDScript" or "Script" => GDResourceCategory.Script,

            _ => GDResourceCategory.Other
        };
    }

    public static string? PropertyToResourceType(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName))
            return null;

        // Handle theme_override paths like "theme_override_fonts/font"
        var baseProp = propertyName;
        if (propertyName.StartsWith("theme_override_fonts/"))
            return "Font";
        if (propertyName.StartsWith("theme_override_icons/"))
            return "Texture2D";
        if (propertyName.StartsWith("theme_override_styles/"))
            return "StyleBox";

        return baseProp switch
        {
            "texture" or "normal_map" or "icon" or "texture_normal" or
            "texture_pressed" or "texture_hover" or "texture_focused" or
            "texture_disabled" or "atlas" or "gradient" => "Texture2D",

            "material" or "material_override" or "material_overlay" or
            "next_pass" or "surface_material_override/0" => "Material",

            "stream" => "AudioStream",

            "theme" => "Theme",

            "mesh" => "Mesh",

            "font" or "custom_font" => "Font",

            "sprite_frames" => "SpriteFrames",

            "tile_set" => "TileSet",

            "shader" => "Shader",

            "shape" => "Shape2D",

            "environment" => "Environment",

            _ => null
        };
    }
}
