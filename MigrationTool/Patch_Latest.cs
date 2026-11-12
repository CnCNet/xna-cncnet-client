using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_Latest : Patch
{
    public Patch_Latest(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.Latest;
    }

    public override void Apply()
    {
        base.Apply();

        // Write migration steps for the next release in Patch_Latest.Apply, especially
        // when the next version number is not decided yet. Patch_Latest.Apply must be
        // empty before an official release.
    }
}

