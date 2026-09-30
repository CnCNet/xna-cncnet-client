#nullable enable
using ClientCore.Caching;

using SixLabors.ImageSharp;

namespace DTAClient.Domain.Multiplayer;

public interface IMapPreviewCacheManager : ICacheManager<Map, Image> { }