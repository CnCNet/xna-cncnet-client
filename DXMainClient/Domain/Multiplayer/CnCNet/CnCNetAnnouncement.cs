using Rampastring.Tools;
using Rampastring.XNAUI;
using System;
using System.IO;
using System.Net;
using Microsoft.Xna.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace DTAClient.Domain.Multiplayer.CnCNet
{
    /// <summary>
    /// Announcement controlled by a remote url for the CnCNet Lobby
    /// </summary>
    public class CnCNetAnnouncement
    {
        private Announcement announcement;

        public CnCNetAnnouncement()
        {
            Message = "";
            Color = AssetLoader.GetColorFromString(ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementColor);

            announcement = new Announcement()
            {
                Message = Message
            };

            fetchAnnouncement();
        }

        private void fetchAnnouncement()
        {
            try
            {
                WebClient client = new WebClient();

                Stream response = client.OpenRead(ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementURL);
                using (StreamReader reader = new StreamReader(response))
                {
                    string json = reader.ReadToEnd();
                    announcement = JsonConvert.DeserializeObject<Announcement>(json);
                }
            }
            catch (Exception ex)
            {
                Logger.Log("Error fetching Announcement text from remote url: "
                    + ClientCore.ClientConfiguration.Instance.CnCNetAnnouncementURL
                    + " Error: " + ex.Message);
            }

            if (announcement.Color != null)
            {
                Color = AssetLoader.GetColorFromString(announcement.Color);
            }

            if (announcement.Message != null)
            {
                Message = announcement.Message;
            }
        }

        public string Message { get; private set; }
        public Color Color { get; private set; }
    }
}
