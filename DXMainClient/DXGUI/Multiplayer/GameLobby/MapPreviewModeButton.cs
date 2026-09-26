#nullable enable
using System;

using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain.Multiplayer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>Owns preview-mode interaction and its theme artwork, not image generation.</summary>
internal sealed class MapPreviewModeButton : XNAClientButton
{
    private readonly Texture2D? previewHDButtonImage, previewHDButtonHoverImage;
    private readonly Texture2D? previewSDButtonImage, previewSDButtonHoverImage;
    private readonly Texture2D previewTextButtonImage;

    public MapPreviewModeButton(WindowManager windowManager) : base(windowManager)
    {
        Name = "btnToggleRenderedPreview";
        FontIndex = 1;
        var config = ClientConfiguration.Instance;
        previewHDButtonImage = LoadPreviewButtonImage(config.MapPreviewHDButtonImage);
        previewHDButtonHoverImage = LoadPreviewButtonImage(config.MapPreviewHDButtonHoverImage);
        previewSDButtonImage = LoadPreviewButtonImage(config.MapPreviewSDButtonImage);
        previewSDButtonHoverImage = LoadPreviewButtonImage(config.MapPreviewSDButtonHoverImage);
        previewTextButtonImage = AssetLoader.CreateTexture(Color.Transparent, 32, 18);
        LeftClick += (sender, args) =>
        {
            if (!MapPreviewGenerationService.Enabled) return;
            var settings = UserINISettings.Instance;
            settings.ShowGeneratedMapPreviews.Value = !settings.ShowGeneratedMapPreviews.Value;
            settings.SaveSettings(); // Host refreshes through its existing SettingsSaved subscription.
        };
    }

    private static Texture2D? LoadPreviewButtonImage(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return null;
        try
        {
            if (AssetLoader.AssetExists(name)) return AssetLoader.LoadTexture(name);
            Logger.Log("Map preview button image not found: " + name);
        }
        catch (Exception e)
        {
            Logger.Log("Cannot load map preview button image " + name + ": " + e.Message);
        }
        return null;
    }

    public void Refresh(int buttonX)
    {
        bool showOriginal = MapPreviewGenerationService.Selected;
        Texture2D? idle = showOriginal ? previewSDButtonImage : previewHDButtonImage;
        Texture2D? hover = showOriginal ? previewSDButtonHoverImage : previewHDButtonHoverImage;
        Text = idle == null ? (showOriginal ? "SD" : "HD") : string.Empty;
        IdleTexture = idle ?? previewTextButtonImage;
        HoverTexture = idle == null ? previewTextButtonImage : (hover ?? idle);
        int width = idle?.Width ?? 32;
        int height = idle?.Height ?? 18;
        ClientRectangle = new Rectangle(buttonX - 28 - width, 4, width, height);
        ToolTipText = showOriginal
            ? "Show original map preview".L10N("Client:Main:ShowOriginalPreview")
            : "Show generated HD map preview".L10N("Client:Main:ShowGeneratedPreview");
        if (MapPreviewGenerationService.Enabled) Enable(); else Disable();
    }

}