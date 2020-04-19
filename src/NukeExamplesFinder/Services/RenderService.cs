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
                .AppendLine("| Name | Url | Stars | Watchers |")
                .AppendLine("| --- | --- | --- | --- |");
            foreach (var item in repositories.OrderBy(q => q.Name).Where(q => !q.Archived))
                sb.AppendLine($"| {item.Name} | {item.HtmlUrl} | {item.Stars} | {item.Watchers} |");

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
