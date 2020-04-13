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

        Models.Repository ToRepository(SearchCode item)
            => new Models.Repository { Name = item.Repository.Name, HtmlUrl = item.Repository.HtmlUrl };

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

        bool CheckApiAvailable()
        {
            var apiInfo = GitHubClient.GetLastApiInfo();
            var rateLimit = apiInfo?.RateLimit;
            if (rateLimit == null || rateLimit.Remaining > 0)
                return true;

            if (rateLimit.Reset - DateTime.UtcNow < new TimeSpan(0, 2, 0))
            {
                Logger.LogInformation("Waiting for Rate Limit reset");
                Thread.Sleep((int)(rateLimit.Reset - DateTime.UtcNow).Ticks + 1000);
                return true;
            }

            Logger.LogInformation("Rate Limit reset at {reset}", rateLimit.Reset.LocalDateTime);
            return true;
        }

        async Task<(T, bool canContinue)> ExecService<T>(Func<Task<T>> call)
        {
            try
            {
                if (CheckApiAvailable())
                {
                    Thread.Sleep(100);
                    return (await call(), true);
                }
            }
            catch (AbuseException ex)
            {
                Logger.LogError(ex, "ExecService");
            }
            return (default, false);
        }

        async Task<List<RepositoryCodeSearch>> CodeSearchAsync(SearchCodeRequest request, Func<SearchCode, bool> filter)
        {
            request.PerPage = 100;
            request.Page = 1;
            request.Forks = false;

            SearchCodeResult searchResult;
            bool canContinue;
            var result = new List<RepositoryCodeSearch>();

            do
            {
                (searchResult, canContinue) = await ExecService(() => GitHubClient.Search.SearchCode(request));
                if (searchResult == null || !canContinue)
                    break;

                if (searchResult.Items.Count > 0)
                {
                    result.AddRange(searchResult.Items.Where(filter).Select(Transform));
                    if (searchResult.IncompleteResults)
                        request.Page++;
                }

            } while (searchResult.IncompleteResults && CheckApiAvailable());

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
            idList = idList.Take(50).ToList();

            var result = new List<RepositoryDetail>();
            foreach (long id in idList)
            {
                var (repo, canContinue) = await ExecService(() => GitHubClient.Repository.Get(id));
                if (repo == null || !canContinue)
                    break;

                result.Add(new RepositoryDetail { Id = id, Description = repo.Description, Archived = repo.Archived, Stars = repo.StargazersCount, Watchers = repo.SubscribersCount });
            }

            return result;
        }
    }
}
