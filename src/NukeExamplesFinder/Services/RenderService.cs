using NukeExamplesFinder.Gateways;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace NukeExamplesFinder.Services
{
    class RenderService
    {
        readonly IFileGateway FileGateway;

        readonly string[] SplitArray;

        readonly Dictionary<string, string> TargetMap = new Dictionary<string, string>
        {
            { "unittests", "test" },
            { "unittest", "test" },
            { "runtests", "test" },
            { "tests", "test" },
            { "rununittests", "test" },
        };

        int Score(Repository repo)
            => repo.Stars * 3 + repo.Watchers * 2 + repo.BuildFileSize / 1000 + repo.Targets?.Count ?? 0;

        bool IsRepoValid(Repository repo)
            => !repo.Archived && repo.BuildFileSize > 0 && (repo.Targets?.Count ?? 0) > 0;

        public RenderService(IFileGateway fileGateway)
        {
            FileGateway = fileGateway;

            var splitArray = new List<string> { ";", "{", "}", "(", ")", "_=>_", "restore=>restore", "configurator=>configurator" };
            for (char i = 'a'; i <= 'z'; i++)
                splitArray.Add($"{i}=>{i}");
            SplitArray = splitArray.ToArray();
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
            foreach (var item in repositories.Where(IsRepoValid).OrderByDescending(q => Score(q)).ThenBy(q => q.Name))
                sb.AppendLine($"| {item.Owner} | [{item.Name}]({item.HtmlUrl}) | {item.Stars:N0} | {item.Watchers:N0} | [{item.BuildFilePath}]({item.BuildFileUrl}) | {item.BuildFileSize:N0} | {RenderTargets(item)} |");

            return sb.ToString();
        }

        string MapForGroup(string target)
        {
            target = target.ToLower();
            if (TargetMap.ContainsKey(target))
                target = TargetMap[target];
            return target;
        }

        string BuildFingerprint(string code)
        {
            var indexExecute = code.IndexOf("Execute", StringComparison.OrdinalIgnoreCase);
            if (indexExecute > -1)
                code = code.Substring(indexExecute + 7);
            else
                return "";

            return string.Join("", code.ToLower().Split('\n', '\r')
                .Select(q => q.Trim())
                .Where(q => !string.IsNullOrEmpty(q) && !q.StartsWith("//") && !q.StartsWith("logger.") && !q.StartsWith("nuke.common.logger."))
                .Select(q => string.Join("", q.Replace(" ", "").Split(SplitArray, StringSplitOptions.None)))
                .Where(q => !string.IsNullOrEmpty(q)));
        }

        List<(string target, List<Repository> repos)> PrepareDataTargets(List<Repository> repositories)
        {
            var baseData = repositories.Where(IsRepoValid).SelectMany(q => q.Targets, (r, t) => new { Repository = r, Target = t });
            var data = from q in baseData
                       group q by MapForGroup(q.Target.TargetName) into g
                       select new { Target = g.Key, Repos = g.OrderBy(s => s.Repository.Stars).ToList() };

            var result = new List<(string target, List<Repository> repos)>();
            var fingerprints = new HashSet<string>();
            foreach (var group in data.OrderByDescending(q => q.Repos.Count).ThenBy(q => q.Target))
            {
                var repos = new List<(Repository repo, int length)>();
                foreach (var item in group.Repos)
                {
                    var fingerprint = BuildFingerprint(item.Target.Code);
                    if (!fingerprints.Contains(fingerprint) && fingerprint.Length > 0)
                    {
                        fingerprints.Add(fingerprint);
                        repos.Add((item.Repository, item.Target.Code.Length));
                    }
                }

                if (repos.Count > 0)
                {
                    result.Add((group.Repos[0].Target.TargetName, repos.OrderByDescending(q => q.length).Select(q => q.repo).ToList()));
                    File.WriteAllText(group.Target, string.Join(Environment.NewLine, fingerprints));
                }
            }
            return result;
        }

        string RenderMarkDownTargets(List<Repository> repositories)
        {
            var data = PrepareDataTargets(repositories);
            var limit = 25;
            var sb = new StringBuilder();
            sb.AppendLine("# GitHub Repositories grouped by Target name");

            foreach (var group in data.OrderByDescending(q => q.repos.Count).ThenBy(q => q.target).Where(q => q.repos.Count > 1))
            {
                sb.AppendLine().AppendLine($"## {group.target}").AppendLine();
                foreach (var item in group.repos.Take(limit))
                    sb.AppendLine($"* [{item.Owner}/{item.Name}]({item.HtmlUrl}) - [{item.BuildFilePath}]({item.BuildFileUrl})");
                if (group.repos.Count > limit)
                    sb.AppendLine("* ...");
            }

            return sb.ToString();
        }


        public void Execute()
        {
            var repoList = FileGateway.LoadRepositories();

            FileGateway.SaveMarkdownDirectory(RenderMarkDownDirectory(repoList));
            FileGateway.SaveMarkdownTargets(RenderMarkDownTargets(repoList));
        }
    }
}
