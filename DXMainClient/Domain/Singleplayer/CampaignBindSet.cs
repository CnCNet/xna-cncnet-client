using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

using Rampastring.Tools;
using ClientCore;

namespace DTAClient.Domain.Singleplayer
{
    public class CampaignBindSet
    {
        public string Prefix;
        public Dictionary<string, string> Bindings;

        public CampaignBindSet(string prefix)
        {
            Prefix = prefix;
            Bindings = new Dictionary<string, string>();
        }

        public void InitFromIniSection(IniSection section)
        {
            foreach (KeyValuePair<string, string> kvp in section.Keys.Where(k => k.Key.StartsWith(Prefix)))
            {
                string[] parts = kvp.Key.Split('.');
                if (parts.Length > 2)
                    Logger.Log("Campaign binding key containing more than one /'./' will be skipped: " + kvp.Key);
                else
                    Bindings.Add(parts[1],kvp.Value);
            }
        }
    }
}
