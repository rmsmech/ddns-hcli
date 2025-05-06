using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace hdns {
    public class IpFinder {
        [JsonPropertyName("url")]
        public string URL { get; set; }
        public DateTime LastUsed { get; set; }
        [JsonPropertyName("coolingtime")]
        public int CoolingTime { get; set; }
        public int ErrorCount { get; set; }
    }
}
