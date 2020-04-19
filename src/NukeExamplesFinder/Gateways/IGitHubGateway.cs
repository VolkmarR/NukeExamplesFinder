using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NukeExamplesFinder.Gateways
{
    public class RepositoryCodeSearch
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Owner { get; set; }
        public string HtmlUrl { get; set; }
        public bool Archived { get; set; }
        public int Stars { get; set; }
    }

    public class RepositoryDetail
    {
        public long Id { get; set; }
        public string Description { get; set; }
        public bool Archived { get; set; }
        public int Stars { get; set; }
        public int Watchers { get; set; }
    }

    public class BuildFile
    {
        public long RepoId { get; set; }
        public string FilePath { get; set; }
        public string Url { get; set; }
        public int Size { get; set; }
        public string Content { get; set; }
    }

    public interface IGitHubGateway
    {
        Task<List<BuildFile>> GetBuildFilesAsync(List<(long id, string owner, string name, string buildFilePath)> repoList);
        Task<List<RepositoryCodeSearch>> GetRepositoriesWithNukeFileAsync();

        Task<List<RepositoryDetail>> GetRepositoryDetailsAsync(List<long> idList);
    }
}
