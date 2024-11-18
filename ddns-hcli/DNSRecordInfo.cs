using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace ddns_hcli {
    public class DNSRecordInfo {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "A"; //Default record type.dhanu
        [JsonPropertyName("name")]
        public string Name { get; set; }
        [JsonPropertyName("proxied")] 
        public bool Proxied { get; set; } = false;
        [JsonPropertyName("ttl")]
        public int TTL { get; set; } = 1; //1 is for auto
        [JsonPropertyName("content")]
        public string Content { get; set; }
    }
}
