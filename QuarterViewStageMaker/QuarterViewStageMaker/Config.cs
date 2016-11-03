using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace QuarterViewStageMaker
{
    [JsonObject()]
    public class Config
    {
        [JsonIgnore()]
        public const string ConfigFileName = "Config.json";

        [JsonProperty("ProjectFolder")]
        public string ProjectFolder { get; set; }

        public Config()
        {
            ProjectFolder = "NewProject";
        }

        public static void Save(Config config)
        {
            var json = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFileName, json);
        }

        public static Config Load()
        {
            var json = "";
            if (File.Exists(ConfigFileName))
                json = File.ReadAllText(ConfigFileName);
            if (!string.IsNullOrWhiteSpace(json))
                return JsonConvert.DeserializeObject<Config>(json);
            else
                return new Config();
        }
    }
}
