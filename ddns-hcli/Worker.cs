using Haley.Utils;
using System.Reflection;
using System.Text.Json;
using Haley.Rest;
using Haley.Models;
using System.Net;
using Haley.Abstractions;
using System.Text.Json.Nodes;
using Haley.Log;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace ddns_hcli {
    public class Worker : BackgroundService {
        private readonly ILogger _logger;
        const string _cfgFileName = "ddns.conf";
        const string _ipfFileName = "ipfinder.conf";
        string _cfgDirectory = string.Empty;
        string _cfgFilePath = string.Empty;
        string _ipfFilePath = string.Empty;
        List<ZoneInfo> _cfgList = new List<ZoneInfo>();
        List<IpFinder> _ipfList = new List<IpFinder>();
        int _ipfIndex = 0;
        int _ipfCount = 0;
        int _sleepTime = 1000; //1 second.
        FluentClient _client = new FluentClient();
        int continuousErrorCount = 0;

        Regex _ipv4Regex = new Regex(@"^\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}$");

        public Worker(ILogger<Worker> logger, ILoggerProvider provider, IConfigurationRoot cfgRoot) {
            //Change logger to Haley logger, so that we can start dumping the logs to a file.
            if (provider != null && provider is FileLogProvider) {
                _logger = provider.CreateLogger("DDNS Worker");
            } else {
                _logger = logger;
            }

            Initialize(cfgRoot);
        }

        void Initialize(IConfigurationRoot cfgRoot) {
            try {
                _logger?.LogInformation("Initializing..");
                //_sleepTime = (config.GetValue<int>("WorkerSleepTime")) * 1000;
                //_cfgDirectory = config.GetValue<string>("CfgDirectory");

                _sleepTime = (cfgRoot.GetSection("WorkerSleepTime").Get<int>()) * 1000;
                _cfgDirectory = cfgRoot.GetSection("CfgDirectory")?.Get<string>();

                if (string.IsNullOrWhiteSpace(_cfgDirectory)) {
                    _cfgDirectory = Path.GetDirectoryName(AssemblyUtils.GetBaseDirectory());

                    if (string.IsNullOrWhiteSpace(_cfgDirectory)) _cfgDirectory = AssemblyUtils.GetBaseDirectory();
                }

                if (!Directory.Exists(_cfgDirectory)) Directory.CreateDirectory(_cfgDirectory);
                //Should we even expect the config also to be present in same folder as the main application? What if the user deletes some main dll by mistake???
                //TODO: Change the location of the conf file to some other location.
                _cfgFilePath = Path.Combine(_cfgDirectory, _cfgFileName); //to optimize later
                _ipfFilePath = Path.Combine(_cfgDirectory, _ipfFileName);
                _logger?.LogInformation("Configration Search path " + _cfgFilePath);
                if (!File.Exists(_cfgFilePath)) {
                    //Then try to export the file from embedded data.
                    _logger?.LogInformation("Configuration doesn't exists. Exporting the default file.");
                    //var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    var res = ResourceUtils.GetEmbeddedResource($@"ddns_hcli.{_cfgFileName}", Assembly.GetExecutingAssembly());
                    using (var fs = new FileStream(_cfgFilePath, FileMode.Create)) {
                        fs.Write(res);
                        fs.Close();
                    }
                }

                if (!File.Exists(_ipfFilePath)) {
                    //Then try to export the file from embedded data.
                    _logger?.LogInformation("Configuration doesn't exists. Exporting the default file.");
                    //var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    var res = ResourceUtils.GetEmbeddedResource($@"ddns_hcli.{_ipfFileName}", Assembly.GetExecutingAssembly());
                    using (var fs = new FileStream(_ipfFilePath, FileMode.Create)) {
                        fs.Write(res);
                        fs.Close();
                    }
                }

                if (!File.Exists(_cfgFilePath)) throw new ArgumentException($@"Configuration file {_cfgFileName} doesn't exists. Please add conf file.");
                //var txt = File.ReadAllText(_cfgFilePath);
                //_cfg = JsonNode.Parse(txt).
                _cfgList = JsonSerializer.Deserialize<ZoneInfo[]>(File.ReadAllText(_cfgFilePath))?.ToList();
                _ipfList = JsonSerializer.Deserialize<IpFinder[]>(File.ReadAllText(_ipfFilePath))?.ToList();
                _ipfCount = _ipfList?.Count ?? 0; //total list of IPF
                _cfgList.ForEach(p => p.ParseRecords()); //to fetch the records as an array.
                _logger?.LogInformation("Initialization Complete");

            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
                throw; //If it is not initialized, do not even proceed.
            }
        }

        async Task<(bool status,string ipaddr)> GetIpAddress() {
            //First try to fetch the ip address from external source.
            int currentIndex = _ipfIndex; //Let us start from ipfIndex
            int searchCount = 0;
            _logger?.LogInformation("Selecting an Ip Finder");
            //01 - Verify cooling period
            while (true) {
                //Clear cache.
                //Check if we have completed one whole rotation
                if (searchCount >= _ipfCount) {
                    _logger?.LogInformation("No IPF has cooled down. Waiting for 30 seconds before checking again.");
                    await Task.Delay(30000); // Wait for 30 seconds before checking cooling period. 
                    //Reset
                    searchCount = 0; 
                    currentIndex = _ipfIndex;
                }

                //Adjust the current Index within the range
                if (currentIndex > (_ipfCount - 1) || currentIndex < 0) currentIndex = 0;
                var currentIpf = _ipfList[currentIndex];
                if ((DateTime.UtcNow - currentIpf.LastUsed).TotalSeconds > currentIpf.CoolingTime) {
                    _ipfIndex = currentIndex; //IPF Index updated.
                    break;
                }
                //
                currentIndex++;
                searchCount++;
            }

            var ipf = _ipfList[_ipfIndex];
            _logger?.LogInformation($@"IP Finder finalized : {ipf.URL}");
            string ipaddr = string.Empty; //LOCAL VARIABLE
            try {
                _logger?.LogInformation("IP Fetching initialized.");
                var strResponse = await (await _client.WithEndPoint(ipf.URL).GetAsync()).AsStringResponseAsync();
                ipf.LastUsed = DateTime.UtcNow; //Whether it fails or not, once a finder is used, we wait for cooling time.
                if (strResponse.IsSuccessStatusCode) {
                    ipaddr = strResponse.ToString().Trim();
                    //If the received ip address is not in the format of a ipv4, then we make it empty.
                    if (!_ipv4Regex.IsMatch(ipaddr)) ipaddr = string.Empty;
                } else {
                    _logger?.LogError($@"IP request failed. {strResponse.ToString().Trim()}");
                }

                //We now check if the ip addrress is empty or not.
                if (string.IsNullOrWhiteSpace(ipaddr)) {
                    _ipfIndex++; //Move to next index and start again.
                    return await GetIpAddress();
                }

            } catch (Exception ex) {
                _logger?.LogError($@"IP Finder {ipf.URL} has ended with error {ex.Message}");
                _ipfIndex++; //Move to next ip finder incase previous had error.
                ipf.ErrorCount++; //So we can track which IP Finder has continous error and report that to be removed.
                continuousErrorCount++; //For some reason, if we have continous error for sometime, we should not proceed with application at all. .may be internet connection is down..
                if (continuousErrorCount > 10) {
                    _logger?.LogInformation("Continuous error for more than 10 times. Taking a break of 3 mintues");
                    await Task.Delay(180000); // Wait for 3 minutes, may be internet is down.
                    continuousErrorCount = 0; //Reset the error count.
                }
                return (false, string.Empty);
            }
            _ipfIndex++; //Move to next ip finder.
            _logger?.LogInformation($@"Ip address found : {ipaddr} using {ipf.URL.Trim()}");
            return (true, ipaddr);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                try {
                    _logger?.LogInformation("Worker Started at: {time}", DateTimeOffset.Now);

                    var ipTup = await GetIpAddress();
                    if (!ipTup.status) continue; //Get the ip address
                                                 //var newip = "46.66.80.12";
                    var newip = ipTup.ipaddr;
                    if (!_ipv4Regex.IsMatch(newip)) {
                        _logger?.LogInformation("Ip address is not in proper ipv4 format");
                        continue;
                    }
                    foreach (var zone in _cfgList) {
                        //Take each zone and then fetch it's existing records.
                        var ep = Endpoints.GET_ALL_RECORDS.Replace("@ZONE_ID", zone.Id);
                        _logger?.LogInformation($@"Initiating process for Zone : {zone.Id}");
                        var allrecords = (await (await _client
                            .WithEndPoint(ep)
                            .DoNotAuthenticate()
                            .AddHeader("Authorization", $@"Bearer {zone.Token}") //Add header to the request and not to the client
                            .GetAsync())
                            .AsStringResponseAsync()).ToString().Trim();
                        var allrecordsJSON = JsonNode.Parse(allrecords);
                        if (!(allrecordsJSON?["success"]?.GetValue<bool>() ?? false)) {
                            _logger?.LogInformation($@"Unable to fetch Cloudflare Zone information");
                            continue;
                        }
                        if (!(allrecordsJSON!["result"] is JsonArray recArr)) {
                            _logger?.LogInformation($@"Cloudflare Zone returned zero results");
                            continue;
                        }
                        var targetRecords = recArr
                            .Where(p => (p["type"] != null &&
                        p["type"]!.GetValue<string>().Equals("a", StringComparison.InvariantCultureIgnoreCase))
                        && zone.RecordsArray.Contains(p["name"]?.GetValue<string>()))?
                        .Select(q => new {
                            id = q["id"]?.GetValue<string>(),
                            content = q["content"]?.GetValue<string>(),
                            name = q["name"]?.GetValue<string>(),
                            proxied = q["proxied"]?.GetValue<bool>() ?? true,
                            ttl = q["ttl"]?.GetValue<int>() ?? 1
                        })
                        .ToList();

                        if (targetRecords == null || targetRecords.Count < 1) {
                            _logger?.LogInformation($@"No matching DNS -A- records found for the given filters.");
                            continue; //To next configuration.
                        }

                        await Parallel.ForEachAsync(targetRecords, async (rec, token) => {
                            do {
                                if (rec == null) break;
                                if (rec!.content.Equals(newip, StringComparison.OrdinalIgnoreCase)) {
                                    _logger?.LogWarning($"NO UPDATE: For the DNS record {rec.name} (proxied:{rec.proxied}), the ip address {rec.content} has not changed.");
                                    break;
                                }
                                var dnsrec = new DNSRecordInfo() { Name = rec.name, TTL = rec.ttl, Proxied = rec.proxied, Content = newip };
                                //For each item, update the ip address
                                var patchEP = Endpoints.UPDATE_RECORD.Replace("@ZONE_ID", zone.Id).Replace("@RECORD_ID", rec.id);
                                var patchResp = await _client
                                .WithEndPoint(patchEP)
                                .WithBody(dnsrec.ToJson(), true, Haley.Enums.BodyContentType.StringContent)
                                .DoNotAuthenticate()
                                .AddHeader("Authorization", $@"Bearer {zone.Token}") //Add header to the request and not to the client
                                .PutAsync();
                                var pathRespResult = await patchResp.AsStringResponseAsync();
                                _logger?.LogInformation($@"The record {rec.name} (proxied:{rec.proxied}) is updated with ip {newip}");
                            } while (false); //Do once
                        });
                    }

                    _logger?.LogInformation($@"Process completed. Sleeping for {_sleepTime / 1000} seconds..");
                    //Loop through the 
                    await Task.Delay(_sleepTime, stoppingToken); // Check every 2 minutes
                } catch (Exception ex) {
                    _logger?.LogError(ex.Message);
                    _client = new FluentClient(); //May be error is because of client. So let us reset the client.
                    await Task.Delay(_sleepTime, stoppingToken);
                }
            }
        }
    }
}
