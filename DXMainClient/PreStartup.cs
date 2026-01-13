using System;
#if WINFORMS
using System.Windows.Forms;
#endif
using System.Diagnostics;
using System.IO;
using DTAClient.Domain;
using Rampastring.Tools;
using ClientCore;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Collections.Generic;
using ClientCore.Extensions;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using ClientCore.I18N;
using System.Globalization;
using System.Security;
using System.Transactions;

namespace DTAClient
{
    /// <summary>
    /// Contains client startup parameters.
    /// </summary>
    struct StartupParams
    {
        public StartupParams(bool noAudio, bool multipleInstanceMode,
            List<string> unknownParams)
        {
            NoAudio = noAudio;
            MultipleInstanceMode = multipleInstanceMode;
            UnknownStartupParams = unknownParams;
        }

        public bool NoAudio { get; }
        public bool MultipleInstanceMode { get; }
        public List<string> UnknownStartupParams { get; }
    }

    static class PreStartup
    {
        /// <summary>
        /// Initializes various basic systems like the client's logger, 
        /// constants, and the general exception handler.
        /// Reads the user's settings from an INI file, 
        /// checks for necessary permissions and starts the client if
        /// everything goes as it should.
        /// </summary>
        /// <param name="parameters">The client's startup parameters.</param>
        public static void Initialize(StartupParams parameters)
        {
            Translation.InitialUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo(ProgramConstants.HARDCODED_LOCALE_CODE);

#if WINFORMS
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.ThrowException);
            Application.ThreadException += (sender, args) => HandleException(sender, args.Exception);
#endif
            AppDomain.CurrentDomain.UnhandledException += (sender, args) => HandleException(sender, (Exception)args.ExceptionObject);

            DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

            Environment.CurrentDirectory = gameDirectory.FullName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckPermissions();

            DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            ProgramConstants.LogFileName = clientLogFile.FullName;

            if (clientLogFile.Exists)
            {
                // Copy client.log file as client_previous.log. Override client_previous.log if it exists.
                FileInfo clientPrevLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log");
                if (clientPrevLogFile.Exists)
                    File.Delete(clientPrevLogFile.FullName);
                File.Move(clientLogFile.FullName, clientPrevLogFile.FullName);
            }

            Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
            Logger.WriteLogFile = true;
            MainClientConstants.LoggerInitialized = true;

            if (!clientUserFilesDirectory.Exists)
                clientUserFilesDirectory.Create();

            Logger.Log("***Logfile for " + MainClientConstants.GAME_NAME_LONG + " client***");

            string clientVersion = GitVersionInformation.AssemblySemVer;
#if DEVELOPMENT_BUILD
            clientVersion = $"{GitVersionInformation.CommitDate} {GitVersionInformation.BranchName}@{GitVersionInformation.ShortSha}";
#endif

            Logger.Log($"Client version: {clientVersion}");
            Logger.Log(GitVersionInformation.InformationalVersion);

#if DEVELOPMENT_BUILD
            Logger.Log("This is a development build of the client. Stability and reliability may not be fully guaranteed.");
#endif
            MainClientConstants.Initialize();

            // Log information about given startup params
            if (parameters.NoAudio)
            {
                Logger.Log("Startup parameter: No audio");

                // TODO fix
                throw new NotImplementedException("-NOAUDIO is currently not implemented, please run the client without it.".L10N("Client:Main:NoAudio"));
            }

            if (parameters.MultipleInstanceMode)
                Logger.Log("Startup parameter: Allow multiple client instances");

            parameters.UnknownStartupParams.ForEach(p => Logger.Log("Unknown startup parameter: " + p));

            Logger.Log("Loading settings.");

            UserINISettings.Initialize(ClientConfiguration.Instance.SettingsIniName);

