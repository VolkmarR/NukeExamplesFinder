using NukeExamplesFinder.Gateways;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NukeExamplesFinder.Services
{
    class RenderService
    {
        readonly IFileGateway FileGateway;

        int Score(Repository repo)
            => repo.Stars * 3 + repo.Watchers * 2 + repo.BuildFileSize / 1000 + repo.Targets?.Count ?? 0;

        public RenderService(IFileGateway fileGateway)
        {
            FileGateway = fileGateway;
        }

        string RenderTargets(Repository item)
            => item.Targets != null ? string.Join(", ", item.Targets.Select(q => q.TargetName)) : "";

        string RenderMarkDownDirectory(List<Repository> repositories)
        {
            var sb = new StringBuilder();
            sb
                .AppendLine("# GitHub Repositories using Nuke.Build")
                .AppendLine()
                .AppendLine("| Username| Projectname | Stars | Watchers | Buildfile | Size | Targets |")
                .AppendLine("| --- | --- | --- | --- | --- | --- | --- |");
            foreach (var item in repositories.Where(q => !q.Archived && q.BuildFileSize > 0 && (q.Targets?.Count ?? 0) > 0).OrderByDescending(q => Score(q)).ThenBy(q => q.Name))
                sb.AppendLine($"| {item.Owner} | [{item.Name}]({item.HtmlUrl}) | {item.Stars:N0} | {item.Watchers:N0} | [{item.BuildFilePath}]({item.BuildFileUrl}) | {item.BuildFileSize:N0} | {RenderTargets(item)} |");

            return sb.ToString();
        }

        public void Execute()
        {
            var repoList = FileGateway.LoadRepositories();
            RenderMarkDownDirectory(repoList);
            FileGateway.SaveMarkdownDirectory(RenderMarkDownDirectory(repoList));
        }
    }
}
