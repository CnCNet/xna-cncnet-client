using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using ClientCore;

namespace DTAClient.Domain.Multiplayer
{
    /// <summary>
    /// An optimized collection of GameModeMaps with O(1) map lookup by SHA1 hash.
    /// </summary>
    public class GameModeMapCollection : IReadOnlyGameModeMapCollection
    {
        private readonly List<GameModeMap> items;
        private readonly Dictionary<string, Map> mapHashIndex;

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
                {
                    mapHashIndex[map.SHA1] = map;
                }
            }
        }

        public IReadOnlyList<GameMode> GameModes => items.Select(gmm => gmm.GameMode).Distinct().ToList();

        /// <summary>
        /// Finds a map by its SHA1 hash with O(1) performance.
        /// </summary>
        /// <param name="mapHash">The SHA1 hash of the map.</param>
        /// <returns>The map if found, null otherwise.</returns>
        public Map FindMapByHash(string mapHash)
        {
            if (string.IsNullOrEmpty(mapHash))
                return null;

            mapHashIndex.TryGetValue(mapHash, out Map map);
            return map;
        }

        /// <summary>
        /// Adds a range of GameModeMaps to the collection and updates the hash index.
        /// </summary>
        public void AddRange(IEnumerable<GameModeMap> gameModeMapCollection)
        {
            foreach (var gameModeMap in gameModeMapCollection)
            {
                items.Add(gameModeMap);
                
                // Update the hash index
                var map = gameModeMap.Map;
                if (!string.IsNullOrEmpty(map.SHA1) && !mapHashIndex.ContainsKey(map.SHA1))
                {
                    mapHashIndex[map.SHA1] = map;
                }
            }
        }

        /// <summary>
        /// Removes a GameModeMap from the collection and updates the hash index if needed.
        /// </summary>
        public bool Remove(GameModeMap gameModeMap)
        {
            bool removed = items.Remove(gameModeMap);
            
            if (removed)
            {
                var map = gameModeMap.Map;
                // Only remove from index if no other GameModeMap references this map
                if (!string.IsNullOrEmpty(map.SHA1) && 
                    !items.Any(gmm => gmm.Map.SHA1 == map.SHA1))
                {
                    mapHashIndex.Remove(map.SHA1);
                }
            }
            
            return removed;
        }

        // IReadOnlyList<GameModeMap> implementation
        public GameModeMap this[int index] => items[index];
        public int Count => items.Count;

        public IEnumerator<GameModeMap> GetEnumerator() => items.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => items.GetEnumerator();
    }
}
