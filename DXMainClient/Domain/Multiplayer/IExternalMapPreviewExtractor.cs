#nullable enable
using System.Threading;
using System.Threading.Tasks;

namespace DTAClient.Domain.Multiplayer;

/// <summary>Produces a PNG at a caller-owned staging path, without publishing cache entries.
/// Kept separate from the synchronous, in-memory IMapPreviewExtractor contract.</summary>
internal interface IExternalMapPreviewExtractor
{
    Task ExtractAsync(MapPreviewRenderOptions options, string mapPath, string outputPath,
        CancellationToken cancellationToken);
}