using Microsoft.Extensions.Options;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;

namespace NukeExamplesFinder.Gateways
{
    class FileGateway : IFileGateway
    {
        readonly string DataPath;
        readonly string ArchivePath;
        readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions { WriteIndented = true };

        void MoveToArchive(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            var archiveFileName = $"{Path.GetFileNameWithoutExtension(filePath)}_{DateTime.Now:yyMMddhms}{Path.GetExtension(filePath)}";
            File.Move(filePath, Path.Combine(ArchivePath, archiveFileName));
        }

        string RepositoriesFilePath => Path.Combine(DataPath, "Repos.json");
        string MarkdownFilePath => Path.Combine(DataPath, "Directory.md");

        public FileGateway(IOptions<DataFilesSettings> dataFileSettings)
        {
            DataPath = dataFileSettings.Value.Path;
            if (string.IsNullOrEmpty(DataPath))
                DataPath = ".\\Data\\";
            Directory.CreateDirectory(DataPath);

            ArchivePath = Path.Combine(DataPath, "Archive");
            Directory.CreateDirectory(ArchivePath);
        }

        public List<Repository> LoadRepositories()
        {
            if (File.Exists(RepositoriesFilePath))
                return JsonSerializer.Deserialize<List<Repository>>(File.ReadAllText(RepositoriesFilePath));

            return new List<Repository>();
        }

        public void SaveRepositories(List<Repository> repositories)
        {
            MoveToArchive(RepositoriesFilePath);
            File.WriteAllText(RepositoriesFilePath, JsonSerializer.Serialize(repositories.OrderBy(q => q.Id), JsonOptions));
        }

        public void SaveMarkdown(string content)
        {
            MoveToArchive(MarkdownFilePath);
            File.WriteAllText(MarkdownFilePath, content);
        }
    }
}
