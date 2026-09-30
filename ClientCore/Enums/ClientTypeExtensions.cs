#nullable enable
using System;

using ClientCore.Extensions;

namespace ClientCore.Enums;

public static class ClientTypeExtensions
{
    private static readonly string unknownClientTypeErrorMsg = string.Format((
            "It seems the client configuration was not migrated to accommodate for the v2.12 changes. " +
            "Please specify 'ClientGameType' in '[Settings]' section of the 'ClientDefinitions.ini' file " +
            "(allowed options: {0}).\n\n" +
            "Please refer to documentation of the client {1} for more details. This link can also be found in the log file.").L10N("Client:Main:ClientGameTypeNotFoundException"),
            EnumExtensions.GetNames<ClientType>(),
            "https://github.com/CnCNet/xna-cncnet-client/");

    extension(ClientType)
    {
        public static ClientType FromString(string value) => value switch
        {
            "TD" => ClientType.TD,
            "RA" => ClientType.RA,
            "TS" => ClientType.TS,
            "YR" => ClientType.YR,
            "Ares" => ClientType.Ares,
            _ => throw new Exception(unknownClientTypeErrorMsg),
        };
    }

    extension(ClientType clientType)
    {
        public uint? ToSteamAppId() => clientType switch
        {
            ClientType.TD => 2229830,
            ClientType.RA => 2229840,
            ClientType.TS => 2229880,
            ClientType.YR or ClientType.Ares => 2229850,
            _ => null,
        };
    }
}

