using Discord;
using Newtonsoft.Json;
using System.IO;
using System.Collections.Generic;

namespace GestaltBot.Types
{
    public class Configurations
    {

        public bool MentionPrefix { get; set; }
        public char Prefix { get; set; }
        public ulong[] Owners { get; set; }
        public string Token { get; set; }
        public List<Channel> NsfwChannels { get; set; }

        public Configurations()
        {
            Prefix = '!';
            MentionPrefix = false;
            Owners = new ulong[0];
            Token = "";
            NsfwChannels = new List<Channel>();
        }

        public void SaveFile(string loc)
        {

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            if (!File.Exists(loc))
                File.Create(loc).Close();

            File.WriteAllText(loc, json);

        }
        public static Configurations LoadFile(string loc)
        {

            string json = File.ReadAllText(loc);
            return JsonConvert.DeserializeObject<Configurations>(json);
        }

    }
}
