using Microsoft.Extensions.Configuration;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace NukeExamplesFinder.Gateways
{
    class FileGateway : IFileGateway
    {
        readonly string DataPath;
        readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        string RepositoriesFilePath => Path.Combine(DataPath, "Repos.json");
        string MarkdownFilePath => Path.Combine(DataPath, "Directory.md");

        public FileGateway(IConfiguration configuration)
        {
            DataPath = configuration.GetValue<string>("DataFiles:Path");
            if (string.IsNullOrEmpty(DataPath))
                DataPath = ".\\Data\\";
            Directory.CreateDirectory(DataPath);
        }

        public List<Repository> LoadRepositories()
        {
            if (File.Exists(RepositoriesFilePath))
                return JsonSerializer.Deserialize<List<Repository>>(File.ReadAllText(RepositoriesFilePath));

            return new List<Repository>();
        }

        public void SaveRepositories(List<Repository> repositories)
        {
            File.WriteAllText(RepositoriesFilePath, JsonSerializer.Serialize(repositories, JsonOptions));
        }

        public void SaveMarkdown(string content)
        {
            File.WriteAllText(MarkdownFilePath, content);
        }
    }
}
