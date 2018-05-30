using Rampastring.Tools;
using System;
using System.IO;
using System.Net;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Announcement controlled by a remote url for the CnCNet Lobby
    /// </summary>
    public class CnCNetAnnouncement
    {
        public CnCNetAnnouncement()
        {
            Color = ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementColorRGB.Split(',');
        }

        public string GetAnnouncementMessage()
        {
            string message = "";

            try
            {
                WebClient client = new WebClient();

                Stream response = client.OpenRead(ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementURL);
                using (StreamReader reader = new StreamReader(response))
                {
                    message = reader.ReadToEnd();
                }
                return message;
            }
            catch(Exception ex)
            {
                Logger.Log("Error fetching Announcement text from remote url: " 
                    + ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementURL 
                    + " Error: " + ex.Message);
            }

            return message;
        }

        public string[] Color { get; private set; }
    }
}
