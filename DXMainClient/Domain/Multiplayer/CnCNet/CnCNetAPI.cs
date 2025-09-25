using ClientCore;
using DTAClient.Online;
using Microsoft.Win32;
using Newtonsoft.Json;
using Rampastring.Tools;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Net;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class CnCNetAPI
    {
        public event Action<bool> Initialized;
        public event Action<object> AccountUpdated;
        public event Action<List<VerifiedAccounts>> VerifyAccountsComplete;
        // Registration URL is separate (web)
        public static string API_REGISTER_URL = "https://ladder.cncnet.org/auth/register";

    private const string API_AUTH_LOGIN = "auth/login";
    // Official API: GET /api/v1/user/account (auth: Bearer <JWT>)
    private const string API_USER_ACCOUNT = "user/account";
    private const string API_IDENTS_VERIFY = "client/accounts/verify";

        private static string ApiBaseUrl
        {
            get
            {
                // Read from configuration, with sensible default for local dev.
                // Setting this value inside ClientDefinitions.ini overrides built-in value.
                string url = ClientConfiguration.Instance.CnCNetApiUrl ?? "http://cncnet-api/api/v1/";
                if (!url.EndsWith("/"))
                    url += "/";
                return url;
            }
        }

        public string Nickname { get; set; }
        public string AuthToken { get; private set; }
        public bool IsAuthed { get; private set; }

        public List<AuthPlayer> Accounts { get; private set; } = new List<AuthPlayer>();
        public string ErrorMessage { get; private set; }

        private const int REQUEST_TIMEOUT = 10000; // In milliseconds
        private readonly string tokenPath = "SOFTWARE\\CnCNet\\QuickMatch";
        private static string TokenFilePath => SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "access.token");

        public CnCNetAPI() { }

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
        /// Checks and verifies auth token is active, retrieves latest account data
        /// </summary>
        public void InitializeAccount()
        {
            try
            {
                AuthToken = ReadAuthToken();

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
        /// Verifies token by calling an authenticated endpoint
        /// </summary>
        private bool VerifyToken()
        {
            try
            {
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);
                    // Call /user/account to validate the token and also refresh local account data
                    byte[] responsebytes = client.DownloadData(ApiBaseUrl + API_USER_ACCOUNT);
                    string response = Encoding.UTF8.GetString(responsebytes);
                    List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                    Accounts = accounts ?? new List<AuthPlayer>();

                    AccountUpdated?.Invoke(this);

                    return true;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <summary>
        /// Used to login and get Auth Token
        /// </summary>
        public bool Login(string email, string password)
        {
            try
            {
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    var request = new NameValueCollection();
                    request.Add("email", email);
                    request.Add("password", password);

                    byte[] responsebytes = client.UploadValues(ApiBaseUrl + API_AUTH_LOGIN, "POST", request);
                    string response = Encoding.UTF8.GetString(responsebytes);

                    AuthTokenResponse authToken = JsonConvert.DeserializeObject<AuthTokenResponse>(response);
                    AuthToken = authToken?.Token;

                    WriteAuthToken(AuthToken ?? string.Empty);

                    bool success = GetAccounts();

                    return success;
                }
            }
            catch (WebException ex)
            {
                var http = ex.Response as HttpWebResponse;
                var statusCode = http?.StatusCode.ToString() ?? ex.Status.ToString();
                switch (statusCode)
                {
                    case "Unauthorized":
                        ErrorMessage = "You have entered an incorrect email or password";
                        break;
                    case "NotFound":
                        ErrorMessage = "Login service endpoint not found. Please verify CnCNetApiUrl points to the API base (e.g. https://ladder.cncnet.org/api/v1/).";
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
                ClearAuthToken();
            }
            catch { }

            IsAuthed = false;
            AuthToken = string.Empty;
            Accounts.Clear();
        }

        private string ReadAuthToken()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.OpenSubKey(tokenPath);
                    if (key != null)
                    {
                        string token = key.GetValue("accessToken", "").ToString();
                        key.Close();
                        return token;
                    }
                    return string.Empty;
                }
                // Non-Windows: read from file under Client user files
                var fi = SafePath.GetFile(TokenFilePath);
                if (fi.Exists)
                {
                    return File.ReadAllText(fi.FullName).Trim();
                }
                return string.Empty;
            }
            catch { return string.Empty; }
        }

        private void WriteAuthToken(string token)
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(tokenPath);
                    key.SetValue("accessToken", token ?? string.Empty);
                    key.Close();
                }
                else
                {
                    // Ensure directory exists
                    DirectoryInfo dir = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
                    if (!dir.Exists) dir.Create();
                    File.WriteAllText(TokenFilePath, token ?? string.Empty);
                }
            }
            catch { }
        }

        private void ClearAuthToken()
        {
            try
            {
                if (OperatingSystem.IsWindows())
                {
                    RegistryKey key = Registry.CurrentUser.CreateSubKey(tokenPath);
                    key.SetValue("accessToken", "");
                    key.Close();
                }
                else
                {
                    var fi = SafePath.GetFile(TokenFilePath);
                    if (fi.Exists) fi.Delete();
                }
            }
            catch { }
        }

        /// <summary>
        /// Gets nick names from their account (active for current month)
        /// </summary>
        public bool GetAccounts()
        {
            try
            {
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);
                    byte[] responsebytes = client.DownloadData(ApiBaseUrl + API_USER_ACCOUNT);
                    string response = Encoding.UTF8.GetString(responsebytes);

                    List<AuthPlayer> accounts = JsonConvert.DeserializeObject<List<AuthPlayer>>(response);
                    Accounts = accounts ?? new List<AuthPlayer>();

                    AccountUpdated?.Invoke(this);

                    return true;
                }
            }
            catch (WebException)
            {
                return false;
            }
        }

        /// <summary>
        /// Verify User Accounts
        /// </summary>
        public void VerifyAccounts(List<string> idents)
        {
            string url = ApiBaseUrl + API_IDENTS_VERIFY;
            try
            {
                using (ExtendedWebClient client = new ExtendedWebClient(REQUEST_TIMEOUT))
                {
                    // Include Authorization if available; some deployments require auth for this endpoint
                    if (!string.IsNullOrEmpty(AuthToken))
                    {
                        client.Headers.Add(HttpRequestHeader.Authorization, "Bearer " + AuthToken);
                    }

                    string jsonIdents = JsonConvert.SerializeObject(idents);

                    var request = new NameValueCollection();
                    request.Add("idents", jsonIdents);
                    string game = ClientConfiguration.Instance.LocalGame.ToLower();
                    request.Add("game", game);

                    byte[] responsebytes = client.UploadValues(url, "POST", request);
                    string response = Encoding.UTF8.GetString(responsebytes);

                    List<VerifiedAccounts> verifiedAccounts = JsonConvert.DeserializeObject<List<VerifiedAccounts>>(response);
                    Logger.Log($"VerifyAccounts OK: sent={idents?.Count ?? 0}, game='{game}', received={verifiedAccounts?.Count ?? 0}");
                    VerifyAccountsComplete?.Invoke(verifiedAccounts ?? new List<VerifiedAccounts>());
                }
            }
            catch (WebException ex)
            {
                var http = ex.Response as HttpWebResponse;
                Logger.Log($"CnCNetAPI.VerifyAccounts failed ({http?.StatusCode ?? 0}) at {url}: {ex.Message}");
                // Do not crash the client; just report no verified accounts so UI stays responsive
                VerifyAccountsComplete?.Invoke(new List<VerifiedAccounts>());
            }
            catch (Exception ex)
            {
                Logger.Log($"CnCNetAPI.VerifyAccounts unexpected error at {url}: {ex.Message}");
                VerifyAccountsComplete?.Invoke(new List<VerifiedAccounts>());
            }
        }
    }

    public class AuthTokenResponse
    {
        [JsonProperty("token")]
        public string Token { get; set; }
    }

    public class AuthPlayer
    {
        [JsonProperty("username")]
        public string username { get; set; }
    }

    public class VerifiedAccounts
    {
        [JsonProperty("ident")]
        public string ident { get; set; }
        [JsonProperty("nicknames")]
        public List<PlayerAccount> nicknames { get; set; }
    }

    public class PlayerAccount
    {
        [JsonProperty("username")]
        public string username { get; set; }
    }
}
