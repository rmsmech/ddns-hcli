using hdns;
using Haley.Log;
using Haley.Utils;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

var cfgBuilder = new ConfigurationBuilder().SetBasePath(AssemblyUtils.GetBaseDirectory()).AddJsonFile("appsettings.json");
var cfg = cfgBuilder.Build();
builder.Services.AddSingleton<IConfigurationRoot>(cfg);

Globals.LogDirectory = FetchVariables("env_hdns_logdir", "LogDirectory")?.ToString();
Globals.CfgDirectory = FetchVariables("env_hdns_cfgdir", "CfgDirectory")?.ToString();
if (int.TryParse(FetchVariables("env_hdns_sleeptime", "WorkerSleepTime")?.ToString(), out int sleeptime)) {
    Globals.WorkerSleepTime = sleeptime;
} else {
    Globals.WorkerSleepTime = 120; //Seconds
}
    //If not verbose, write to the directory.
if (!args.Contains("-v", StringComparer.OrdinalIgnoreCase)) {

    if (!string.IsNullOrWhiteSpace(Globals.LogDirectory)) {
        if (!Directory.Exists(Globals.LogDirectory)) Directory.CreateDirectory(Globals.LogDirectory);
    }
  
    //Clear other providers first.
    builder.Logging.ClearProviders(); //Don't log to console
    builder.Logging.AddHaleyFileLogger((o) => { o.OutputDirectory = Globals.LogDirectory; o.FileName = "hdns"; }); //Since this is a worker service, the log will be created one time and rotation will not happen. So, ensure that single file name is used and rotation is done outside or, conduct log rotation outside of the logger.
}
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();

object FetchVariables(string env_name, string cfg_name) {
    //1. Preference to Environment variables
    //2. Appsettings.json
    if (string.IsNullOrWhiteSpace(env_name) || string.IsNullOrWhiteSpace(cfg_name)) return string.Empty;
    object value = Environment.GetEnvironmentVariable(env_name);
    if (value == null) {
        value = cfg.GetSection(cfg_name).Get<object>();
    }
    return value;
}