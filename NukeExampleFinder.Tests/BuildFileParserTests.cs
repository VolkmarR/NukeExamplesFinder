using FluentAssertions;
using NukeExamplesFinder.Common;
using System;
using System.Linq;
using Xunit;

namespace NukeExampleFinder.Tests
{
    public class BuildFileParserTests
    {
        [Fact]
        public void SimpleScript()
        {
            var content = @"using System;
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

namespace XXX
{
[GitHubActions(
    nameof(Run),
    GitHubActionsImage.WindowsLatest, 
    On = new GitHubActionsTrigger[] { GitHubActionsTrigger.Push }, 
    ImportSecrets = new string[]{ ""NukeExamplesFinder_Credentials__GitHubToken"" },
    InvokedTargets = new string[] { nameof(Run) })]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    public static int Main() => Execute<Build>(x => x.Run);

    [Parameter(""Configuration to build - Default is 'Debug' (local) or 'Release' (server)"")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    [Solution] readonly Solution Solution;
    [GitRepository] readonly GitRepository GitRepository;

    AbsolutePath SourceDirectory => RootDirectory / ""src"";
    AbsolutePath ArtifactsDirectory => RootDirectory / ""artifacts"";
    AbsolutePath RunOutputDirectory => RootDirectory / ""output"";

    Target Clean => _ => _
    .Before(Restore)
    .Executes(clean);

    void clean()
    {
        SourceDirectory.GlobDirectories("" * */ bin"", "" * */ obj"").ForEach(DeleteDirectory);
        EnsureCleanDirectory(ArtifactsDirectory);
    }


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

    ^void
}
}";

            new BuildFileParser(content).Targets.Select(q => q.TargetName).Should().BeEquivalentTo("Clean", "Restore", "Compile");
        }

        [Fact]
        public void Reintroduce()
        {
            var content = @"using System;
using Nuke.Common.Execution;
using Rocket.Surgery.Nuke.DotNetCore;
using Rocket.Surgery.Nuke;
using JetBrains.Annotations;

[PublicAPI]
[CheckBuildProjectConfigurations]
[UnsetVisualStudioEnvironmentVariables]
[AzurePipelinesSteps(
    InvokedTargets = new[] { nameof(Default) },
    NonEntryTargets = new[] { nameof(BuildVersion), nameof(Generate_Code_Coverage_Reports), nameof(Default) },
    ExcludedTargets = new[] { nameof(Restore), nameof(DotnetToolRestore) },
    Parameters = new[] { nameof(CoverageDirectory), nameof(ArtifactsDirectory), nameof(Verbosity), nameof(Configuration) }
)]
[PackageIcon(""https://raw.githubusercontent.com/RocketSurgeonsGuild/graphics/master/png/social-square-thrust-rounded.png"")]
[EnsurePackageSourceHasCredentials(""RocketSurgeonsGuild"")]
[EnsureGitHooks(GitHook.PreCommit)]
class Solution : DotNetCoreBuild, IDotNetCoreBuild
{
    public static int Main() => Execute<Solution>(x => x.Default);

    Target Default => _ => _
        .DependsOn(Restore)
        .DependsOn(Build)
        .DependsOn(Test)
        .DependsOn(Pack)
        ;

    public new Target Restore => _ => _.With(this, DotNetCoreBuild.Restore);

    public new Target Build => _ => _.With(this, DotNetCoreBuild.Build);

    public new Target Test => _ => _.With(this, DotNetCoreBuild.Test);

    public new Target Pack => _ => _.With(this, DotNetCoreBuild.Pack);
}";

            new BuildFileParser(content).Targets.Select(q => q.TargetName).Should().BeEquivalentTo("Default", "Restore", "Build", "Test", "Pack");
        }


    }
}
