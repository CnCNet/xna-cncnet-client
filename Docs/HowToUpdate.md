# How to update your mod to latest client version

This guide outlines the steps for updating the XNA CnCNet Client version for any mod or game package that is using it (like, for example, Tiberian Sun Client, CnCNet YR, YR Mod Base or any mod that derives from them etc.).

> [!WARNING]
> It is also strongly recommended to keep the client launcher (the EXE file that resides in the mod folder and launches the actual client) up to date. To update - download the latest release from [it's repository](https://github.com/CnCNet/xna-cncnet-client-launcher/), open the EXE file with [Resource Hacker](https://www.angusj.com/resourcehacker/), change the icons, save and replace the EXE you currently have (for example, `TiberianSun.exe` or `CnCNetYRLauncher.exe`). Don't forget also to copy the rest of the files from the archive to the game folder!

## Updating the XNA CnCNet Client binaries for the package

1. **Download the latest client binaries release:**
   - Find the latest released client from [XNA CnCNet Client repo releases page](https://github.com/CnCNet/xna-cncnet-client/releases).
   - Download the `[xna-cncnet-client-X.Y.Z.7z]` file, inside of which the updated `Resources` folder should reside.
   - Make note of any migration steps noted in the release.

2. **Clean up old binaries folders:**
   - Go to your local game/mod repo or working folder.
   - Find `Resources/` folder inside of the "game root" folder.
   - **IMPORTANT:** Delete `Binaries` and `BinariesNET8` to ensure that no obsolete/renamed library files remain.

3. **Paste files into the package repository:**
   - Go to your local game/mod repo or working folder.
   - Unarchive `Resources` folder from `[xna-cncnet-client-X.Y.Z.7z]` file downloaded earlier inside the "game root" folder.
   - You **must** get a prompt to replace `Resources/` folder and files inside it. If not, you're in the wrong directory.

4. **Apply the migration steps:**
   - If updating to next version: follow the instructions from release notes mentioned in step 1.
   - If updating skipping multiple versions, either:
     - look up all release notes skipped and apply migrations;
     - or refer to the [client docs on migration](Migration.md).

5. **Run the packaged client to test:**
   - Launch: `YourClientLauncher.exe` on Windows or `YourClientLauncher.sh` on Linux/Mac (the names will vary depending on the mod/game client package).
   - Verify the version hash and client version in the online lobby.
   - Does it work? If no - you missed some migration steps or screwed up somewhere in the steps above, verify your changes or start anew.

After that you can commit/push the changes, if using Git, or publish an update
