#nullable enable
using System;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

public interface IMapPreviewCacheManager : ICacheManager<Map, Image> { }