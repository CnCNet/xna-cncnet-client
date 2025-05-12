using System;
using ClientCore.Extensions;

namespace ClientCore.Enums
{
    public static class ClientTypeHelper
    {
        public static ClientType FromString(string value) => value switch
        {
            "TS" => ClientType.TS,
            "YR" => ClientType.YR,
            "Ares" => ClientType.Ares,
            _ => throw new Exception(string.Format(("It seems the client configuration was not migrated to accommodate for the v2.12 changes. " +
                                                    "Please specify 'ClientGameType' in '[Settings]' section of the 'ClientDefinitions.ini' file, " +
                                                    "e.g., 'ClientGameType=Ares'.\n\n" +
                                                    "Please refer to {0} for more details. The full link can be found in the log file.").L10N("Client:Main:ClientGameTypeNotFoundException"),
                                                    "https://github.com/CnCNet/xna-cncnet-client/blob/888d3f025dd689d3e407de8f606bd1769296d6e0/Docs/Migration-INI.md")),
        };
    }
}
