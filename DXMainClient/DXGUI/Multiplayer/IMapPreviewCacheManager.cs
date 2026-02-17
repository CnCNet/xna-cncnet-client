#nullable enable
using System;

using DTAClient.Domain.Multiplayer;

using SixLabors.ImageSharp;

namespace DTAClient.DXGUI.Multiplayer
{
    public interface IMapPreviewCacheManager : IDisposable
    {
        void Clear();
        Image? RequestImage(Map map);
    }
}