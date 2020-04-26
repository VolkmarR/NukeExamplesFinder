using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NukeExamplesFinder.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NukeExamplesFinder.Common
{
    public class BuildFileParser
    {
        readonly SyntaxTree Tree;

        List<T> SearchFor<T>() where T: SyntaxNode
            => Tree.GetRoot().DescendantNodes().OfType<T>().ToList();

        BuildFileTarget  AnalyzeProperty(PropertyDeclarationSyntax property)
        {
            var childs = property.ChildNodesAndTokens();
            if (childs.Any(q => q.IsNode && q.ToString() == "Target") && property.ToString().Contains("Execute"))
            {
                return new BuildFileTarget { TargetName = property.Identifier.ToString(), Code = property.ToString() };
            }
            return null;
        }

        public readonly List<BuildFileTarget> TargetsWithExecute;

        public BuildFileParser(string content)
        {
            if (string.IsNullOrEmpty(content))
            {
                TargetsWithExecute = new List<BuildFileTarget>();
                return;
            }

            Tree = CSharpSyntaxTree.ParseText(content);

            TargetsWithExecute = SearchFor<PropertyDeclarationSyntax>().Select(AnalyzeProperty).Where(q => q != null).ToList();
        }
    }
}
