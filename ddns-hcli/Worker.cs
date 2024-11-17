using Haley.Utils;
using System.Reflection;
using System.Text.Json;
using Haley.Rest;
using Haley.Models;

namespace ddns_hcli {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        const string _cfgFileName = "ddns.conf";
        const string _ipfFileName = "ipfinder.conf";
        string _cfgFilePath = string.Empty;
        string _ipfFilePath = string.Empty;
        List<ZoneInfo> _cfgList = new List<ZoneInfo>();
        List<IpFinder> _ipfList = new List<IpFinder>();
        int _ipfIndex = 0;
        int _ipfCount = 0;
        FluentClient _client = new FluentClient();

        int continuousErrorCount = 0;
        public Worker(ILogger<Worker> logger) {
            try {
                //Change logger to Haley logger, so that we can start dumping the logs to a file.
                _logger = logger;
                //Should we even expect the config also to be present in same folder as the main application? What if the user deletes some main dll by mistake???
                //TODO: Change the location of the conf file to some other location.
                _cfgFilePath = Path.Combine(AssemblyUtils.GetBaseDirectory(), _cfgFileName); //to optimize later
                _ipfFilePath = Path.Combine(AssemblyUtils.GetBaseDirectory(), _ipfFileName);
                _logger.LogInformation("Configration Search path " + _cfgFilePath);
                if (!File.Exists(_cfgFilePath)) {
                    //Then try to export the file from embedded data.
                    _logger.LogInformation("Configuration doesn't exists. Exporting the default file.");
                    //var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                    var res = ResourceUtils.GetEmbeddedResource($@"ddns_hcli.{_cfgFileName}", Assembly.GetExecutingAssembly());
                    using (var fs = new FileStream(_cfgFilePath, FileMode.Create)) {
                        fs.Write(res);
                        fs.Close();
                    }
                }

                if (!File.Exists(_ipfFilePath)) {
                    //Then try to export the file from embedded data.
                    _logger.LogInformation("Configuration doesn't exists. Exporting the default file.");
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
            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            try {
                while (!stoppingToken.IsCancellationRequested) {
                    if (_logger.IsEnabled(LogLevel.Information)) {
                        _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                    }

                    //First try to fetch the ip address from external source.
                    if (_ipfIndex > (_ipfCount - 1) || _ipfIndex < 0) _ipfIndex = 0;
                    var ipf = _ipfList[_ipfIndex];
                    string ipaddr = string.Empty;
                    try {
                        ipaddr = (await (await _client.WithEndPoint(ipf.URL).GetAsync()).AsStringResponseAsync()).ToString();
                    } catch (Exception) {
                        _ipfIndex++; //Move to next ip finder incase previous had error.
                        ipf.ErrorCount++; //So we can track which IP Finder has continous error and report that to be removed.
                        continuousErrorCount++; //For some reason, if we have continous error for sometime, we should not proceed with application at all. .may be internet connection is down..
                        if (continuousErrorCount > 10) {
                            _logger.LogInformation("Continuous error for more than 10 times. Taking a break of 15 mintues");
                            await Task.Delay(900000, stoppingToken); // Wait for 15 minutes, may be internet is down.
                            continuousErrorCount = 0; //Reset the error count.
                        }
                        continue;
                    }
                    _ipfIndex++; //Move to next ip finder.
                    await Task.Delay(2000, stoppingToken); // Check every 2 minutes
                }
            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
            }
        }
    }
}
