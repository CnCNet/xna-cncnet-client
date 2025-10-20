# How to update your mod to latest client version

This guide outlines the steps for updating the XNA CnCNet Client version for any mod or game package that is using it (like, for example, Tiberian Sun Client, CnCNet YR, YR Mod Base or any mod that derives from them etc.).

## Updating the XNA CnCNet Client binaries for the package

1. **Download the latest client binaries release:**
   - Find the latest released client from [XNA CnCNet Client repo releases page](https://github.com/CnCNet/xna-cncnet-client/releases).
   - Download the `[xna-cncnet-client-X.Y.Z.7z]` file, inside of which the updated `Resources` folder should reside.
   - Make note of any migration steps noted in the release.

2. **Clean up old binaries folders:**
   - Go to your local game/mod repo or working folder.
   - Find `Resources/` folder inside of the "game root" folder.
   - Delete `Binaries` and `BinariesNET8` to ensure that no obsolete/renamed library files remain.

3. **Paste files into the package repository:**
   - Go to your local game/mod repo or working folder.
   - Unarchive `Resources` folder from `[xna-cncnet-client-X.Y.Z.7z]` file downloaded earlier inside the "game root" folder.
   - You **must** get a prompt to replace `Resources/` folder and files inside it. If not, you're in the wrong directory.

> [!WARNING]
> If you are using our automatic updater, make sure to check the release notes for any files that need to be added to the `[Delete]` section of `updateexec` or `preupdateexec`. Each release may specify the exact files that must be removed to prevent issues during the update process. For example (from release [2.12.12](https://github.com/CnCNet/xna-cncnet-client/releases/tag/2.12.12)):
> ```ini
> [Delete]
> ; append those lines in the section
> Resources\Binaries\Windows\DTAConfig.dll
> Resources\Binaries\Windows\DTAConfig.pdb
> Resources\Binaries\OpenGL\DTAConfig.dll
> Resources\Binaries\OpenGL\DTAConfig.pdb
> Resources\Binaries\XNA\DTAConfig.dll
> Resources\Binaries\XNA\DTAConfig.pdb
> Resources\BinariesNET8\Windows\DTAConfig.dll
> Resources\BinariesNET8\Windows\DTAConfig.pdb
> Resources\BinariesNET8\OpenGL\DTAConfig.dll
> Resources\BinariesNET8\OpenGL\DTAConfig.pdb
> Resources\BinariesNET8\UniversalGL\DTAConfig.dll
> Resources\BinariesNET8\UniversalGL\DTAConfig.pdb
> Resources\BinariesNET8\XNA\DTAConfig.dll
> Resources\BinariesNET8\XNA\DTAConfig.pdb
> ```

4. **Apply the migration steps:**
   - If updating to next version: follow the instructions from release notes mentioned in step 1.
   - If updating skipping multiple versions, either:
     - look up all release notes skipped and apply migrations;
     - or refer to the [client docs on migration](https://github.com/CnCNet/xna-cncnet-client/blob/develop/Docs/Migration.md).

5. **Run the packaged client to test:**
   - Launch: `YourClientLauncher.exe` on Windows or `YourClientLauncher.sh` on Linux/Mac (the names will vary depending on the mod/game client package).
   - Verify the version hash and client version in the online lobby.
   - Does it work? If no - you missed some migration steps or screwed up somewhere in the steps above, verify your changes or start anew.

After that you can commit/push the changes, if using Git, or publish an update
