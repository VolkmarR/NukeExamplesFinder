using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;

namespace NukeExamplesFinder
{
    class Program
    {
        public IConfiguration Configuration { get; }

        public Program(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public void Run()
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(Configuration.GetValue<string>("Credentials:GitHubToken"));
        }

        static void Main(string[] args)
        {
            var builder = new HostBuilder()
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddLogging(configure => configure.AddConsole());
                   services.AddTransient<Program>();

               })
               .ConfigureAppConfiguration((hostContext, builder) =>
               {
                   builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json", optional: false);
                   
                   if (string.Equals(Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "development", "development", StringComparison.OrdinalIgnoreCase))
                       builder.AddUserSecrets<Program>();
               })
               .UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    services.GetRequiredService<Program>().Run();
                }
                catch (Exception ex)
                {
                    host.Services.GetRequiredService<ILogger<Program>>().LogError(ex, "Error Occured");
                }
            }
        }
    }
}
