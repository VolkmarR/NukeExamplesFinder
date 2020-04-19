using Microsoft.Extensions.Configuration;
using NukeExamplesFinder.Models;
using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;

namespace NukeExamplesFinder.Gateways
{
    class GitHubGateway : IGitHubGateway
    {
        readonly IGitHubClient GitHubClient;
        readonly ILogger Logger;

        RepositoryCodeSearch Transform(SearchCode item)
            => new RepositoryCodeSearch
            {
                Id = item.Repository.Id,
                Owner = item.Repository.Owner.Login,
                Name = item.Repository.Name,
                HtmlUrl = item.Repository.HtmlUrl,
                Archived = item.Repository.Archived,
                Stars = item.Repository.StargazersCount,
            };

        async Task<bool> CheckApiAvailableAsync()
        {
            var apiInfo = GitHubClient.GetLastApiInfo();
            var rateLimit = apiInfo?.RateLimit;
            if (rateLimit == null)
            {
                var serverApiInfo = await GitHubClient.Miscellaneous.GetRateLimits();
                rateLimit = serverApiInfo.Resources.Core;
            }

            if (rateLimit == null || rateLimit.Remaining > 2)
                return true;

            if ((rateLimit.Reset > DateTime.UtcNow) && (rateLimit.Reset - DateTime.UtcNow < new TimeSpan(0, 2, 0)))
            {
                Logger.LogInformation($"Waiting for Rate Limit reset {rateLimit.Reset.ToLocalTime()}");
                Thread.Sleep((int)(rateLimit.Reset - DateTime.UtcNow).Ticks + 1000);
                return true;
            }

            Logger.LogInformation("Rate Limit reset at {reset}", rateLimit.Reset.LocalDateTime);
            return false;
        }

        async Task<(bool canContinue, T result)> ExecServiceAsync<T>(Func<Task<T>> call)
        {
            try
            {
                if (await CheckApiAvailableAsync())
                {
                    Thread.Sleep(100);
                    return (true, await call());
                }
            }
            catch (AbuseException ex)
            {
                Logger.LogError(ex, "ExecService");
            }
            return (false, default);
        }

        async Task<List<RepositoryCodeSearch>> CodeSearchAsync(SearchCodeRequest request, Func<SearchCode, bool> filter)
        {
            request.PerPage = 100;
            request.Page = 1;
            request.Forks = false;
            request.Order = SortDirection.Descending;
            request.SortField = CodeSearchSort.Indexed;

            var result = new List<RepositoryCodeSearch>();

            while (true)
            {
                (var canContinue, var searchResult) = await ExecServiceAsync(() => GitHubClient.Search.SearchCode(request));
                if (!canContinue || searchResult == null || searchResult.Items.Count == 0)
                    break;

                result.AddRange(searchResult.Items.Where(filter).Select(Transform));
                request.Page++;
            }

            return result;
        }

        public GitHubGateway(IGitHubClient gitHubClient, ILogger<GitHubGateway> logger)
        {
            Logger = logger;
            GitHubClient = gitHubClient;
        }

        public async Task<List<RepositoryCodeSearch>> GetRepositoriesWithNukeFileAsync()
        {
            /*
            var searchReq = new SearchCodeRequest("Nuke.Common;")
            {
                Forks = false,
                Language = Language.CSharp,
                PerPage = 100,
                Page = 1,
            };
            */

            var request = new SearchCodeRequest(".sln")
            {
                FileName = ".nuke",
            };

            return await CodeSearchAsync(request, q => string.Equals(q.Path, ".nuke", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<RepositoryDetail>> GetRepositoryDetailsAsync(List<long> idList)
        {
            var result = new List<RepositoryDetail>();
            foreach (var id in idList)
            {
                (var canContinue, var repo) = await ExecServiceAsync(() => GitHubClient.Repository.Get(id));
                if (!canContinue || repo == null)
                    break;

                result.Add(new RepositoryDetail { Id = id, Description = repo.Description, Archived = repo.Archived, Stars = repo.StargazersCount, Watchers = repo.SubscribersCount });
            }

            return result;
        }
    }
}
