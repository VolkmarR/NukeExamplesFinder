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

    public interface IGitHubGateway
    {
        Task<List<RepositoryCodeSearch>> GetRepositoriesWithNukeFileAsync();

        Task<List<RepositoryDetail>> GetRepositoryDetailsAsync(List<long> idList);
    }
}
