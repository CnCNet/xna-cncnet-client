using System;
using System.Linq;
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
            "RA" => ClientType.RA,
            _ => throw new Exception(string.Format((
                "It seems the client configuration was not migrated to accommodate for the v2.12 changes. " +
                "Please specify 'ClientGameType' in '[Settings]' section of the 'ClientDefinitions.ini' file " +
                "(allowed options: {0}).\n\n" +
                "Please refer to documentation of the client {1} for more details. This link can also be found in the log file.").L10N("Client:Main:ClientGameTypeNotFoundException"),
                EnumExtensions.GetNames<ClientType>(),
                "https://github.com/CnCNet/xna-cncnet-client/")),
        };
    }
}
