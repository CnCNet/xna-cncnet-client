using System;
using ClientCore.Enums;

namespace ClientCore.Extensions
{
    public static class ClientTypeExtensions
    {
        private static readonly string errorMsg = string.Format((
                "It seems the client configuration was not migrated to accommodate for the v2.12 changes. " +
                "Please specify 'ClientGameType' in '[Settings]' section of the 'ClientDefinitions.ini' file " +
                "(allowed options: {0}).\n\n" +
                "Please refer to documentation of the client {1} for more details. This link can also be found in the log file.").L10N("Client:Main:ClientGameTypeNotFoundException"),
                EnumExtensions.GetNames<ClientType>(),
                "https://github.com/CnCNet/xna-cncnet-client/");

        public static ClientType FromString(string value) => value switch
        {
            "TD" => ClientType.TD,
            "RA" => ClientType.RA,
            "TS" => ClientType.TS,
            "YR" => ClientType.YR,
            "Ares" => ClientType.Ares,
            _ => throw new Exception(errorMsg),
        };

        public static uint ToSteamAppId(this ClientType ct) => ct switch
        {
            ClientType.TD => 2229830,
            ClientType.RA => 2229840,
            ClientType.TS => 2229880,
            ClientType.YR 
            or ClientType.Ares => 2229850,
            _ => throw new Exception(errorMsg),
        };
    }
}
