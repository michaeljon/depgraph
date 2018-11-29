using System;
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

        [Option("add-labels", HelpText = "Replace . in name with a newline", Default = false)]
        public bool AddLabels { get; set; } = false;

        [Option("for-each", HelpText = "Process each discovered .csproj individually", Default = false)]
        public bool ForEach { get; set; } = false;
    }
}
