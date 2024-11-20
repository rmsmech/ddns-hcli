using ddns_hcli;
using Haley.Log;

var builder = Host.CreateApplicationBuilder(args);

var cfgBuilder = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json");
var cfg = cfgBuilder.Build();
builder.Services.AddSingleton<IConfigurationRoot>(cfg);

//If not verbose, write to the directory.
if (!args.Contains("-v", StringComparer.OrdinalIgnoreCase)) {
    var logdir = cfg.GetSection("LogDirectory")?.Get<string>();
    if (string.IsNullOrWhiteSpace(logdir)) {
        logdir = null;
    } else {
        if (!Directory.Exists(logdir)) Directory.CreateDirectory(logdir);
    }
  
    //Clear other providers first.
    builder.Logging.ClearProviders(); //Don't log to console
    builder.Logging.AddHaleyFileLogger((o) => { o.OutputDirectory = logdir; o.FileName = "ddns-hcli"; }); //Since this is a worker service, the log will be created one time and rotation will not happen. So, ensure that single file name is used and rotation is done outside or, conduct log rotation outside of the logger.
}
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
