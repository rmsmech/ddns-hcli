using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace hdns {
    public class ZoneInfo {
        [JsonPropertyName("zone-id")]
        public string Id { get; set; }
        [JsonPropertyName("token")]
        public string Token { get; set; }
        [JsonPropertyName("records")]
        public string RecordsRaw { get; set; }
        internal string[] RecordsArray { get; set; }
        internal void ParseRecords() {
            if (string.IsNullOrWhiteSpace(RecordsRaw)) return;
            RecordsArray = RecordsRaw.Split(new char[] { ',' }); //Comma separated.
        }
        internal bool IsInvalid() {
            return string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Token) || string.IsNullOrWhiteSpace(RecordsRaw);
        }
    }
}
