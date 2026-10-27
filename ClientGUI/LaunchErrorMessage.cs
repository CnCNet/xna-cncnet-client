#nullable enable

using ClientCore.Extensions;
using ClientCore.Launch;

using Rampastring.XNAUI;

namespace ClientGUI;

/// <summary>Tells the player why the game didn't start.</summary>
public static class LaunchErrorMessage
{
    public static void Show(WindowManager windowManager, LaunchResult result)
    {
        switch (result.FailureReason)
        {
            case LaunchFailureReason.PreprocessingIncomplete:
                XNAMessageBox.Show(windowManager,
                    "INI preprocessing not complete".L10N("Client:ClientGUI:INIPreprocessingNotCompleteTitle"),
                    ("INI preprocessing not complete. Please try " +
                    "launching the game again. If the problem persists, " +
                    "contact the game or mod authors for support.").L10N("Client:ClientGUI:INIPreprocessingNotCompleteText"));
                break;
            case LaunchFailureReason.ProcessStartFailed:
                string errorLaunchingText = ("Error launching {0}. Please check that your anti-virus isn't blocking the CnCNet Client. " +
                    "You can also try running the client as an administrator.\n\nYou are unable to participate in this match. \n\n" +
                    "Returned error: {1}").L10N("Client:ClientGUI:ErrorLaunchingText");

                XNAMessageBox.Show(windowManager,
                    "Error launching game".L10N("Client:ClientGUI:ErrorLaunchingTitle"),
                    string.Format(errorLaunchingText, result.ExecutableName, result.ErrorMessage));
                break;
        }
    }
}
