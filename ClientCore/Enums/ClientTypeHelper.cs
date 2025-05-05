using System;

namespace ClientCore.Enums
{
    public static class ClientTypeHelper
    {
        public static ClientType FromString(string value) => value switch
        {
            "TS" => ClientType.TS,
            "YR" => ClientType.YR,
            "Ares" => ClientType.Ares,
            _ => throw new Exception("It seems the client configuration was not migrated to accommodate for the v2.12 changes. Please specify 'ClientGameType' in `[Settings]` section of the 'ClientDefinitions.ini' file, e.g., 'ClientGameType=Ares'."),
        };
    }
}