            // Try to load translation
            try
            {
                Translation translation;
                FileInfo translationThemeFile = SafePath.GetFile(UserINISettings.Instance.TranslationThemeFolderPath, ClientConfiguration.Instance.TranslationIniName);
                FileInfo translationFile = SafePath.GetFile(UserINISettings.Instance.TranslationFolderPath, ClientConfiguration.Instance.TranslationIniName);

                if (translationFile.Exists)
                {
                    Logger.Log($"Loading generic translation file at {translationFile.FullName}");
                    translation = new Translation(translationFile.FullName, UserINISettings.Instance.Translation);
                    if (translationThemeFile.Exists)
                    {
                        Logger.Log($"Loading theme-specific translation file at {translationThemeFile.FullName}");
                        translation.AppendValuesFromIniFile(translationThemeFile.FullName);
                    }

                    Translation.Instance = translation;
                }
                else
                {
                    Logger.Log($"Failed to load a translation file. " +
                        $"Neither {translationThemeFile.FullName} nor {translationFile.FullName} exist.");
                }

                Logger.Log("Loaded translation: " + Translation.Instance.Name);
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to load the translation file. " + ex.ToString());
                Translation.Instance = new Translation(UserINISettings.Instance.Translation);
            }

            CultureInfo.CurrentUICulture = Translation.Instance.Culture;

            try
            {
                if (UserINISettings.Instance.GenerateTranslationStub)
                {
                    string stubPath = SafePath.CombineFilePath(
                        ProgramConstants.ClientUserFilesPath, ClientConfiguration.Instance.TranslationIniName);

                    AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
                    {
                        Logger.Log("Writing the translation stub file.");
                        var ini = Translation.Instance.DumpIni(UserINISettings.Instance.GenerateOnlyNewValuesInTranslationStub);
                        ini.WriteIniFile(stubPath);
                    };

                    Logger.Log("Translation stub generation feature is now enabled. The stub file will be written when the client exits.");

                    // Lookup all compile-time available strings
                    ClientCore.Generated.TranslationNotifier.Register();
                    ClientGUI.Generated.TranslationNotifier.Register();
                    ClientUpdater.Generated.TranslationNotifier.Register();
                    DTAClient.Generated.TranslationNotifier.Register();
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Failed to generate the translation stub: " + ex.ToString());
            }

            // Delete obsolete files from old target project versions

            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            gameDirectory.EnumerateFiles("aunchupdt.dat").SingleOrDefault()?.Delete();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                LogException(ex);

                string error = ("Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again." + "\n\n" +
                    "Message:").L10N("Client:Main:DeleteWsock32Failed") + " " + ex.Message;

                MainClientConstants.DisplayErrorAction(null, error, true);
            }

            Startup startup = new();
#if DEBUG
            startup.Execute();
#else
            try
            {
                startup.Execute();
            }
            catch (Exception ex)
            {
                // MainClientConstants.DisplayErrorAction might have been overriden by XNA messagebox, which might be unable to display an error message.
                // Fallback to MessageBox.
                MainClientConstants.DisplayErrorAction = MainClientConstants.DefaultDisplayErrorAction;
                HandleException(startup, ex);
            }
#endif

        }

        public static void LogException(Exception ex, bool innerException = false)
        {
            if (!innerException)
                Logger.Log("KABOOOOOOM!!! Info:");
            else
                Logger.Log("InnerException info:");

            Logger.Log("Type: " + ex.GetType());
            Logger.Log("Message: " + ex.Message);
            Logger.Log("Source: " + ex.Source);
            Logger.Log("TargetSite.Name: " + ex.TargetSite?.Name);
            Logger.Log("Stacktrace: " + ex.StackTrace);

            if (ex.InnerException is not null)
                LogException(ex.InnerException, true);
        }

        public static void HandleException(object sender, Exception ex)
        {
            LogException(ex, innerException: false);

            string errorLogPath = SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs", FormattableString.Invariant($"ClientCrashLog{DateTime.Now.ToString("_yyyy_MM_dd_HH_mm")}.txt"));
            bool crashLogCopied = false;

            try
            {
                DirectoryInfo crashLogsDirectoryInfo = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath, "ClientCrashLogs");

                if (!crashLogsDirectoryInfo.Exists)
                    crashLogsDirectoryInfo.Create();

                File.Copy(SafePath.CombineFilePath(ProgramConstants.ClientUserFilesPath, "client.log"), errorLogPath, true);
                crashLogCopied = true;
            }
            catch { }

