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

        void UpdateValues(Repository values, BuildFile newValues)
        {
            values.BuildFilePath = newValues.FilePath;
            values.BuildFileUrl = newValues.Url;
            values.BuildFileSize = newValues.Size;
            values.BuildFileContent = newValues.Content;
            values.LastBuildFileUpdated = DateTime.Now;
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

            var nukeFile = await GitHubGateway.GetRepositoriesWithNukeFileAsync();
            var nukeBuild = await GitHubGateway.GetRepositoriesWithNukeBuildAsync();

            // Update the RepoList
            foreach (var item in nukeFile.Union(nukeBuild))
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

            // Refresh the Build Files
            refreshTrigger = DateTime.Now.AddDays(-7);
            var refreshRepoList = repoList.Where(q => q.LastBuildFileUpdated < refreshTrigger && !q.Archived).OrderByDescending(q => q.LastBuildFileUpdated).Select(q => (q.Id, q.Owner, q.Name, q.BuildFilePath)).ToList();
            foreach (var item in await GitHubGateway.GetBuildFilesAsync(refreshRepoList))
            {
                if (repoIndex.TryGetValue(item.RepoId, out var repository))
                    UpdateValues(repository, item);
            }

            FileGateway.SaveRepositories(repoList);
        }
    }
}
