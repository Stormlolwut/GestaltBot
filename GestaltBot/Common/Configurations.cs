using Newtonsoft.Json;
using System.IO;

namespace GestaltBot.Types {
    public class Configurations {


        public char Prefix { get; set; }
        public ulong[] Owners { get; set; }
        public string Token { get; set; }

        public Configurations() {
            Prefix = '!';
            Owners = new ulong[0];
            Token = "MjQxMTk2MzAxNjc1Mzk3MTIx.Cvk2NA.ra-tWKeGDhCgr9YYX9SnXrMflBM";
        }

        public void SaveFile(string loc) {

            string json = JsonConvert.SerializeObject(this, Formatting.Indented);

            if (!File.Exists(loc))
                File.Create(loc).Close();

            File.WriteAllText(loc, json);

        }
        public static Configurations LoadFile(string loc) {

            string json = File.ReadAllText(loc);
            return JsonConvert.DeserializeObject<Configurations>(json);
        }

    }
}
