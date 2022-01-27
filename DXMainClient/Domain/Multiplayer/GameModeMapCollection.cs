using System.Collections.Generic;
using System.Linq;
using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    public class GameModeMapCollection : List<GameModeMap>
    {
        public GameModeMapCollection(IEnumerable<GameMode> gameModes) :
            base(gameModes.SelectMany(gm => gm.Maps.Select(map =>
                new GameModeMap(gm, map, UserINISettings.Instance.IsFavoriteMap(map.Name, gm.Name)))).Distinct())
        {
        }

        public List<GameMode> GameModes => this.Select(gmm => gmm.GameMode).Distinct().ToList();
    }
}
