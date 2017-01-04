using ClientCore;
using ClientCore.CnCNet5;
using DTAClient.Domain.Multiplayer.CnCNet;
using DTAClient.Online.EventArguments;
using Microsoft.Xna.Framework;
using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DTAClient.Online.Services
{
    public class CnCNetServices
    {
        CnCNetManager cm;
        WindowManager wm;
        public bool IsAuthenticated;
        public bool AlwaysConnect;
        public bool ConnectOnce;
        public string UserName;
        public string Password;
        public string ClanName;

        public ClanServices ClanServices;
        public string ServNick;
        string ServUserName;

        public event EventHandler<CnCNetServAuthEventArgs> AuthResponse;

        public CnCNetServices(WindowManager wm, CnCNetManager cm)
        {
            this.cm = cm;
            this.wm = wm;
            IsAuthenticated = false;
            ServNick = "dkbot";
            ClanServices = new ClanServices(wm, cm, ServNick);

            cm.Connected += Authenticate;
            cm.CTCPMessageReceived += DoCTCPMessageReceived;
            cm.UserAdded += DoUserAdded;
        }
        private void _authenticate(string u, string p)
        {
            string message = "PRIVMSG "+ ServNick +" :auth auth dist "+ u +" "+ p;
            Console.WriteLine(message);
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        public void Authenticate()
        {
            Authenticate(null,null);
        }

        public void Authenticate(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(UserName) || String.IsNullOrEmpty(Password))
                return;

            if (AlwaysConnect)
            {
                _authenticate(UserName, Password);
            }
            else if (ConnectOnce)
            {
                ConnectOnce = false;
                _authenticate(UserName, Password);
                Password = "";
            }
        }


        private void DoCTCPMessageReceived(object s, CTCPEventArgs a)
        {
            if (a.Sender == "dkbot"
                && a.CTCPMessage.StartsWith("CNCSERV", StringComparison.CurrentCulture))
            {
                string[] words = a.CTCPMessage.Split(' ');
                if (words.Length < 4)
                {
                    Console.WriteLine("words is less than 4 = {0}", words.Length);
                    return;
                }
                string CNCSERV = words[0];
                string distinguisher = words[1];
                string facility = words[2];
                string command = words[3];
                Console.WriteLine("CNCSERV = {0}, dist = {1}, facility = {2}, command = {3}, rest = {4}",
                                  CNCSERV, distinguisher, facility, command, a.CTCPMessage);
                switch (facility)
                {
                case "AUTH":
                    DoAuthResponse(words);
                    break;
                case "CLAN":
                    ClanServices.DoClanResponse(a.CTCPMessage);
                    break;
                default:
                    return;
                }
            }
        }

        private void DoAuthResponse(string[] words)
        {
            if (words.Length < 5)
                return;
            string CNCSERV = words[0];
            string distinguisher = words[1];
            string facility = words[2];
            string command = words[3];
            string result = words[4];
            string rest = String.Join(" ", words.Skip(5).ToArray());

            switch (command)
            {
            case "AUTH":
                if (result == "FAIL")
                    IsAuthenticated = false;

                else if (result == "SUCCESS")
                {
                    try
                    {
                        IsAuthenticated = true;
                        UserName = words[5];
                        ClanName = words[6];
                    }
                    catch
                    {
                        IsAuthenticated = false;
                    }
                }
                break;
            case "DEAUTH":
                break;
            default:
                break;
            }
            var a = new CnCNetServAuthEventArgs(command, result, UserName,
                                                ClanName, rest);
            AuthResponse?.Invoke(this, a);
        }

        private void DoUserAdded(object s, UserEventArgs u)
        {
            Console.WriteLine("{0} {1}",u.User.Name, u.User.Hostname);
        }

    }
    public class ClanMember
    {
        public ClanMember(string n, string r)
        {
            Name = n;
            Role = r;
        }
        public string Name;
        public string Role;
    }

    public class CnCNetServAuthEventArgs : EventArgs
    {
        public CnCNetServAuthEventArgs(string command, string r, string username,
                                       string clanname, string failmessage)
        {
            Command = command;
            Result = r;
            UserName = username;
            ClanName = clanname;
            FailMessage = failmessage;
        }
        public string Command;
        public string Result;
        public string UserName;
        public string ClanName;
        public string FailMessage;
    }
}
