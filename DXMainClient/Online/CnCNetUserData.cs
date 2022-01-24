using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace DTAClient.Online
{
    public class CnCNetUserData
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
        public List<string> FriendList { get; private set;  } = new List<string>();

        /// <summary>
        /// A list which contains idents of ignored users. If you manipulate this list
        /// directly you have to also invoke UserIgnoreToggled event handler for every
        /// user ident added or removed.
        /// </summary>
        public List<string> IgnoreList { get; private set; } = new List<string>();

        /// <summary>
        /// A list which contains names of players from recent games.
        /// </summary>
        public List<RecentPlayer> RecentList { get; private set; } = new List<RecentPlayer>();

        public event EventHandler<UserNameEventArgs> UserFriendToggled;
        public event EventHandler<IdentEventArgs> UserIgnoreToggled;

        public CnCNetUserData(WindowManager windowManager)
        {
            LoadFriendList();
            LoadIgnoreList();
            LoadRecentPlayerList();

            windowManager.GameClosing += WindowManager_GameClosing;
        }

        private void LoadFriendList()
        {
            try
            {
                FriendList = File.ReadAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH).ToList();
            }
            catch
            {
                Logger.Log("Loading friend list failed!");
                FriendList = new List<string>();
            }
        }

        private void LoadIgnoreList()
        {
            try
            {
                IgnoreList = File.ReadAllLines(ProgramConstants.GamePath + IGNORE_LIST_PATH).ToList();
            }
            catch
            {
                Logger.Log("Loading ignore list failed!");
                IgnoreList = new List<string>();
            }
        }

        private void LoadRecentPlayerList()
        {
            try
            {
                RecentList = JsonConvert.DeserializeObject<List<RecentPlayer>>(File.ReadAllText(ProgramConstants.GamePath + RECENT_LIST_PATH)) ?? new List<RecentPlayer>();
            }
            catch
            {
                Logger.Log("Loading recent player list failed!");
                RecentList = new List<RecentPlayer>();
            }
        }

        private void WindowManager_GameClosing(object sender, EventArgs e) => Save();

        private void SaveFriends()
        {
            Logger.Log("Saving friend list.");

            try
            {
                File.Delete(ProgramConstants.GamePath + FRIEND_LIST_PATH);
                File.WriteAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH,
                    FriendList.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log("Saving friends failed! Error message: " + ex.Message);
            }
        }

        private void SaveIgnoreList()
        {
            Logger.Log("Saving ignore list.");

            try
            {
                File.Delete(ProgramConstants.GamePath + IGNORE_LIST_PATH);
                File.WriteAllLines(ProgramConstants.GamePath + IGNORE_LIST_PATH,
                    IgnoreList.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log("Saving ignore list failed! Error message: " + ex.Message);
            }
        }

        private void SaveRecentList()
        {
            Logger.Log("Saving recent list.");

            try
            {
                File.Delete(ProgramConstants.GamePath + RECENT_LIST_PATH);
                File.WriteAllText(ProgramConstants.GamePath + RECENT_LIST_PATH, JsonConvert.SerializeObject(RecentList));
            }
            catch (Exception ex)
            {
                Logger.Log("Saving recent players list failed! Error message: " + ex.Message);
            }
        }

        public void Save()
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
            if (string.IsNullOrEmpty(name))
                return;

            if (IsFriend(name))
                FriendList.Remove(name);
            else
                FriendList.Add(name);

            UserFriendToggled?.Invoke(this, new UserNameEventArgs(name));
        }

        /// <summary>
        /// Adds or removes a specified user to or from the chat ignore list
        /// depending on whether they already are on the ignore list.
        /// </summary>
        /// <param name="ident">The ident of the IRCUser.</param>
        public void ToggleIgnoreUser(string ident)
        {
            if (string.IsNullOrEmpty(ident))
                return;

            if (IsIgnored(ident))
                IgnoreList.Remove(ident);
            else
                IgnoreList.Add(ident);

            UserIgnoreToggled?.Invoke(this, new IdentEventArgs(ident));
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
        /// <returns></returns>
        public bool IsIgnored(string ident) => IgnoreList.Contains(ident);

        /// <summary>
        /// Checks if a specified user belongs to the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public bool IsFriend(string name) => FriendList.Contains(name);
    }

    public class IdentEventArgs : EventArgs
    {
        public IdentEventArgs(string ident)
        {
            Ident = ident;
        }

        public string Ident { get; private set; }
    }
}
