using System.Linq;

using Rampastring.Tools;

namespace MigrationTool;

internal class Patch_Latest : Patch
{
    public Patch_Latest(string clientPath) : base(clientPath)
    {
        ClientVersion = Version.Latest;
    }

    public override Patch Apply()
    {
        base.Apply();

        // Write latest patch there

        return this;
    }
}

