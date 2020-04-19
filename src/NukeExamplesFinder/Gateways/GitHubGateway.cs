using Octokit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.Extensions.Logging;
using NukeExamplesFinder.Common;

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
                    return (true, await call());
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

        async Task<bool> AddCodeFiles(string owner, string name, IEnumerable<string> pathList, List<RepositoryContent> contentList)
        {
            if (!await CheckApiAvailableAsync())
                return false;

            foreach (var path in pathList)
            {
                (var canContinue, var contentResponse) = await ExecServiceAsync(() => GitHubClient.Repository.Content.GetAllContents(owner, name, path));
                if (!canContinue)
                    return false;
                contentList.Add(contentResponse[0]);
            }

            return true;
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
            Logger.LogInformation("Searching Repositories containing a .nuke file");

            var request = new SearchCodeRequest(".sln")
            {
                FileName = ".nuke",
            };

            return await CodeSearchAsync(request, q => string.Equals(q.Path, ".nuke", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<List<RepositoryDetail>> GetRepositoryDetailsAsync(List<long> idList)
        {
            Logger.LogInformation("Refeshing {count} repository details", idList.Count);

            var result = new List<RepositoryDetail>();
            var logPoition = 0;
            Octokit.Repository repo;
            bool canContinue;

            foreach (var id in idList)
            {
                (canContinue, repo) = await ExecServiceAsync(() => GitHubClient.Repository.Get(id));
                if (!canContinue || repo == null)
                    break;

                result.Add(new RepositoryDetail
                {
                    Id = id,
                    Description = repo.Description,
                    Archived = repo.Archived,
                    Stars = repo.StargazersCount,
                    Watchers = repo.SubscribersCount,

                });

                if (++logPoition % 25 == 0)
                    Logger.LogInformation("{position}", logPoition);
            }

            return result;
        }

        public async Task<List<BuildFile>> GetBuildFilesAsync(List<(long id, string owner, string name, string buildFilePath)> repoList)
        {
            Logger.LogInformation("Refeshing {count} build files", repoList.Count);

            var result = new List<BuildFile>();
            var logPoition = 0;
            List<RepositoryContent> files = new List<RepositoryContent>();

            foreach (var (id, owner, name, buildFilePath) in repoList)
            {
                files.Clear();
                var canContinue = true;
                IReadOnlyList<RepositoryContent> contentListResponse;

                try
                {
                    var path = !string.IsNullOrWhiteSpace(buildFilePath) ? buildFilePath : "build/build.cs";
                    (canContinue, contentListResponse) = await ExecServiceAsync(() => GitHubClient.Repository.Content.GetAllContents(owner, name, path));
                    if (contentListResponse.Count == 1)
                        files.Add(contentListResponse[0]);
                }
                catch (NotFoundException)
                { }

                if (files.Count == 0)
                {
                    try
                    {
                        var path = !string.IsNullOrWhiteSpace(buildFilePath) ? buildFilePath : "build";
                        (canContinue, contentListResponse) = await ExecServiceAsync(() => GitHubClient.Repository.Content.GetAllContents(owner, name, path));
                        if (contentListResponse.Count == 1)
                            files.Add(contentListResponse[0]);
                        else if (canContinue)
                            canContinue = await AddCodeFiles(owner, name, contentListResponse.Where(q => q.Type == ContentType.File && BuildFileAnalyzer.IsCSharpFile(q.Name)).Select(q => q.Path), files);

                        if (!files.Any(q => q.Type == ContentType.File && BuildFileAnalyzer.IsCSharpFile(q.Name) && BuildFileAnalyzer.BuildFileHits(q.Content) > 1))
                            files.Clear();
                    }
                    catch (NotFoundException)
                    {
                        files.Clear();
                    }
                }

                if (files.Count == 0)
                {
                    var searchCodeRequest = new SearchCodeRequest("Nuke", owner, name) { Language = Language.CSharp, In = new List<CodeInQualifier> { CodeInQualifier.File } };
                    SearchCodeResult searchResponse;

                    (canContinue, searchResponse) = await ExecServiceAsync(() => GitHubClient.Search.SearchCode(searchCodeRequest));
                    if (canContinue && searchResponse != null)
                        canContinue = await AddCodeFiles(owner, name, searchResponse.Items.Where(q => BuildFileAnalyzer.IsCSharpFile(q.Name)).Select(q => q.Path), files);
                }

                if (!canContinue)
                    break;

                var contentFile = files?.Where(q => q.Type == ContentType.File && BuildFileAnalyzer.IsCSharpFile(q.Name))
                                    .OrderByDescending(q => BuildFileAnalyzer.BuildFileHits(q.Content))
                                    .FirstOrDefault();

                if (contentFile != null)
                    result.Add(new BuildFile { RepoId = id, FilePath = contentFile.Path, Url = contentFile.HtmlUrl, Size = contentFile.Size, Content = contentFile.Content });

                if (++logPoition % 25 == 0)
                    Logger.LogInformation("{position}", logPoition);
            }

            return result;
        }

    }
}
