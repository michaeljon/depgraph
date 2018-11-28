using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using CommandLine;

namespace depgraph
{
    class Program
    {
        static void Main(string[] args)
        {
            var result = Parser.Default.ParseArguments<ParserOptions>(args);

            var exitCode = result.MapResult(
                options =>
                {
                    var projects = LoadProjectInformation(options);
                    GenerateGraph(options, projects);
                    
                    return 0;
                },
                errors =>
                {
                    errors.All(e =>
                    {
                        Console.Error.WriteLine(e);
                        return true;
                    });
                    return 1;
                }
            );

            Environment.Exit(exitCode);
        }

        static IList<ProjectInformation> LoadProjectInformation(ParserOptions options)
        {
            var projects = new List<ProjectInformation>();

            var projectFiles = Directory.EnumerateFiles(options.Path, "*.csproj", SearchOption.AllDirectories);

            foreach (var projectFile in projectFiles)
            {
                var projectInformation = ParseProjectFile(options, projectFile);

                projects.Add(projectInformation);
            }

            return projects;
        }

        static void GenerateGraph(ParserOptions options, IList<ProjectInformation> projects)
        {
            var projectList = projects
                .Select(p => p.ProjectName)
                .Union(projects
                    .SelectMany(p => p.ProjectReferences)
                    .ToHashSet())
                .OrderBy(p => p);

            var packageList = projects
                .SelectMany(p => p.PackageReferences)
                .ToHashSet()
                .OrderBy(p => p);

            using (var streamWriter = new StreamWriter(options.GraphFile))
            {
                streamWriter.WriteLine(
@"digraph std {
    graph [ bgcolor=white, fontname=Arial, fontcolor=blue, fontsize=8 ];
    edge [ fontname=""Fira Code"", fontcolor=red, fontsize=8, arrowsize=0.5 ];
    node [ fontname=""Fira Code"", style=filled, fillcolor=black, fontcolor=white, fontsize=8, shape=""box"" ];

    # this project
    Solution
");

                if (options.IncludeProjects)
                {
                    streamWriter.WriteLine();
                    streamWriter.WriteLine("    node [ fontname=\"Fira Code\", fontcolor=black, style=\"\", fontsize=8, shape=\"box\" ];");
                    foreach (var project in projectList)
                    {
                        if (options.AddLabels)
                        {
                            streamWriter.WriteLine($"    \"{project}\" [label=\"{project.Replace(".", "\\n")}\"]");
                        }
                        else
                        {
                            streamWriter.WriteLine($"    \"{project}\"");
                        }
                    }
                }

                if (options.IncludePackages)
                {
                    streamWriter.WriteLine();
                    streamWriter.WriteLine("    node [ fontname=\"Fira Code\", fontcolor=gray, style=\"\", fontsize=8, shape=\"oval\" ];");
                    foreach (var package in packageList)
                    {
                        if (options.AddLabels)
                        {
                            streamWriter.WriteLine($"    \"{package}\" [label=\"{package.Replace(".", "\\n")}\"]");
                        }
                        else
                        {
                            streamWriter.WriteLine($"    \"{package}\"");
                        }
                    }
                }

                foreach (var project in projects)
                {
                    streamWriter.WriteLine();
                    streamWriter.WriteLine($"    # {project.ProjectName}");
                    streamWriter.WriteLine($"    Solution -> \"{project.ProjectName}\"");

                    if (options.IncludeProjects)
                    {
                        foreach (var reference in project.ProjectReferences)
                        {
                            streamWriter.WriteLine($"    \"{project.ProjectName}\" -> \"{reference}\"");
                        }
                    }

                    if (options.IncludePackages)
                    {
                        foreach (var reference in project.PackageReferences)
                        {
                            streamWriter.WriteLine($"    \"{project.ProjectName}\" -> \"{reference}\"");
                        }
                    }
                }

                streamWriter.WriteLine("}");
            }

            return;
        }

        static ProjectInformation ParseProjectFile(ParserOptions options, string projectFile)
        {
            var projectInformation = new ProjectInformation(Path.GetFileNameWithoutExtension(projectFile));
            
            var document = XDocument.Load(projectFile);

            if (options.IncludeProjects)
            {
                LoadProjectReferences(document, projectInformation);
            }

            if (options.IncludePackages)
            {
                LoadPackageReferences(document, projectInformation);
            }

            return projectInformation;
        }
        
        static void LoadProjectReferences(XDocument document, ProjectInformation projectInformation)
        {
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("x", "http://example.com");

            var references = document
                .XPathSelectElements("//*[local-name()='ProjectReference']", namespaceManager)
                .Select(r => r.Attribute("Include").Value)
                .ToList();

            foreach (var reference in references)
            {
                var slashPos = reference.LastIndexOf('\\');
                var fileName = reference.Substring(slashPos + 1);
                fileName = fileName.Replace(".csproj", "");

                projectInformation.ProjectReferences.Add(fileName);
            }
        }

        static void LoadPackageReferences(XDocument document, ProjectInformation projectInformation)
        {
            var namespaceManager = new XmlNamespaceManager(new NameTable());
            namespaceManager.AddNamespace("x", "http://example.com");

            var references = document
                .XPathSelectElements("//*[local-name()='PackageReference']", namespaceManager)
                .Select(r => r.Attribute("Include").Value)
                .ToList();

            foreach (var reference in references)
            {
                projectInformation.PackageReferences.Add(reference);
            }
        }
    }
}
