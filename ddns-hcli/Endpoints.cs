using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ddns_hcli {
    internal class Endpoints {
        public const string GET_ALL_RECORDS = $@"https://api.cloudflare.com/client/v4/zones/@ZONE_ID/dns_records";
    }
}
