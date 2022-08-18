using Newtonsoft.Json;

namespace Proxmox.Config{
    public class Config{
        public ulong GUID { get; set; }
        public string DBIP { get; set; }
        public string DBUsername { get; set; }
        public string DBPassword { get; set; }
        public string Token { get; set; }   
        public string DBName { get; set; }

        public Config Parse(){
            string configraw = File.ReadAllText("./config.json");
            return JsonConvert.DeserializeObject<Config>(configraw);
        }
    }

}