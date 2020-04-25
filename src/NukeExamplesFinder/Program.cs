using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NukeExamplesFinder.Services;
using Octokit;

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
                Environment.Exit(-1);
            }
        }

    }
}
