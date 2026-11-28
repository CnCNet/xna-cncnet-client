namespace MigrationTool;

internal class Patch_v2_11_7 : Patch
{
    public Patch_v2_11_7(string clientPath) : base(clientPath)
    {
    }

    public override ClientVersion PatchVersion => ClientVersion.V2_11_7;
}
