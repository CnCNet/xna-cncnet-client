using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    public class Badge
    {
        public string Ident { get; set; }
        public string Type { get; set; }
    }

    public class CnCNetBadges
    {
        public List<Badge> Badges = new List<Badge>();
        public event Action<object> BadgesReceived;

        public CnCNetBadges()
        {
        }

        public void GetBadges()
        {
            try
            {
                WebClient client = new WebClient();
                Stream data = client.OpenRead("https://ladder.cncnet.org/badges");

                string info = string.Empty;

                using (StreamReader reader = new StreamReader(data))
                {
                    info = reader.ReadToEnd();
                }

                Badges = JsonConvert.DeserializeObject<List<Badge>>(info);
                BadgesReceived?.Invoke(this);
            }
            catch
            {
            }
        }
    }
}
