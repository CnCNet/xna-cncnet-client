#nullable enable
using System;

using DTAClient.Domain.Multiplayer.CnCNet;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

internal static class PingQualityVisuals
{
    public const int TextureCount = 5;

    public static int GetTextureIndex(PingValue ping)
        => GetTextureIndex(PingQualityRules.GetTier(ping));

    public static int GetTextureIndex(int milliseconds)
        => GetTextureIndex(PingQualityRules.GetTier(milliseconds));

    public static int GetTextureIndex(PingQualityTier tier) => tier switch
    {
        PingQualityTier.Good => 1,
        PingQualityTier.Fair => 2,
        PingQualityTier.Poor => 3,
        PingQualityTier.Bad => 4,
        _ => 0
    };

    public static Texture2D GetTexture(Texture2D[] textures, PingValue ping)
    {
        if (textures == null)
            throw new ArgumentNullException(nameof(textures));

        if (textures.Length < TextureCount)
            throw new ArgumentException($"Expected at least {TextureCount} ping textures.", nameof(textures));

        return textures[GetTextureIndex(ping)];
    }

    public static Color GetTextColor(PingValue ping)
        => GetTextColor(PingQualityRules.GetTier(ping));

    public static Color GetTextColor(int milliseconds)
        => GetTextColor(PingQualityRules.GetTier(milliseconds));

    public static Color GetTextColor(PingQualityTier tier) => tier switch
    {
        PingQualityTier.Good => Color.LightGreen,
        PingQualityTier.Fair => Color.Yellow,
        PingQualityTier.Poor => Color.Orange,
        PingQualityTier.Bad => Color.Red,
        _ => Color.Gray
    };

    public static Color GetBarColor(PingQualityTier tier) => tier switch
    {
        PingQualityTier.Good => new Color(0, 180, 0, 200),
        PingQualityTier.Fair => new Color(200, 180, 0, 200),
        PingQualityTier.Poor => new Color(200, 100, 0, 200),
        PingQualityTier.Bad => new Color(200, 0, 0, 200),
        _ => new Color(128, 128, 128, 200)
    };
}
