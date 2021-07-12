using System;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class SHA1EventArgs : EventArgs
    {
        public SHA1EventArgs(string sha1, string mapName)
        {
            SHA1 = sha1;
            MapName = mapName;
        }

        public string SHA1 { get; private set; }

        public string MapName { get; private set; }
    }
}
