#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;

using DTAClient.Domain.Multiplayer.LAN;

using Microsoft.Xna.Framework.Graphics;

using Rampastring.XNAUI.XNAControls;

namespace DTAClient.DXGUI.Multiplayer;

/// <summary>
/// Thread-safe manager for LAN lobby players.
/// Encapsulates all player tracking operations to ensure atomicity between
/// the player dictionary and UI updates.
/// </summary>
internal class LANPlayerManager
{
    private readonly object lockObject = new();
    private readonly Dictionary<string, LANLobbyUser> players = [];
    private readonly Dictionary<string, int> usernameToListIndex = [];
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
            if (players.TryGetValue(key, out LANLobbyUser? existingUser))
            {
                return existingUser;
            }

            // Create new user
            var newUser = new LANLobbyUser(name, gameTexture, endPoint);
            players[key] = newUser;

            // Add to UI if username not already displayed
            if (!usernameToListIndex.ContainsKey(name))
            {
                // FIXME: This logic allows multiple players with the same username but different endpoints to exist simultaneously.
                // Only the first player with a given username is shown in the UI.
                // When that player disconnects, the username is removed from the UI even if other players with the same username are still connected.
                // This can lead to invisible players.
                // Consider either enforcing unique usernames or updating the UI tracking to handle multiple players per username correctly.

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
            _ = players.TryGetValue(key, out LANLobbyUser? user);
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

            if (!players.TryGetValue(key, out LANLobbyUser? user))
                return false;

            _ = players.Remove(key);

            // Check if any other player has the same username
            bool usernameStillInUse = players.Values.Any(p => p.Name == user.Name);

            if (!usernameStillInUse && usernameToListIndex.TryGetValue(user.Name, out int index))
            {
                // Remove from UI
                _ = usernameToListIndex.Remove(user.Name);
                playerListBox.RemoveItem(index);

                // Update indices for all usernames that came after the removed one
                // We need to iterate carefully to avoid modifying the dictionary while iterating
                List<string> keysToUpdate = usernameToListIndex
                    .Where(kvp => kvp.Value > index)
                    .Select(kvp => kvp.Key)
                    .ToList();

                // Apply the updates
                foreach (string username in keysToUpdate)
                    usernameToListIndex[username]--;
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
