using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Nuke.Common;
using Nuke.Common.CI;
using Nuke.Common.CI.GitHubActions;
using Nuke.Common.Execution;
using Nuke.Common.Git;
using Nuke.Common.IO;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Tools.DotNet;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using static Nuke.Common.Tools.DotNet.DotNetTasks;

[GitHubActions(
    nameof(Run),
    GitHubActionsImage.WindowsLatest, 
    On = new GitHubActionsTrigger[] { GitHubActionsTrigger.Push }, 
    ImportSecrets = new string[]{ "NukeExamplesFinder_Credentials__GitHubToken" },
    InvokedTargets = new string[] { nameof(Run) })]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Run);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / "src";
    AbsolutePath ArtifactsDirectory => RootDirectory / "artifacts";
    AbsolutePath RunOutputDirectory => RootDirectory / "output";
    Target Clean => _ => _
    .Before(Restore)
    .Executes(() =>
    {
        SourceDirectory.GlobDirectories("**/bin", "**/obj").ForEach(DeleteDirectory);
        EnsureCleanDirectory(ArtifactsDirectory);
    });

    Target Restore => _ => _
        .Executes(() =>
        {
            DotNetRestore(_ => _
                .SetProjectFile(Solution));
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            DotNetBuild(_ => _
                .SetProjectFile(Solution)
                .SetConfiguration(Configuration)
                .SetOutputDirectory(ArtifactsDirectory)
                .EnableNoRestore());
        });

    Target Run => _ => _
        .DependsOn(Compile)
        .Produces(RunOutputDirectory / "Directory.md", RunOutputDirectory / "Repos.json")
        .Executes(() =>
        {
            // Workaround to activate loading user secrets (for executing on developer machine)
            Environment.SetEnvironmentVariable("NETCORE_ENVIRONMENT", "development");

            var settings = new { DataFiles = new { Path = RunOutputDirectory } };
            File.WriteAllText(ArtifactsDirectory / "appsettings.json", JsonConvert.SerializeObject(settings));

            Tool tool = ToolResolver.GetLocalTool(ArtifactsDirectory / "NukeExamplesFinder.exe");
            tool.Invoke(workingDirectory: ArtifactsDirectory);
        });

}
