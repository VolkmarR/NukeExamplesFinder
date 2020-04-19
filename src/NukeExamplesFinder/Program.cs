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
            var host = Startup.CreateHostBuilder().Build();

            using var serviceScope = host.Services.CreateScope();
            var services = serviceScope.ServiceProvider;

            try
            {
                services.GetRequiredService<RepositoryListService>().ExecuteAsync().GetAwaiter().GetResult();
                services.GetRequiredService<RenderService>().Execute();
            }
            catch (Exception ex)
            {
                services.GetRequiredService<ILogger<RepositoryListService>>().LogError(ex, "Error Occured");
            }
        }

    }
}
