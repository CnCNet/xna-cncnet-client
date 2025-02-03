# README for Build Scripts

Before running any scripts in this folder, please close Visual Studio.

## Build the client

Click one of the following script file: `BuildAres.bat`, `BuildTS.bat`, and `BuildYR.bat`.

## Update the common assembly list

You should do this if you have introduced any new NuGet dependencies.

1. Launch Powershell (`pwsh`, not `PowerShell`) and switch to this folder. 

2. `.\build.ps1 -Games Ares -NoMove`

3. `.\Get-CommonAssemblyList.ps1 -Net8 > ..\CommonAssemblies.txt`

4. `.\Get-CommonAssemblyList.ps1 > ..\CommonAssembliesNetFx.txt`

5. Carefully check the changes with Git diff:
- If you have introduce new NuGet dependencies, check if they have appeared in the list.
    - If they do show in the list, it's expected.
    - If they do not show there, **do not** manually add them to the list. Think carefully about whether these libraries should differ among DX/GL/XNA builds.
- If there are other libraries get **removed** from this list, don't just commit the changes. Does this library exist in the `Compiled` folder?
    - If so, we can **resume** this line instead of removing it. 
    - If not, think carefully if we should keep this item, depending on whether these libraries should differ among DX/GL/XNA builds.
        - Specifially, we intend to leave `ClientUpdater.dll` and `ClientUpdater.pdb` files in that list since we *know* this library does not differ among DX/GL/XNA builds, regardless the fact that these two files are different among DX/GL/XNA builds.
- If there are other libraries just get **added** in this list, check if such a library has already been shown up in **previous** releases of the client.
    - If so, we should **delete** such a line, because a library showing in this list has a lower priority than the library that is not included in this list.
    - If not, we can keep the changes. This means a commit after the latest release brought another dependency and **forgetting** to update the common assembly list. It's lucky we catch it up before making a new release.

6. Delete the `Compiled` folder since it is produced with `-NoMove` parameter. We should absolutely **not** distribute these files.