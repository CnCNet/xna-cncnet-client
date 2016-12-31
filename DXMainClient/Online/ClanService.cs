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
    public class ClanServices
    {
        WindowManager wm;
        CnCNetManager cm;
        public string ClanBot;
        public event EventHandler<ClanMemberEventArgs> ReceivedNextClanMember;
        public event EventHandler<ClanMemberEventArgs> ReceivedChangeRoleResponse;
        public event EventHandler<ClanMemberEventArgs> ReceivedRemoveMemberResponse;

        public ClanServices(WindowManager wm, CnCNetManager cm, string bot)
        {
            this.wm = wm;
            this.cm = cm;
            ClanBot = bot;
        }

        public void DoClanResponse(string message)
        {
            string[] words = message.Split(' ');
            string CNCSERV = words[0];
            string distinguisher = words[1];
            string facility = words[2];
            string command = words[3];
            string rest = String.Join(" ", words.Skip(4));
            Console.WriteLine("DoClanResponse {0}", message);

            switch (command)
            {
            case "LIST":
                if (words.Length < 5)
                    return;
                if (words[4] == "NEXT")
                    NextListClanMembers(distinguisher, words);
                if (words[4] == "COMPLETE")
                    CompleteListClanMembers(distinguisher);
                break;
            case "INVITE":
                InviteResponse(distinguisher, words);
                break;
            case "ROLE":
                ChangeRoleResponse(distinguisher, words);
                break;
            case "REMOVE":
                RemoveMemberResponse(distinguisher, words);
                break;
            case "ACCEPT":
            case "DECLINE":
            case "LIST-INVITES":
            case "LIST-CLAN-INVITES":
            case "CREATE":
            default:
                return;
            }
        }

        public void ListClanMembers(string distinguisher, string clan)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan list "+
                distinguisher +" "+ clan;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }
        private void NextListClanMembers(string d, string[] words)
        {
            if (words.Length < 8)
                Console.WriteLine("Not enough words");
            ReceivedNextClanMember?.Invoke(this,
                new ClanMemberEventArgs(d, words[4], words[5], words[6], words[7], ""));
        }
        private void CompleteListClanMembers(string d)
        {
        }


        public void Invite(string distinguisher, string user, string clan)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan invite "+
                distinguisher +" "+ user +" "+ clan;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        public void InviteResponse(string distinguisher, string[] words)
        {
        }

        public void ChangeRole(string distinguisher, string clan, string user, string role)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan role "+
                distinguisher +" "+ clan +" "+ user +" "+ role;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }
        public void ChangeRoleResponse(string distinguisher, string[] words)
        {
            if (words.Length < 8)
                return;
            string result = words[4];
            string clan = words[5];
            string user = words[6];
            string role = "";
            string rest = "";

            if (result == "FAIL")
            {
                if (words.Length > 8)
                    rest = words[7] +" "+ words[8];
                else
                    rest = words[7];
            }
            if (result == "SUCCESS")
            {
                role = words[7];
            }
            ReceivedChangeRoleResponse?.Invoke(this,
                new ClanMemberEventArgs(distinguisher, result, clan, user,
                                        role, rest));
        }

        public void RemoveMember(string distinguisher, string user, string clan)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan remove "+
                distinguisher +" "+ user +" "+ clan;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }
        public void RemoveMemberResponse(string distinguisher, string[] words)
        {
            if (words.Length < 7)
                return;
            string result = words[4];
            string clan = words[5];
            string user = words[6];
            string rest = "";
            if (result == "FAIL")
            {
                if (words.Length > 7)
                    rest = words[7] +" "+ words[8];
                else if (words.Length > 6)
                    rest = words[7];
            }
            ReceivedRemoveMemberResponse?.Invoke(this,
               new ClanMemberEventArgs(distinguisher, result, clan, user,
                                        "", rest));
        }

        public void AcceptInvite(string distinguisher, string invitation_id)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan accept "+
                distinguisher +" "+ invitation_id;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        public void DeclineInvite(string distinguisher, string invitation_id)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan decline "+
                distinguisher +" "+ invitation_id;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        public void ListClanInvites(string distinguisher, string clan)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan list-clan-invites "+
                distinguisher +" "+ clan;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }

        public void CreateClan(string distinguisher, string clan)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan create "+
                distinguisher +" "+ clan;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }


    }
    public class ClanMemberEventArgs : EventArgs
    {
        public ClanMemberEventArgs(string distinguisher, string r, string clan,
                                   string username, string role, string rest)
        {
            Distinguisher = distinguisher;
            Result = r;
            ClanName = clan;
            Member = new ClanMember(username, role);
            rest = rest;
        }
        public string Result;
        public string Distinguisher;
        public string ClanName;
        public ClanMember Member;
        public string rest;
    }
}
