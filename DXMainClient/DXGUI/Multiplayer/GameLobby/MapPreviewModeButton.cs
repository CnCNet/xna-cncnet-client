#nullable enable
using ClientCore;
using ClientCore.Extensions;

using ClientGUI;

using DTAClient.Domain.Multiplayer;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI;

namespace DTAClient.DXGUI.Multiplayer.GameLobby;

/// <summary>Owns preview-mode interaction and its theme artwork, not image generation.</summary>
internal sealed class MapPreviewModeButton : XNAClientButton
{
    private readonly Texture2D previewHDButtonImage, previewSDButtonImage;
    private readonly Texture2D? previewHDButtonHoverImage, previewSDButtonHoverImage;

    public MapPreviewModeButton(WindowManager windowManager) : base(windowManager)
    {
        Name = "btnToggleRenderedPreview";
        previewHDButtonImage = AssetLoader.LoadTexture("previewHD.png");
        previewHDButtonHoverImage = LoadOptionalImage("previewHD_c.png");
        previewSDButtonImage = AssetLoader.LoadTexture("previewSD.png");
        previewSDButtonHoverImage = LoadOptionalImage("previewSD_c.png");
        LeftClick += (sender, args) =>
        {
            if (!MapPreviewGenerationService.Enabled) return;
            var settings = UserINISettings.Instance;
            settings.ShowGeneratedMapPreviews.Value = !settings.ShowGeneratedMapPreviews.Value;
            settings.SaveSettings(); // The service then raises ModeChanged for previews to refresh.
        };
    }

    private static Texture2D? LoadOptionalImage(string name)
        => AssetLoader.AssetExists(name) ? AssetLoader.LoadTexture(name) : null;

    /// <summary>Updates the artwork, size and tooltip; the map preview box positions the button.</summary>
    public void Refresh()
    {
        bool showOriginal = MapPreviewGenerationService.Selected;
        Texture2D idle = showOriginal ? previewSDButtonImage : previewHDButtonImage;
        IdleTexture = idle;
        HoverTexture = (showOriginal ? previewSDButtonHoverImage : previewHDButtonHoverImage) ?? idle;
        ClientRectangle = new Rectangle(X, Y, idle.Width, idle.Height);
        ToolTipText = showOriginal
            ? "Show original map preview".L10N("Client:Main:ShowOriginalPreview")
            : "Show generated HD map preview".L10N("Client:Main:ShowGeneratedPreview");
        if (MapPreviewGenerationService.Enabled) Enable(); else Disable();
    }
}
