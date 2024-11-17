using Haley.Utils;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace ddns_hcli {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        const string _cfgFileName = "ddns.conf";
        string _cfgFilePath = string.Empty;
        JsonNode _cfg = null;
        public Worker(ILogger<Worker> logger) {
            try {
                //Change logger to Haley logger, so that we can start dumping the logs to a file.
                _logger = logger;
                //Should we even expect the config also to be present in same folder as the main application? What if the user deletes some main dll by mistake???
                //TODO: Change the location of the conf file to some other location.
                _cfgFilePath = Path.Combine(AssemblyUtils.GetBaseDirectory(), _cfgFileName); //to optimize later
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

                if (!File.Exists(_cfgFilePath)) throw new ArgumentException($@"Configuration file {_cfgFileName} doesn't exists. Please add conf file.");
                var txt = File.ReadAllText(_cfgFilePath);
               _cfg = JsonNode.Parse(txt);
            } catch (Exception ex) {
                _logger?.LogError(ex.Message);
            }
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                if (_logger.IsEnabled(LogLevel.Information)) {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }

               

                await Task.Delay(5000, stoppingToken); // Check every 5 seconds.
            }
        }
    }
}
