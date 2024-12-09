using Rampastring.Tools;

using System;
using System.Collections.Generic;


namespace DTAClient.Domain.Singleplayer
{
    /// <summary>
    /// Represents a relationship between a client variable and related in-game events or configurations
    /// </summary>
    public class CampaignBinding
    {
        public CampaignBinding(string key)
        {
            Key = key;
        }
        public string Key { get; set; }
        public Dictionary<string, string> Binds { get; } = new Dictionary<string, string>();

        public void Bind(string str)
        {
            // Syntax:
            // Variable1:State1,Variable2:State2,Variable3:State3,...

            string[] binds = str.Split(',');
            foreach (var binding in binds)
            {
                string[] components = binding.Split(':');
                if (components.Length != 2)
                {
                    Logger.Log("Parsing CampaignBinding from \"" + str + "\" failed:" + binding);
                    continue;
                }

                Binds.Add(components[0], components[1]);                
            }
        }
    }
}
