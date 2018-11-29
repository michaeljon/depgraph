using System;
using System.Collections.Generic;
using CommandLine;

namespace depgraph
{
    [Verb("graph", HelpText = "Parse .csproj profiles into a dependency graph")]
    public class ParserOptions
    {
        [Option("include-packages", Required = false, HelpText = "Include PackageReferences in graph", Default = false)]
        public bool IncludePackages { get; set; } = false;

        [Option("include-projects", Required = false, HelpText = "Include ProjectReferences in graph", Default = true)]
        public bool IncludeProjects { get; set; } = true;

        [Option("source-path", Required = true, HelpText = "Source path to search for .csproj files")]
        public string Path { get; set; } = Environment.CurrentDirectory;

        [Option("graph-file", HelpText = "Output graph file")]
        public string GraphFile { get; set; } = "dependencies.dot";

        [Option("left-right", HelpText = "Prefer a left-right layout algorithm", Default = false)]
        public bool LeftRight { get; set; } = false;

        [Option("for-each", HelpText = "Process each discovered .csproj individually", Default = false)]
        public bool ForEach { get; set; } = false;

        [Option("skip-solution", HelpText = "If running a for-each build, skip the top-level solution node", Default = false)]
        public bool SkipSolutionNode { get; set; } = false;

        [Option("exclude", HelpText = "Used as a pattern to exclude top-level projects", Separator = ';')]
        public IEnumerable<string> Exclude { get; set; }

        [Option("verbose", HelpText = "Write a bunch of stuff to the console", Default = false)]
        public bool Verbose { get; set; } = false;

    }
}
