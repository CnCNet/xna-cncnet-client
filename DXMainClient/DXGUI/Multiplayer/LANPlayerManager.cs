#nullable enable
using DTAClient.Domain.Multiplayer.LAN;

using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI.XNAControls;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

namespace DTAClient.DXGUI.Multiplayer
{
    /// <summary>
    /// Thread-safe manager for LAN lobby players.
    /// Encapsulates all player tracking operations to ensure atomicity between
    /// the player dictionary and UI updates.
    /// </summary>
    internal class LANPlayerManager
    {
        private readonly object lockObject = new object();
        private readonly Dictionary<string, LANLobbyUser> players = new Dictionary<string, LANLobbyUser>();
        private readonly Dictionary<string, int> usernameToListIndex = new Dictionary<string, int>();
        private readonly XNAListBox playerListBox;

        /// <summary>
        /// Initializes a new instance of the LANPlayerManager class with the specified player list box.
        /// 
        /// Note: after passing the XNAListBox, do not modify the XNAListBox directly! Use the methods of this class to ensure thread safety.
        /// </summary>
        /// <param name="playerListBox">The XNAListBox control that displays the list of players in the LAN session. Cannot be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if playerListBox is null.</exception>
        public LANPlayerManager(XNAListBox playerListBox)
        {
            this.playerListBox = playerListBox ?? throw new ArgumentNullException(nameof(playerListBox));
        }

        private static string GetKeyFromEndPoint(IPEndPoint endPoint)
            => endPoint.ToString();

        /// <summary>
        /// Gets or creates a player. Returns the LANLobbyUser instance (either newly created or existing).
        /// This operation is atomic - both the internal dictionary and UI are updated together.
        /// </summary>
        /// <param name="endPoint">The endpoint (IP:Port) that uniquely identifies this connection.</param>
        /// <param name="name">The player's username.</param>
        /// <param name="gameTexture">The game icon texture.</param>
        /// <returns>The LANLobbyUser instance (either newly created or existing).</returns>
        public LANLobbyUser GetOrCreatePlayer(IPEndPoint endPoint, string name, Texture2D gameTexture)
        {
            lock (lockObject)
            {
                string key = GetKeyFromEndPoint(endPoint);

                // If this endpoint already exists, return the existing user
                if (players.TryGetValue(key, out var existingUser))
                {
                    return existingUser;
                }

                // Create new user
                var newUser = new LANLobbyUser(name, gameTexture, endPoint);
                players[key] = newUser;

                // Add to UI if username not already displayed
                if (!usernameToListIndex.ContainsKey(name))
                {
                    int index = playerListBox.Items.Count;
                    usernameToListIndex[name] = index;
                    playerListBox.AddItem(name, gameTexture);
                }

                return newUser;
            }
        }

        /// <summary>
        /// Attempts to get a player by endpoint.
        /// </summary>
        public LANLobbyUser? GetPlayerIfExist(IPEndPoint endPoint)
        {
            lock (lockObject)
            {
                string key = GetKeyFromEndPoint(endPoint);
                players.TryGetValue(key, out var user);
                return user;
            }
        }

        /// <summary>
        /// Removes a player by endpoint. This operation is atomic.
        /// </summary>
        /// <returns>True if the player was removed, false if not found.</returns>
        public bool RemovePlayer(IPEndPoint endPoint)
        {
            lock (lockObject)
            {
                string key = GetKeyFromEndPoint(endPoint);

                if (!players.TryGetValue(key, out var user))
                {
                    return false;
                }

                players.Remove(key);

                // Check if any other player has the same username
                bool usernameStillInUse = players.Values.Any(p => p.Name == user.Name);

                if (!usernameStillInUse && usernameToListIndex.TryGetValue(user.Name, out int index))
                {
                    // Remove from UI
                    usernameToListIndex.Remove(user.Name);
                    playerListBox.RemoveItem(index);

                    // Update indices for all usernames that came after the removed one
                    // We need to iterate carefully to avoid modifying the dictionary while iterating
                    var keysToUpdate = new List<string>();
                    foreach (var kvp in usernameToListIndex)
                    {
                        if (kvp.Value > index)
                        {
                            keysToUpdate.Add(kvp.Key);
                        }
                    }

                    // Apply the updates
                    foreach (var username in keysToUpdate)
                    {
                        usernameToListIndex[username]--;
                    }
                }

                return true;
            }
        }

        /// <summary>
        /// Gets a thread-safe snapshot of all players.
        /// </summary>
        public List<LANLobbyUser> GetAllPlayers()
        {
            lock (lockObject)
            {
                return players.Values.ToList();
            }
        }

        /// <summary>
        /// Clears all players from both internal tracking and UI.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                players.Clear();
                usernameToListIndex.Clear();
                playerListBox.Clear();
            }
        }

        /// <summary>
        /// Gets the current player count.
        /// </summary>
        public int Count
        {
            get
            {
                lock (lockObject)
                {
                    return players.Count;
                }
            }
        }
    }
}
