using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

#pragma warning disable 1591

namespace IK.Imager.Api
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var host = CreateHostBuilder(args).Build();
            await host.RunAsync();
        }
        
        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((_, config) =>
                {
                    config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    config.AddEnvironmentVariables();
                })
                .ConfigureLogging((c, l) =>
                {
                    l.ClearProviders();
                    l.AddConfiguration(c.Configuration);
                    l.AddSystemdConsole(options => options.TimestampFormat = "[dd-MM-yyyy HH:mm:ss.fff] ");
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}