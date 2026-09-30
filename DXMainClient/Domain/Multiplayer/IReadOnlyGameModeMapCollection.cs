#nullable enable
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer
{
    public interface IReadOnlyGameModeMapCollection : IReadOnlyList<GameModeMap>
    {
        public IReadOnlyList<GameMode> GameModes { get; }
        public Map? FindMapByHash(string mapHash);
    }
}
