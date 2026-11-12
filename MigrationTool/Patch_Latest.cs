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

        // Add the latest migration steps to Patch_Latest.Apply.
    }
}

