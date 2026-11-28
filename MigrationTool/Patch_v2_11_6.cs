namespace MigrationTool;

internal class Patch_v2_11_6 : Patch
{
    public Patch_v2_11_6(string clientPath) : base(clientPath)
    {
        PatchVersion = ClientVersion.V2_11_6;
    }
}
