using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;

namespace NukeExamplesFinder.Gateways
{
    public interface IFileGateway
    {
        void SaveMarkdown(string content);
        List<Repository> LoadRepositories();
        void SaveRepositories(List<Repository> repositories);


    }
}
