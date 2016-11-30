using Discord;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;
using System;

namespace GestaltBot.Types
{
    public class Configurations
    {
        const string m_accesconfig = "configurations.json";

        public bool MentionPrefix { get; set; }
        public char Prefix { get; set; }
        public ulong[] Owners { get; set; }
        public string Token { get; set; }
        public List<string> NsfwChannels { get; set; }

        public static Configurations m_config;
        public static Configurations config { get { return m_config; } }

        public Configurations()
        {
            Prefix = '!';
            MentionPrefix = true;
            Owners = new ulong[0];
            Token = "";
            NsfwChannels = new List<string>();
        }

        public void SaveFile()
        {

            string json = JsonConvert.SerializeObject(m_config, Formatting.Indented);

            if (!File.Exists(m_accesconfig))
                File.Create(m_accesconfig).Close();

            File.WriteAllText(m_accesconfig, json);

        }

        public static void LoadFile()
        {
            try
            {
                string json = File.ReadAllText(m_accesconfig);
                m_config = JsonConvert.DeserializeObject<Configurations>(json);
            }

            catch
            {
                m_config = new Configurations();

                Console.WriteLine("The example bot's configuration file has been created. Please enter a valid token.");
                Console.Write("Token: ");

                m_config.Token = Console.ReadLine();
                m_config.SaveFile();
            }
        }
    }
}
