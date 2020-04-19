using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace NukeExamplesFinder.Common
{
    static class BuildFileAnalyzer
    {
        static readonly Regex TargetRegex = new Regex(@"Target\s\w+\s=>\s_\s=>\s_");
        static readonly Regex UsingNukeRegex = new Regex(@"using\sNuke.(\w|\.)*;");
        static readonly Regex SequenceRegex = new Regex(@"\.(DependsOn|TriggeredBy|Before|After)\(\w+\)");
        static readonly Regex NukeBuildRegex = new Regex(@"class\s+\w+\s*:\s*NukeBuild");

        public static bool IsCSharpFile(string fileName)
            => Path.GetExtension(fileName ?? "").Equals(".cs", StringComparison.OrdinalIgnoreCase);

        public static int BuildFileHits(string content)
            => string.IsNullOrEmpty(content) ? 0 : TargetRegex.Matches(content).Count +
                    UsingNukeRegex.Matches(content).Count +
                    SequenceRegex.Matches(content).Count +
                    NukeBuildRegex.Matches(content).Count * 5;
    }
}
