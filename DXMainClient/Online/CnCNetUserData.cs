using ClientCore;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DTAClient.Online
{
    public class CnCNetUserData
    {
        private const string FRIEND_LIST_PATH = "Client\\friend_list";
        private const string IGNORE_LIST_PATH = "Client\\ignore_list";

        /// <summary>
        /// A list which contains names of friended users. If you manipulate this list
        /// directly you have to also invoke UserFriendToggled event handler for every
        /// user name added or removed.
        /// </summary>
        public List<string> FriendList { get; private set; } = new List<string>();

        /// <summary>
        /// A list which contains idents of ignored users. If you manipulate this list
        /// directly you have to also invoke UserIgnoreToggled event handler for every
        /// user ident added or removed.
        /// </summary>
        public List<string> IgnoreList { get; private set; } = new List<string>();

        public event EventHandler<UserNameEventArgs> UserFriendToggled;
        public event EventHandler<IdentEventArgs> UserIgnoreToggled;

        public CnCNetUserData(WindowManager windowManager)
        {
            try
            {
                FriendList = File.ReadAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH).ToList();
                IgnoreList = File.ReadAllLines(ProgramConstants.GamePath + IGNORE_LIST_PATH).ToList();
            }
            catch 
            {
                Logger.Log("Loading friend/ignore list failed!");
            }

            windowManager.GameClosing += WindowManager_GameClosing;
        }

        private void WindowManager_GameClosing(object sender, EventArgs e) => Save();

        public void Save()
        {
            Logger.Log("Saving friend and ignore list.");

            try
            {
                File.Delete(ProgramConstants.GamePath + FRIEND_LIST_PATH);
                File.WriteAllLines(ProgramConstants.GamePath + FRIEND_LIST_PATH,
                    FriendList.ToArray());

                File.Delete(ProgramConstants.GamePath + IGNORE_LIST_PATH);
                File.WriteAllLines(ProgramConstants.GamePath + IGNORE_LIST_PATH,
                    IgnoreList.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Log("Saving User Data failed! Error message: " + ex.Message);
            }
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
