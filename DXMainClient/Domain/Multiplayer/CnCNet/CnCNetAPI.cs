using ClientCore;
using DTAClient.Online;
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
    public class CnCNetAPI
    {
        public event Action<bool> Initialized;
        public event Action<object> AccountUpdated;
        public event Action<List<VerifiedAccounts>> VerifyAccountsComplete;

        public static string API_URL = "http://cncnet-api/api/v1/";
        public static string API_REGISTER_URL = "https://ladder.cncnet.org/auth/register";

        private const string API_AUTH_LOGIN = "auth/login";
        private const string API_USER_NICKNAMES = "client/accounts/user/nicknames";
        private const string API_IDENTS_VERIFY = "client/accounts/verify";

        public string Nickname { get; set; }
        public string AuthToken { get; private set; }
        public bool IsAuthed { get; private set; }

        public List<AuthPlayer> Accounts { get; private set; }
        public string ErrorMessage { get; private set; }

        private const int REQUEST_TIMEOUT = 10000; // In milliseconds
        private readonly string tokenPath = "SOFTWARE\\CnCNet\\QuickMatch";

        public CnCNetAPI()
        {

        }

        private static CnCNetAPI _instance;
        public static CnCNetAPI Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new CnCNetAPI();
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
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);

                    var request = new NameValueCollection();
                    request.Add("game", ClientConfiguration.Instance.LocalGame.ToLower());
                    request.Add("ident", Connection.GetId());

                    byte[] responsebytes = client.UploadValues(API_URL + API_USER_NICKNAMES, "POST", request);

                    string response = Encoding.UTF8.GetString(responsebytes);

                    List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                    Accounts = accounts;

                    AccountUpdated?.Invoke(this);

                    return true;
                }
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
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    var request = new NameValueCollection();
                    request.Add("email", email);
                    request.Add("password", password);

                    byte[] responsebytes = client.UploadValues(API_URL + API_AUTH_LOGIN, "POST", request);
                    string response = Encoding.UTF8.GetString(responsebytes);

                    AuthToken authToken = JsonConvert.DeserializeObject<AuthToken>(response);
                    AuthToken = authToken.Token;

                    RegistryKey key = Registry.CurrentUser.CreateSubKey(tokenPath);
                    key.SetValue("accessToken", AuthToken);
                    key.Close();

                    bool success = GetAccounts();

                    return success;
                }
            }
            catch (WebException ex)
            {
                var statusCode = ((HttpWebResponse)ex.Response).StatusCode;
                switch (statusCode.ToString())
                {
                    case "Unauthorized":
                        ErrorMessage = "You have entered an incorrect email or password";
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
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);

                    byte[] responsebytes = client.DownloadData(API_URL + API_USER_NICKNAMES);
                    string response = Encoding.UTF8.GetString(responsebytes);

                    List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                    Accounts = accounts;

                    AccountUpdated?.Invoke(this);

                    return true;
                }
            }
            catch (WebException ex)
            {
                return false;
            }
        }

        /// <summary>
        /// Verify User Accounts
        /// </summary>
        /// <param name="idents"></param>
        /// 
        public void VerifyAccounts(List<string> idents)
        {
            using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
            {
                string jsonIdents = JsonConvert.SerializeObject(idents);

                var request = new NameValueCollection();
                request.Add("idents", jsonIdents);
                request.Add("game", ClientConfiguration.Instance.LocalGame.ToLower());

                byte[] responsebytes = client.UploadValues(API_URL + API_IDENTS_VERIFY, "POST", request);
                string response = Encoding.UTF8.GetString(responsebytes);

                List<VerifiedAccounts> verifiedAccounts = JsonConvert.DeserializeObject<List<VerifiedAccounts>>(response);
                VerifyAccountsComplete?.Invoke(verifiedAccounts);
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

public class VerifiedAccounts
{
    public string ident { get; set; }
    public List<PlayerAccount> nicknames { get; set; }
}

public class PlayerAccount
{
    public string username { get; set; }
}