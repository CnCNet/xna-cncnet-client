#nullable enable
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    public class GameModeMapCollection : IReadOnlyGameModeMapCollection
    {
        private readonly List<GameModeMap> items;
        private readonly Dictionary<string, Map> mapHashIndex;

        // Note: whenever `items` is modified, we must invalidate the cached GameModes list by setting `_gameModes` to null.
        private List<GameMode>? _gameModes = null;
        public IReadOnlyList<GameMode> GameModes => _gameModes ??= items.Select(gmm => gmm.GameMode).Distinct().ToList();

        public GameModeMapCollection(IEnumerable<GameMode> gameModes)
        {
            // Build the list of GameModeMaps
            items = gameModes.SelectMany(gm => gm.Maps.Select(map =>
                new GameModeMap(gm, map, UserINISettings.Instance.IsFavoriteMap(map.SHA1, map.UntranslatedName, gm.Name))))
                .Distinct()
                .ToList();

            // Build the hash index for fast lookups
            mapHashIndex = new Dictionary<string, Map>(StringComparer.OrdinalIgnoreCase);
            foreach (var gameModeMap in items)
            {
                var map = gameModeMap.Map;
                if (!string.IsNullOrEmpty(map.SHA1) && !mapHashIndex.ContainsKey(map.SHA1))
                    mapHashIndex[map.SHA1] = map;
            }
        }

        /// <summary>
        /// Finds a map by its SHA1 hash with optimized performance.
        /// </summary>
        /// <param name="mapHash">The SHA1 hash of the map.</param>
        /// <returns>The map if found, null otherwise.</returns>
        public Map? FindMapByHash(string mapHash)
        {
            if (string.IsNullOrEmpty(mapHash))
                return null;

            mapHashIndex.TryGetValue(mapHash, out Map? map);
            return map;
        }

        /// <summary>
        /// Adds the specified game mode map to the collection.
        /// </summary>
        /// <param name="gameModeMap">The game mode map to add to the collection.</param>
        public void Add(GameModeMap gameModeMap)
        {
            items.Add(gameModeMap);
            _gameModes = null;

            // Update the hash index
            Map? map = gameModeMap?.Map;
            if (map != null)
            {
                string sha1 = map.SHA1;

                if (!string.IsNullOrEmpty(sha1) && !mapHashIndex.ContainsKey(sha1))
                    mapHashIndex[sha1] = map;
            }
        }

        /// <summary>
        /// Adds a range of GameModeMaps to the collection and updates the hash index.
        /// </summary>
        public void AddRange(IEnumerable<GameModeMap> gameModeMapCollection)
        {
            foreach (var gameModeMap in gameModeMapCollection)
                Add(gameModeMap);
        }

        /// <summary>
        /// Removes a GameModeMap from the collection and updates the hash index if needed.
        /// </summary>
        public bool Remove(GameModeMap gameModeMap)
        {
            bool removed = items.Remove(gameModeMap);

            if (removed)
            {
                _gameModes = null;

                var map = gameModeMap.Map;
                // Only remove from index if no other GameModeMap references this map
                if (!string.IsNullOrEmpty(map.SHA1) &&
                    !items.Any(gmm => string.Equals(gmm.Map.SHA1, map.SHA1, StringComparison.OrdinalIgnoreCase)))
                    mapHashIndex.Remove(map.SHA1);
            }

            return removed;
        }

        public GameModeMap this[int index] => items[index];
        public int Count => items.Count;

        public IEnumerator<GameModeMap> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
}
