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

        public List<string> FriendList { get; private set; }
        public List<string> IgnoreList { get; private set; }

        public CnCNetUserData(WindowManager windowManager)
        {
            FriendList = new List<string>();
            IgnoreList = new List<string>();

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

        private void WindowManager_GameClosing(object sender, EventArgs e)
        {
            Save();
        }

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
        /// Adds or removes an user from the friend list depending on whether
        /// they already are on the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public void ToggleFriend(string name)
        {
            if (IsFriend(name))
            {
                FriendList.Remove(name);
            }
            else
            {
                FriendList.Add(name);
            }
        }

        /// <summary>
        /// Adds a specified user to the chat ignore list.
        /// </summary>
        /// <param name="ident">The ident of the IRCUser.</param>
        public void ToggleIgnoreUser(string ident)
        {
            if (string.IsNullOrEmpty(ident))
                return;

            if (IsIgnored(ident))
            {
                IgnoreList.Remove(ident);
            }
            else
            {
                IgnoreList.Add(ident);
            }
        }

        /// <summary>
        /// Checks to see if a user is in the ignore list.
        /// </summary>
        /// <param name="ident">The IRC identifier of the user.</param>
        /// <returns></returns>
        public bool IsIgnored(string ident)
        {
            if (IgnoreList == null)
                return false;

            return IgnoreList.Contains(ident);
        }

        /// <summary>
        /// Checks if a specified user belongs to the friend list.
        /// </summary>
        /// <param name="name">The name of the user.</param>
        public bool IsFriend(string name)
        {
            return FriendList.Contains(name);
        }
    }
}
