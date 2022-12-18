using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ClientCore.Extensions;

namespace DTAClient.Online
{
    public sealed class CnCNetUserData
    {
        private const string FRIEND_LIST_PATH = "Client/friend_list";
        private const string IGNORE_LIST_PATH = "Client/ignore_list";
        private const string RECENT_LIST_PATH = "Client/recent_list";

        private const int RECENT_LIMIT = 50;

        /// <summary>
        /// A list which contains names of friended users. If you manipulate this list
        /// directly you have to also invoke UserFriendToggled event handler for every
        /// user name added or removed.
        /// </summary>
        public List<string> FriendList { get; private set; }

        /// <summary>
        /// A list which contains idents of ignored users. If you manipulate this list
        /// directly you have to also invoke UserIgnoreToggled event handler for every
        /// user ident added or removed.
        /// </summary>
        public List<string> IgnoreList { get; private set; }

        /// <summary>
        /// A list which contains names of players from recent games.
        /// </summary>
        public List<RecentPlayer> RecentList { get; private set; }

        public event EventHandler<UserNameEventArgs> UserFriendToggled;
        public event EventHandler<IdentEventArgs> UserIgnoreToggled;

        public CnCNetUserData(WindowManager windowManager)
        {
            windowManager.GameClosing += WindowManager_GameClosing;
        }

        public async ValueTask InitializeAsync()
        {
            FriendList = await LoadTextListAsync(FRIEND_LIST_PATH).ConfigureAwait(false);
            IgnoreList = await LoadTextListAsync(IGNORE_LIST_PATH).ConfigureAwait(false);
            RecentList = await LoadJsonListAsync<RecentPlayer>(RECENT_LIST_PATH).ConfigureAwait(false);
        }

        private static async ValueTask<List<string>> LoadTextListAsync(string path)
        {
            try
            {
                FileInfo listFile = SafePath.GetFile(ProgramConstants.GamePath, path);

                if (listFile.Exists)
                    return (await File.ReadAllLinesAsync(listFile.FullName).ConfigureAwait(false)).ToList();

                Logger.Log($"Loading {path} failed! File does not exist.");
                return new();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"Loading {path} list failed!");
                return new();
            }
        }

        private static async ValueTask<List<T>> LoadJsonListAsync<T>(string path)
        {
            try
            {
                FileInfo listFile = SafePath.GetFile(ProgramConstants.GamePath, path);

                if (listFile.Exists)
                {
                    FileStream fileStream = File.OpenRead(listFile.FullName);

                    await using (fileStream.ConfigureAwait(false))
                    {
                        return (await JsonSerializer.DeserializeAsync<List<T>>(fileStream).ConfigureAwait(false)) ?? new List<T>();
                    }
                }

                Logger.Log($"Loading {path} failed! File does not exist.");
                return new();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"Loading {path} list failed!");
                return new();
            }
        }

        private static async ValueTask SaveTextListAsync(string path, List<string> textList)
        {
            Logger.Log($"Saving {path}.");

            try
            {
                FileInfo listFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

                listFileInfo.Delete();
                await File.WriteAllLinesAsync(listFileInfo.FullName, textList).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"Saving {path} failed!");
            }
        }

        private static async ValueTask SaveJsonListAsync<T>(string path, IReadOnlyCollection<T> jsonList)
        {
            Logger.Log($"Saving {path}.");

            try
            {
                FileInfo listFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

                listFileInfo.Delete();

                FileStream fileStream = listFileInfo.OpenWrite();

                await using (fileStream.ConfigureAwait(false))
                {
                    await JsonSerializer.SerializeAsync(fileStream, jsonList).ConfigureAwait(false);
                }
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, $"Saving {path} failed!");
            }
        }

        private static void Toggle(string value, ICollection<string> list)
        {
            if (string.IsNullOrEmpty(value))
                return;

            if (list.Contains(value))
                list.Remove(value);
            else
                list.Add(value);
        }

        private void WindowManager_GameClosing(object sender, EventArgs e) => SaveAsync().HandleTask();

        private async ValueTask SaveAsync()
        {
            await SaveTextListAsync(FRIEND_LIST_PATH, FriendList).ConfigureAwait(false);
            await SaveTextListAsync(IGNORE_LIST_PATH, IgnoreList).ConfigureAwait(false);
            await SaveJsonListAsync(RECENT_LIST_PATH, RecentList).ConfigureAwait(false);
        }

        /// <summary>
        /// Adds or removes a specified user to or from the friend list
        /// depending on whether they already are on the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void ToggleFriend(string name)
        {
            Toggle(name, FriendList);
            UserFriendToggled?.Invoke(this, new(name));
        }

        /// <summary>
        /// Adds or removes a specified user to or from the chat ignore list
        /// depending on whether they already are on the ignore list.
        /// </summary>
        /// <param name="ident">The ident of the IRCUser.</param>
        public void ToggleIgnoreUser(string ident)
        {
            Toggle(ident, IgnoreList);
            UserIgnoreToggled?.Invoke(this, new(ident));
        }

        public void AddRecentPlayers(IEnumerable<string> recentPlayerNames, string gameName)
        {
            recentPlayerNames = recentPlayerNames.Where(name => name != ProgramConstants.PLAYERNAME);
            var now = DateTime.UtcNow;
            RecentList.AddRange(recentPlayerNames.Select(rp => new RecentPlayer()
            {
                PlayerName = rp,
                GameName = gameName,
                GameTime = now
            }));
            int skipCount = Math.Max(0, RecentList.Count - RECENT_LIMIT);
            RecentList = RecentList.Skip(skipCount).ToList();
        }

        /// <summary>
        /// Checks to see if a user is in the ignore list.
        /// </summary>
        /// <param name="ident">The IRC identifier of the user.</param>
        public bool IsIgnored(string ident) => IgnoreList.Contains(ident);

        /// <summary>
        /// Checks if a specified user belongs to the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public bool IsFriend(string name) => FriendList.Contains(name);
    }

    public sealed class IdentEventArgs : EventArgs
    {
        public IdentEventArgs(string ident)
        {
            Ident = ident;
        }

        public string Ident { get; }
    }
}