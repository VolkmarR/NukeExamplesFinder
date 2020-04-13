using Microsoft.Extensions.Configuration;
using System;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.IO;
using Octokit;
using NukeExamplesFinder.Services;
using NukeExamplesFinder.Gateways;

namespace NukeExamplesFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            var builder = new HostBuilder()
               .ConfigureAppConfiguration((hostContext, builder) =>
               {
                   builder
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile($"appsettings.json", optional: false);

                   if (string.Equals(Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "development", "development", StringComparison.OrdinalIgnoreCase))
                       builder.AddUserSecrets<Program>();
               })
               .ConfigureServices((hostContext, services) =>
               {
                   services.AddLogging(configure => configure.AddConsole());
                   services.AddTransient<ExampleFinderService>();
                   services.AddTransient<RepositoryListService>();
                   services.AddTransient<IGitHubGateway, GitHubGateway>();
                   services.AddTransient<IFileGateway, FileGateway>();
                   services.AddGitHubClient();
               })
               .UseConsoleLifetime();

            var host = builder.Build();

            using (var serviceScope = host.Services.CreateScope())
            {
                var services = serviceScope.ServiceProvider;

                try
                {
                    services.GetRequiredService<RepositoryListService>().ExecuteAsync().GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    host.Services.GetRequiredService<ILogger<ExampleFinderService>>().LogError(ex, "Error Occured");
                }
            }
        }
    }
    static class ServiceRegistration
    {
        public static IServiceCollection AddGitHubClient(this IServiceCollection collection)
        {
            return collection.AddTransient<IGitHubClient>((serviceProvider) =>
            {
                var token = serviceProvider.GetRequiredService<IConfiguration>().GetValue<string>("Credentials:GitHubToken");
                if (string.IsNullOrEmpty(token))
                    throw new ArgumentException("Credentials:GitHubToken can not be empty");

                return new GitHubClient(new ProductHeaderValue("NukeExampleFinder")) { Credentials = new Credentials(token) };
            });
        }
    }
}
