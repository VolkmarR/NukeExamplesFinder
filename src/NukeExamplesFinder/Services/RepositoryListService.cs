using NukeExamplesFinder.Gateways;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NukeExamplesFinder.Services
{
    class RepositoryListService
    {
        readonly IGitHubGateway GitHubGateway;
        readonly IFileGateway FileGateway;

        void UpdateValues(Repository values, RepositoryCodeSearch newValues)
        {
            values.Id = newValues.Id;
            values.Owner = newValues.Owner;
            values.Name = newValues.Name;
            values.HtmlUrl = newValues.HtmlUrl;
            values.LastIndexUpdated = DateTime.Now;
        }

        void UpdateValues(Repository values, RepositoryDetail newValues)
        {
            values.Stars = newValues.Stars;
            values.Watchers = newValues.Watchers;
            values.Archived = newValues.Archived;
            values.Description = newValues.Description;
            values.LastDetailUpdated = DateTime.Now;
        }

        string RenderMarkDown(List<Repository> repositories)
        {
            var sb = new StringBuilder();
            sb
                .AppendLine("# GitHub Repositories using Nuke.Build")
                .AppendLine()
                .AppendLine("| Name | Url | Stars | Watchers |")
                .AppendLine("| --- | --- | --- | --- |");
            foreach (var item in repositories.OrderBy(q => q.Name).Where(q => !q.Archived))
                sb.AppendLine($"| {item.Name} | {item.HtmlUrl} | {item.Stars} | {item.Watchers} |");

            return sb.ToString();
        }

        public RepositoryListService(IGitHubGateway gitHubGateway, IFileGateway fileGateway)
        {
            FileGateway = fileGateway;
            GitHubGateway = gitHubGateway;
        }

        public async Task ExecuteAsync()
        {
            var repoList = FileGateway.LoadRepositories();
            var repoIndex = repoList.ToDictionary(q => q.Id, q => q);

            // Update the RepoList
            foreach (var item in await GitHubGateway.GetRepositoriesWithNukeFileAsync())
            {
                if (!repoIndex.TryGetValue(item.Id, out var repository))
                {
                    repository = new Repository { FirstIndexed = DateTime.Now };
                    repoList.Add(repository);
                    repoIndex[item.Id] = repository;
                }

                UpdateValues(repository, item);
            }

            // Refresh the Repo Details
            var refreshTrigger = DateTime.Now.AddDays(-1);
            var ids = repoList.Where(q => q.LastDetailUpdated < refreshTrigger).OrderByDescending(q => q.LastDetailUpdated).Select(q => q.Id).ToList();
            foreach (var item in await GitHubGateway.GetRepositoryDetailsAsync(ids))
            {
                if (repoIndex.TryGetValue(item.Id, out var repository))
                    UpdateValues(repository, item);
            }

            FileGateway.SaveRepositories(repoList);

            FileGateway.SaveMarkdown(RenderMarkDown(repoList));
        }
    }
}
