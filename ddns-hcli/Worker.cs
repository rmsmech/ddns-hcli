using Haley.Utils;
using System.Reflection;

namespace ddns_hcli {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;
        const string _confName = "ddns.conf";
        public Worker(ILogger<Worker> logger) {
            //Change logger to Haley logger, so that we can start dumping the logs to a file.
            _logger = logger;
            var conf = Path.Combine(AssemblyUtils.GetBaseDirectory(), _confName);
            _logger.LogInformation("Configration Search path " + conf);
            if (!File.Exists(conf)) {
                //Then try to export the file from embedded data.
                _logger.LogInformation("Configuration doesn't exists. Exporting the default file.");
                //var names = Assembly.GetExecutingAssembly().GetManifestResourceNames();
                var res = ResourceUtils.GetEmbeddedResource($@"ddns_hcli.{_confName}", Assembly.GetExecutingAssembly());
                using (var fs = new FileStream(conf, FileMode.Create)) {
                    fs.Write(res);
                    fs.Close();
                }
            }


        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                if (_logger.IsEnabled(LogLevel.Information)) {
                    _logger.LogInformation("Worker running at: {time}", DateTimeOffset.Now);
                }
                await Task.Delay(1000, stoppingToken); 
            }
        }
    }
}
