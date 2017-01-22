using ClientCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

namespace DTAClient.DXGUI.Multiplayer.CnCNet.Api
{
    class Auth
    {
        private NetworkCredential credentials;
        private string username;
        private string email;

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

                account = new AuthenticatedUser();
                account.Username = username;
                account.Email = email;
                account.Clan = "";

                return response;
            }
            catch (WebException ex)
            {
                var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                switch (statusCode.ToString())
                {
                    case "Unauthorized":
                        return "This account exists already, try a different username";
                    default:
                        return "An error occurred, status code: " + statusCode;
                }
            }
        }
    }
}
