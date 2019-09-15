using Microsoft.Win32;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class CnCNetAuthApi
    {
        public event Action<bool> Initialized;
        public event Action<object> AccountUpdated;

        public static string API_URL = "https://ladder.cncnet.org/api/v1/";
        public static string API_REGISTER_URL = "https://ladder.cncnet.org/auth/register";

        public string Nickname { get; set; }
        public string AuthToken { get; private set; }
        public bool IsAuthed { get; private set; }

        public List<AuthPlayer> Accounts { get; private set; }
        public string ErrorMessage { get; private set; }

        private string tokenPath = "SOFTWARE\\CnCNet\\QuickMatch";

        public CnCNetAuthApi()
        {
        }

        private static CnCNetAuthApi _instance;
        public static CnCNetAuthApi Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CnCNetAuthApi();
                }
                return _instance;
            }
        }

        /// <summary>
        /// Checks and verifies auth token is active, 
        /// Retreives latest account data
        /// </summary>
        public void InitializeAccount()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(tokenPath);
                key.GetValue("accessToken", "");
                if (key != null)
                {
                    AuthToken = key.GetValue("accessToken", "").ToString();
                }
                key.Close();

                IsAuthed = VerifyToken();

                Initialized?.Invoke(IsAuthed);
            }
            catch
            {
                Initialized?.Invoke(false);
                Console.Write("Failed to get access token for QM account");
            }
        }

        /// <summary>
        /// Verifies token works
        /// </summary>
        /// <returns></returns>
        private bool VerifyToken()
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);

                byte[] responsebytes = webClient.DownloadData(API_URL + "user/account");
                string response = Encoding.UTF8.GetString(responsebytes);

                List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                Accounts = accounts;

                AccountUpdated?.Invoke(this);

                webClient.Dispose();

                return true;
            }
            catch (WebException ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Used to login and get Auth Token
        /// </summary>
        /// <param name="email"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public bool Login(string email, string password)
        {
            try
            {
                WebClient webClient = new WebClient();

                var request = new NameValueCollection();
                request.Add("email", email);
                request.Add("password", password);

                byte[] responsebytes = webClient.UploadValues(API_URL + "auth/login", "POST", request);
                string response = Encoding.UTF8.GetString(responsebytes);

                AuthToken authToken = JsonConvert.DeserializeObject<AuthToken>(response);
                AuthToken = authToken.Token;

                RegistryKey key = Registry.CurrentUser.CreateSubKey(tokenPath);
                key.SetValue("accessToken", AuthToken);
                key.Close();

                bool success = GetAccounts();
                webClient.Dispose();

                return success;
            }
            catch (WebException ex)
            {
                var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                switch (statusCode.ToString())
                {
                    case "Unauthorized":
                        ErrorMessage = "This account exists already, try a different username";
                        break;

                    default:
                        ErrorMessage = "An error occurred, status code: " + statusCode;
                        break;
                }
                return false;
            }
        }

        public void Logout()
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.CreateSubKey(tokenPath);
                key.SetValue("accessToken", "");
                key.Close();
            }
            catch
            {
            }

            IsAuthed = false;
            Accounts.Clear();
        }

        /// <summary>
        /// Gets players nicknames names from their account
        /// </summary>
        /// <returns></returns>
        public bool GetAccounts()
        {
            try
            {
                WebClient webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);

                byte[] responsebytes = webClient.DownloadData(API_URL + "user/account");
                string response = Encoding.UTF8.GetString(responsebytes);

                List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                Accounts = accounts;

                webClient.Dispose();

                AccountUpdated?.Invoke(this);

                return true;
            }
            catch (WebException ex)
            {
                return false;
            }
        }
    }
}

public class AuthToken
{
    public string Token { get; set; }
}

public class AuthPlayer
{
    public string username { get; set; }
}
