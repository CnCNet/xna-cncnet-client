#nullable enable
namespace MigrationTool;

internal class Patch_v2_11_6 : Patch
{
    public Patch_v2_11_6(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.v2_11_6;
    }

    public override void Apply()
    {
        base.Apply();

        CopyEmbeddedResourceWithLog("skillLevel1.png");
        CopyEmbeddedResourceWithLog("skillLevel2.png");
        CopyEmbeddedResourceWithLog("skillLevel3.png");
    }
}
