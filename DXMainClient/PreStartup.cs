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
            Application.ThreadException += (_, args) => ProgramConstants.HandleException(args.Exception);
#endif
            AppDomain.CurrentDomain.UnhandledException += (_, args) => ProgramConstants.HandleException((Exception)args.ExceptionObject);

            DirectoryInfo gameDirectory = SafePath.GetDirectory(ProgramConstants.GamePath);

            Environment.CurrentDirectory = gameDirectory.FullName;

            DirectoryInfo clientUserFilesDirectory = SafePath.GetDirectory(ProgramConstants.ClientUserFilesPath);
            FileInfo clientLogFile = SafePath.GetFile(clientUserFilesDirectory.FullName, "client.log");
            ProgramConstants.LogFileName = clientLogFile.FullName;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                CheckPermissions();

            if (clientLogFile.Exists)
                File.Move(clientLogFile.FullName, SafePath.GetFile(clientUserFilesDirectory.FullName, "client_previous.log").FullName, true);

            Logger.Initialize(clientUserFilesDirectory.FullName, clientLogFile.Name);
            Logger.WriteLogFile = true;

            if (!clientUserFilesDirectory.Exists)
                clientUserFilesDirectory.Create();

            ProgramConstants.OSId = ClientConfiguration.Instance.GetOperatingSystemVersion();
            ProgramConstants.GAME_NAME_SHORT = ClientConfiguration.Instance.LocalGame;
            ProgramConstants.GAME_NAME_LONG = ClientConfiguration.Instance.LongGameName;
            ProgramConstants.SUPPORT_URL_SHORT = ClientConfiguration.Instance.ShortSupportURL;
            ProgramConstants.CREDITS_URL = ClientConfiguration.Instance.CreditsURL;
            ProgramConstants.MAP_CELL_SIZE_X = ClientConfiguration.Instance.MapCellSizeX;
            ProgramConstants.MAP_CELL_SIZE_Y = ClientConfiguration.Instance.MapCellSizeY;

            if (string.IsNullOrEmpty(ProgramConstants.GAME_NAME_SHORT))
                throw new ClientConfigurationException("LocalGame is set to an empty value.");

            if (ProgramConstants.GAME_NAME_SHORT.Length > ProgramConstants.GAME_ID_MAX_LENGTH)
            {
                throw new ClientConfigurationException("LocalGame is set to a value that exceeds length limit of " +
                    ProgramConstants.GAME_ID_MAX_LENGTH + " characters.");
            }

            Logger.Log("***Logfile for " + ProgramConstants.GAME_NAME_LONG + " client***");
            Logger.Log("Client version: " + Assembly.GetAssembly(typeof(PreStartup)).GetName().Version);

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
                ProgramConstants.LogException(ex, "Failed to load the translation file.");
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
                    DTAConfig.Generated.TranslationNotifier.Register();
                    DTAClient.Generated.TranslationNotifier.Register();
                }
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex, "Failed to generate the translation stub.");
            }

            // Delete obsolete files from old target project versions

            gameDirectory.EnumerateFiles("mainclient.log").SingleOrDefault()?.Delete();
            gameDirectory.EnumerateFiles("launchupdt.dat").SingleOrDefault()?.Delete();

            try
            {
                gameDirectory.EnumerateFiles("wsock32.dll").SingleOrDefault()?.Delete();
            }
            catch (Exception ex)
            {
                ProgramConstants.LogException(ex);

                string error = "Deleting wsock32.dll failed! Please close any " +
                    "applications that could be using the file, and then start the client again."
                    + Environment.NewLine + Environment.NewLine +
                    "Message: " + ex.Message;

                ProgramConstants.DisplayErrorAction(null, error, true);
            }

#if WINFORMS
            ApplicationConfiguration.Initialize();
#endif

            new Startup().Execute();
        }

        [SupportedOSPlatform("windows")]
        private static void CheckPermissions()
        {
            if (UserHasDirectoryAccessRights(ProgramConstants.GamePath, FileSystemRights.Modify))
                return;

            string error = string.Format(("You seem to be running {0} from a write-protected directory.\n\n" + 
                "For {1} to function properly when run from a write-protected directory, it needs administrative priveleges.\n\n" +
                "Would you like to restart the client with administrative rights?\n\n" +
                "Please also make sure that your security software isn't blocking {1}.").L10N("Client:Main:AdminRequiredText"), ProgramConstants.GAME_NAME_LONG, ProgramConstants.GAME_NAME_SHORT);

            ProgramConstants.DisplayErrorAction("Administrative privileges required".L10N("Client:Main:AdminRequiredTitle"), error, false);

            using var _ = Process.Start(new ProcessStartInfo
            {
                FileName = "dotnet",
                Arguments = SafePath.CombineFilePath(ProgramConstants.StartupExecutable),
                Verb = "runas",
                CreateNoWindow = true
            });
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

                        if (principal.IsInRole(ntAccount.Value))
                        {
                            if (fsAccessRule.AccessControlType == AccessControlType.Deny)
                                return false;
                            isInRoleWithAccess = true;
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