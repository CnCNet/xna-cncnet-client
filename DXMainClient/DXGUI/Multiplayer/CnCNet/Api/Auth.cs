using ClientCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Xml.Linq;

namespace DTAClient.DXGUI.Multiplayer.CnCNet.Api
{
    class Auth
    {
        private NetworkCredential credentials;
        private string clan;
        private string username;
        private string email;
        private string defgame = ClientConfiguration.Instance.LocalGame;

        public AuthenticatedUser account;

        public Auth (NetworkCredential credentials, string username, string email)
        {
            this.credentials = credentials;
            this.username = username;
            this.email = email;
        }

        public string Login()
        {
            try
            {
                var webClient = new WebClient();
                webClient.Credentials = credentials;
               
                string response = webClient.UploadString(ProgramConstants.AUTH_URL + username, "PUT", email);
                clan = getPlayerClan();

                account = new AuthenticatedUser();
                account.Username = username;
                account.Email = email;
                account.Clan = clan;

                return response;
            }
            catch (WebException ex)
            {
                var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                switch (statusCode.ToString())
                {
                    default:
                        return "An error occurred, status code: " + statusCode;
                }
            }
        }

        private string getPlayerClan()
        {
            try
            {
                var webClient = new WebClient();
                string response = webClient.DownloadString(ProgramConstants.LADDER_API + "/ladder/" + defgame + "/player/" + username);
                return JObject.Parse(response)["clan"].ToString();
            }
            catch (WebException ex)
            {
                return "";
            }
        }
    }
}
