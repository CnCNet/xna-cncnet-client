using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using ClientCore;

using Rampastring.Tools;
using Rampastring.XNAUI;

namespace DTAClient.Online
{
    public sealed class CnCNetUserData
    {
        private const string FRIEND_LIST_PATH = "Client/friend_list";
        private const string IGNORE_LIST_PATH = "Client/ignore_list";
        private const string RECENT_LIST_PATH = "Client/recent_list";

        private const int RECENT_LIMIT = 50;

        private readonly Lazy<List<string>> lazyFriendList;

        /// <summary>
        /// A list which contains names of friended users. If you manipulate this list
        /// directly you have to also invoke UserFriendToggled event handler for every
        /// user name added or removed.
        /// </summary>
        public List<string> FriendList => lazyFriendList.Value;

        private readonly Lazy<List<string>> lazyIgnoreList;

        /// <summary>
        /// A list which contains idents of ignored users. If you manipulate this list
        /// directly you have to also invoke UserIgnoreToggled event handler for every
        /// user ident added or removed.
        /// </summary>
        public List<string> IgnoreList => lazyIgnoreList.Value;

        private readonly Lazy<List<RecentPlayer>> lazyRecentList;

        /// <summary>
        /// A list which contains names of players from recent games.
        /// </summary>
        public List<RecentPlayer> RecentList => lazyRecentList.Value;

        public event EventHandler<UserNameEventArgs> UserFriendToggled;
        public event EventHandler<IdentEventArgs> UserIgnoreToggled;

        public CnCNetUserData(WindowManager windowManager)
        {
            lazyFriendList = new Lazy<List<string>>(LoadFriendList, LazyThreadSafetyMode.ExecutionAndPublication);
            lazyIgnoreList = new Lazy<List<string>>(LoadIgnoreList, LazyThreadSafetyMode.ExecutionAndPublication);
            lazyRecentList = new Lazy<List<RecentPlayer>>(LoadRecentPlayerList, LazyThreadSafetyMode.ExecutionAndPublication);

            // Load lists in background. Fire-and-forget.
            Task.Run(() => _ = FriendList);
            Task.Run(() => _ = IgnoreList);
            Task.Run(() => _ = RecentList);

            windowManager.GameClosing += WindowManager_GameClosing;
        }

        private static List<string> LoadTextList(string path)
        {
            try
            {
                FileInfo listFile = SafePath.GetFile(ProgramConstants.GamePath, path);

                if (listFile.Exists)
                    return File.ReadAllLines(listFile.FullName).ToList();

                Logger.Log($"Loading {path} failed! File does not exist.");
                return new();
            }
            catch
            {
                Logger.Log($"Loading {path} list failed!");
                return new();
            }
        }

        private static List<T> LoadJsonList<T>(string path)
        {
            try
            {
                FileInfo listFile = SafePath.GetFile(ProgramConstants.GamePath, path);

                if (listFile.Exists)
                    return JsonSerializer.Deserialize<List<T>>(File.ReadAllText(listFile.FullName)) ?? new List<T>();

                Logger.Log($"Loading {path} failed! File does not exist.");
                return new();
            }
            catch
            {
                Logger.Log($"Loading {path} list failed!");
                return new();
            }
        }

        private static void SaveTextList(string path, List<string> textList)
        {
            Logger.Log($"Saving {path}.");

            try
            {
                FileInfo listFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

                listFileInfo.Delete();
                File.WriteAllLines(listFileInfo.FullName, textList.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log($"Saving {path} failed! Error message: " + ex.ToString());
            }
        }

        private static void SaveJsonList<T>(string path, IReadOnlyCollection<T> jsonList)
        {
            Logger.Log($"Saving {path}.");

            try
            {
                FileInfo listFileInfo = SafePath.GetFile(ProgramConstants.GamePath, path);

                listFileInfo.Delete();
                File.WriteAllText(listFileInfo.FullName, JsonSerializer.Serialize(jsonList));
            }
            catch (Exception ex)
            {
                Logger.Log($"Saving {path} failed! Error message: " + ex.ToString());
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

        private List<string> LoadFriendList() => LoadTextList(FRIEND_LIST_PATH);

        private List<string> LoadIgnoreList() => LoadTextList(IGNORE_LIST_PATH);

        private List<RecentPlayer> LoadRecentPlayerList() => LoadJsonList<RecentPlayer>(RECENT_LIST_PATH);

        private void WindowManager_GameClosing(object sender, EventArgs e) => Save();

        private void SaveFriends() => SaveTextList(FRIEND_LIST_PATH, FriendList);

        private void SaveIgnoreList() => SaveTextList(IGNORE_LIST_PATH, IgnoreList);

        private void SaveRecentList() => SaveJsonList(RECENT_LIST_PATH, RecentList);

        private void Save()
        {
            SaveFriends();
            SaveIgnoreList();
            SaveRecentList();
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
            RecentList.RemoveRange(0, skipCount);
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