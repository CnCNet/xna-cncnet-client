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
        public event EventHandler<ClanEventArgs> ReceivedClanMemberNext;
        public event EventHandler<ClanEventArgs> ReceivedClanMemberComplete;
        public event EventHandler<ClanEventArgs> ReceivedChangeRoleResponse;
        public event EventHandler<ClanEventArgs> ReceivedRemoveMemberResponse;
        public event EventHandler<ClanEventArgs> ReceivedCreateClanResponse;
        public event EventHandler<InviteEventArgs> ReceivedListInvitesNext;
        public event EventHandler<InviteEventArgs> ReceivedListInvitesComplete;
        public event EventHandler<InviteEventArgs> ReceivedListClanInvitesNext;
        public event EventHandler<InviteEventArgs> ReceivedListClanInvitesComplete;
        public event EventHandler<InviteEventArgs> ReceivedAcceptInviteResponse;

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
            //string rest = String.Join(" ", words.Skip(4));
            Console.WriteLine("DoClanResponse {0}", message);

            switch (command)
            {
            case "LIST":
                if (words.Length < 5)
                    return;
                if (words[4] == "NEXT")
                    NextListClanMembers(distinguisher, words);
                if (words[4] == "COMPLETE")
                    CompleteListClanMembers(distinguisher, words);
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
                AcceptInviteResponse(distinguisher, words);
                break;
            case "DECLINE":
                DeclineInviteResponse(distinguisher, words);
                break;
            case "LIST-INVITES":
                if (words.Length < 5)
                    return;
                if (words[4] == "NEXT")
                    NextListInvitesResponse(distinguisher, words);
                if (words[4] == "COMPLETE")
                    CompleteListInvitesResponse(distinguisher, words);
                break;
            case "LIST-CLAN-INVITES":
                if (words.Length < 5)
                    return;
                if (words[4] == "NEXT")
                    NextListClanInvitesResponse(distinguisher, words);
                if (words[4] == "COMPLETE")
                    CompleteClanListInvitesResponse(distinguisher, words);
                break;
            case "CREATE":
                if (words.Length < 6)
                    return;
                CreateClanResponse(distinguisher, words);
                break;
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
            {
                Console.WriteLine("Not enough words");
                return;
            }
            ReceivedClanMemberNext?.Invoke(this,
                new ClanEventArgs(d, words[4], words[5], words[6], words[7], ""));
        }

        private void CompleteListClanMembers(string d, string[] words)
        {
            ReceivedClanMemberComplete?.Invoke(this,
                new ClanEventArgs(d, words[4], "", "", "", ""));
        }


        public void Invite(string distinguisher, string user, string clan, string m)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan invite "+
                distinguisher +" "+ user +" "+ clan +" "+ m;
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
                new ClanEventArgs(distinguisher, result, clan, user,
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
            string user = words[5];
            string clan = words[6];
            string rest = "";
            if (result == "FAIL")
            {
                if (words.Length > 7)
                    rest = words[7] +" "+ words[8];
                else if (words.Length > 6)
                    rest = words[7];
            }
            ReceivedRemoveMemberResponse?.Invoke(this,
               new ClanEventArgs(distinguisher, result, clan, user,
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
        public void AcceptInviteResponse(string distinguisher, string[] words)
        {
            if (words.Length < 6)
                return;
            string result = words[4];
            string id = words[5];
            string msg = "";
            if (result == "FAIL")
            {
                if (words.Length > 7)
                    msg = string.Join(" ", words.Skip(6).ToArray());

                ReceivedAcceptInviteResponse?.Invoke(this,
                   new InviteEventArgs(distinguisher, result, id, "", "", msg));
            }
            if (result == "SUCCESS")
            {
                if (words.Length < 7)
                    return;
                string clan = words[6];
                cm.CncServ.ClanName = clan;
                ReceivedAcceptInviteResponse?.Invoke(this,
                   new InviteEventArgs(distinguisher, result, id, "", clan, ""));
            }
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
        public void DeclineInviteResponse(string distinguisher, string[] words)
        {

        }

        public void ListInvites(string distinguisher)
        {
            if (String.IsNullOrEmpty(ClanBot))
                return;

            string message = "PRIVMSG "+ ClanBot +" :clan list-invites "+
                distinguisher;
            cm.SendCustomMessage(new QueuedMessage(message,
                                 QueuedMessageType.INSTANT_MESSAGE, 0));
        }
        public void NextListInvitesResponse(string distinguisher, string[] words)
        {
            if (words.Length < 8)
                return;
            string id = words[5];
            string name = words[6];
            string clan = words[7];
            string comment = String.Join(" ", words.Skip(8).ToArray());
            ReceivedListInvitesNext?.Invoke(this,
                new InviteEventArgs(distinguisher, words[4], id, name, clan, comment));
        }

        public void CompleteListInvitesResponse(string distinguisher, string[] words)
        {
            ReceivedListInvitesComplete?.Invoke(this,
                new InviteEventArgs(distinguisher, words[4], "", "", "", ""));
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

        public void NextListClanInvitesResponse(string distinguisher, string[] words)
        {
            if (words.Length < 8)
                return;
            string id = words[5];
            string name = words[6];
            string clan = words[7];
            string comment = String.Join(" ", words.Skip(8).ToArray());
            ReceivedListClanInvitesNext?.Invoke(this,
                new InviteEventArgs(distinguisher, words[4], id, name, clan, comment));
        }
        public void CompleteClanListInvitesResponse(string distinguisher,
                                                    string[] words)
        {
            ReceivedListClanInvitesComplete?.Invoke(this,
                new InviteEventArgs(distinguisher, words[4], "", "", "", ""));
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
        public void CreateClanResponse(string distinguisher, string[] words)
        {
            if (words[4] == "SUCCESS")
            {
                cm.CncServ.ClanName = words[5];
            }
            ReceivedCreateClanResponse?.Invoke(this,
                new ClanEventArgs(distinguisher, words[4], words[5], "", "",
                                  string.Join(" ", words.Skip(6).ToArray())));
        }

    }
    public class ClanEventArgs : EventArgs
    {
        public ClanEventArgs(string distinguisher, string r, string clan,
                                   string username, string role, string rest)
        {
            Distinguisher = distinguisher;
            Result = r;
            ClanName = clan;
            Member = new ClanMember(username, role);
            FailMessage = rest;
        }
        public string Result;
        public string Distinguisher;
        public string ClanName;
        public ClanMember Member;
        public string FailMessage;
    }

    public class InviteEventArgs : EventArgs
    {
        public InviteEventArgs(string distinguisher, string r, string id,
                               string name, string clan, string comment)
        {
            Distinguisher = distinguisher;
            Result = r;
            ID = id;
            ClanName = clan;
            UserName = name;
            Comment = comment;
            FailMessage = comment;
        }
        public string Distinguisher;
        public string Result;
        public string ID;
        public string ClanName;
        public string UserName;
        public string Comment;
        public string FailMessage;
    }
}
