using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_vLatest : Patch
{
    public Patch_vLatest(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.Latest;
    }

    public override void Apply()
    {
        base.Apply();

        // Write latest patch there
    }
}

