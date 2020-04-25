using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;

namespace NukeExamplesFinder.Gateways
{
    public interface IFileGateway
    {
        void SaveMarkdownDirectory(string content);
        List<Repository> LoadRepositories();
        void SaveRepositories(List<Repository> repositories);


    }
}
