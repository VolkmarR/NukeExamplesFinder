using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NukeExamplesFinder.Gateways;
using NukeExamplesFinder.Models;
using NukeExamplesFinder.Services;
using Octokit;
using System;
using System.IO;

namespace NukeExamplesFinder
{
    public static class Startup
    {
        public static IHostBuilder CreateHostBuilder()
            => new HostBuilder()
                .ConfigureAppConfiguration(ConfigureAppConfiguration)
                .ConfigureServices(ConfigureServices)
                .UseConsoleLifetime();

        public static void ConfigureAppConfiguration(HostBuilderContext hostContext, IConfigurationBuilder builder)
        {
            builder
             .SetBasePath(Directory.GetCurrentDirectory())
             .AddJsonFile($"appsettings.json", optional: false)
             .AddEnvironmentVariables("");

            if (string.Equals(Environment.GetEnvironmentVariable("NETCORE_ENVIRONMENT") ?? "development", "development", StringComparison.OrdinalIgnoreCase))
                builder.AddUserSecrets<Program>();
        }

        public static void ConfigureServices(HostBuilderContext hostContext, IServiceCollection services)
        {
            services.Configure<CredentialsSettings>(hostContext.Configuration.GetSection("Credentials"));
            services.Configure<DataFilesSettings>(hostContext.Configuration.GetSection("DataFiles"));
            services.AddLogging(configure => configure.AddConsole());
            services.AddTransient<RenderService>();
            services.AddTransient<RepositoryListService>();
            services.AddTransient<IGitHubGateway, GitHubGateway>();
            services.AddTransient<IFileGateway, FileGateway>();

            services.AddTransient<IGitHubClient>((serviceProvider) =>
            {
                var token = serviceProvider.GetRequiredService<IOptions<CredentialsSettings>>().Value.GitHubToken;
                if (string.IsNullOrEmpty(token))
                    throw new ArgumentException("Credentials:GitHubToken can not be empty");

                return new GitHubClient(new ProductHeaderValue("NukeExampleFinder")) { Credentials = new Credentials(token) };
            });
        }

    }
}
