using System.Linq;
using System.Reflection;
using UnityEngine;

namespace BeatSaberMultiplayerChat.Assets;

internal static class Sprites
{
    public static Sprite? Keyboard;
    public static Sprite? MicOff;
    public static Sprite? MicOn;

    public static bool IsInitialized { get; private set; }

    public static void Initialize()
    {
        IsInitialized = true;

        Keyboard = LoadSpriteFromResources("BeatSaberMultiplayerChat.Assets.Keyboard.png");
        MicOff = LoadSpriteFromResources("BeatSaberMultiplayerChat.Assets.MicOff.png");
        MicOn = LoadSpriteFromResources("BeatSaberMultiplayerChat.Assets.MicOn.png");
    }

    private static Sprite? LoadSpriteFromResources(string resourcePath, float pixelsPerUnit = 100.0f)
    {
        var rawData = GetResource(Assembly.GetCallingAssembly(), resourcePath);

        if (rawData is null)
            return null;

        var sprite = LoadSpriteRaw(rawData, pixelsPerUnit);

        if (sprite is null)
            return null;

        sprite.name = resourcePath;
        return sprite;
    }

    private static byte[]? GetResource(Assembly asm, string resourceName)
    {
        var stream = asm.GetManifestResourceStream(resourceName);

        if (stream is null)
            return null;

        var data = new byte[stream.Length];
        stream.Read(data, 0, (int) stream.Length);
        return data;
    }

    internal static Sprite? LoadSpriteRaw(byte[] image, float pixelsPerUnit = 100.0f,
        SpriteMeshType spriteMeshType = SpriteMeshType.Tight)
    {
        var texture = LoadTextureRaw(image);

        if (texture is null)
            return null;

        return LoadSpriteFromTexture(texture, pixelsPerUnit, spriteMeshType);
    }

    private static Texture2D? LoadTextureRaw(byte[] file)
    {
        if (!file.Any())
            return null;

        var texture = new Texture2D(2, 2);
        return texture.LoadImage(file) ? texture : null;
    }

    private static Sprite LoadSpriteFromTexture(Texture2D spriteTexture, float pixelsPerUnit = 100.0f,
        SpriteMeshType spriteMeshType = SpriteMeshType.Tight)
    {
        return Sprite.Create(spriteTexture, new Rect(0, 0, spriteTexture.width, spriteTexture.height),
            new Vector2(0, 0), pixelsPerUnit, 0, spriteMeshType);
    }
}