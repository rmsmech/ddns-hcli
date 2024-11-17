using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ddns_hcli {
    public class ZoneInfo {
        [JsonPropertyName("zone-id")]
        public string Id { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("records")]
        public string RecordsRaw { get; set; }
        internal string[] RecordsArray { get; set; }
    }
}
