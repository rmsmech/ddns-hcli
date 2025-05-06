using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace hdns {
    internal class Endpoints {
        public const string GET_ALL_RECORDS = $@"https://api.cloudflare.com/client/v4/zones/@ZONE_ID/dns_records";
        public const string UPDATE_RECORD = $@"https://api.cloudflare.com/client/v4/zones/@ZONE_ID/dns_records/@RECORD_ID";
    }
}
