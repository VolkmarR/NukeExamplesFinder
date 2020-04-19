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
        private readonly IFileGateway FileGateway;

        public RenderService(IFileGateway fileGateway)
        {
            FileGateway = fileGateway;
        }

        string RenderMarkDown(List<Repository> repositories)
        {
            var sb = new StringBuilder();
            sb
                .AppendLine("# GitHub Repositories using Nuke.Build")
                .AppendLine()
                .AppendLine("| Username| Projectname | Stars | Watchers |")
                .AppendLine("| --- | --- | --- | --- |");
            foreach (var item in repositories.Where(q => !q.Archived).OrderByDescending(q => q.Stars).ThenByDescending(q => q.Watchers).ThenBy(q => q.Name))
                sb.AppendLine($"| {item.Owner} | [{item.Name}]({item.HtmlUrl}) | {item.Stars:N0} | {item.Watchers:N0} |");

            return sb.ToString();
        }

        public void Execute()
        {
            var repoList = FileGateway.LoadRepositories();
            RenderMarkDown(repoList);
            FileGateway.SaveMarkdown(RenderMarkDown(repoList));
        }
    }
}