            string error = string.Format("{0} has crashed. Error message:".L10N("Client:Main:FatalErrorText1") + Environment.NewLine + Environment.NewLine +
                ex.Message + Environment.NewLine + Environment.NewLine + (crashLogCopied ?
                "A crash log has been saved to the following file:".L10N("Client:Main:FatalErrorText2") + " " + Environment.NewLine + Environment.NewLine +
                errorLogPath + Environment.NewLine + Environment.NewLine : "") +
                (crashLogCopied ? "If the issue is repeatable, contact the {1} staff at {2} and provide the crash log file.".L10N("Client:Main:FatalErrorText3") :
                "If the issue is repeatable, contact the {1} staff at {2}.".L10N("Client:Main:FatalErrorText4")),
                MainClientConstants.GAME_NAME_LONG,
                MainClientConstants.GAME_NAME_SHORT,
                MainClientConstants.SUPPORT_URL_SHORT);

            MainClientConstants.DisplayErrorAction("KABOOOOOOOM".L10N("Client:Main:FatalErrorTitle"), error, true);
        }

        [SupportedOSPlatform("windows")]
        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
                return;

            string error = string.Format(("You seem to be running {0} from a write-protected directory.\n\n" +
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges.\n\n" +
                "Please also make sure that your security software isn't blocking {1}.").L10N("Client:Main:AdminRequiredExplanation"),
                MainClientConstants.GAME_NAME_LONG, MainClientConstants.GAME_NAME_SHORT);

            string question = "Would you like to restart the client with administrative rights?".L10N("Client:Main:AdminRequiredRestartPrompt");

            string title = "Administrative privileges required".L10N("Client:Main:AdminRequiredTitle");

#if WINFORMS && NETFRAMEWORK
            DialogResult result = MessageBox.Show(error + "\n\n" + question, title, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2);
            if (result == DialogResult.Yes)
            {
                AdminRestarter.RestartAsAdmin();
            }
#else
            MainClientConstants.DisplayErrorAction(title, error, true);
#endif
            Environment.Exit(1);
        }

        /// <summary>
        /// Checks whether the client has specific file system rights to a directory.
        /// See ssds's answer at https://stackoverflow.com/questions/1410127/c-sharp-test-if-user-has-write-access-to-a-folder
        /// </summary>
        /// <param name="path">The path to the directory.</param>
        /// <param name="accessRights">The file system rights.</param>
        [SupportedOSPlatform("windows")]
        private static bool UserHasDirectoryAccessRights(string path, FileSystemRights accessRights)
        {
            var currentUser = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(currentUser);

            // If the user is not running the client with administrator privileges in Program Files, they need to be prompted to do so.
            if (!principal.IsInRole(WindowsBuiltInRole.Administrator))
            {
                string progfiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                string progfilesx86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
                if (ProgramConstants.GamePath.Contains(progfiles) || ProgramConstants.GamePath.Contains(progfilesx86))
                    return false;
            }

            var isInRoleWithAccess = false;

            try
            {
                var di = new DirectoryInfo(path);
                var acl = di.GetAccessControl();
                var rules = acl.GetAccessRules(true, true, typeof(NTAccount));

                foreach (AuthorizationRule rule in rules)
                {
                    var fsAccessRule = rule as FileSystemAccessRule;
                    if (fsAccessRule == null)
                        continue;

                    if ((fsAccessRule.FileSystemRights & accessRights) > 0)
                    {
                        var ntAccount = rule.IdentityReference as NTAccount;
                        if (ntAccount == null)
                            continue;

                        try
                        {
                            if (principal.IsInRole(ntAccount.Value))
                            {
                                if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                    return false;
                                isInRoleWithAccess = true;
                            }
                        }
                        catch (SecurityException)
                        {
                            //IsInRole may throw for selected roles when running in Wine, keep iterating other rules 
                            continue;
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
            return isInRoleWithAccess;
        }
    }
}