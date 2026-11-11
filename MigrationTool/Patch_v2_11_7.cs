#nullable enable
namespace MigrationTool;

internal class Patch_v2_11_7 : Patch
{
    public Patch_v2_11_7(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_11_7;
    }
}
