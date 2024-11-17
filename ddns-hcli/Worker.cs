using Haley.Utils;

namespace ddns_hcli {
    public class Worker : BackgroundService {
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger) {
            //Change logger to Haley logger, so that we can start dumping the logs to a file.
            _logger = logger;
            var conf = Path.Combine(AssemblyUtils.GetBaseDirectory(), "ddns.conf");
            _logger.LogInformation("Configration Search path " + conf);
            if (!File.Exists(conf)) {
                //Then try to export the file from embedded data.
                _logger.LogInformation("Configuration doesn't exists. Exporting the default file.");
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
