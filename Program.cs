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
                    ResolveReferences(projects);
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

        static void ResolveReferences(IList<ProjectInformation> projects)
        {
            return;
        }

        static void GenerateGraph(ParserOptions options, IList<ProjectInformation> projects)
        {
            foreach (var project in projects)
            {
                Console.WriteLine($"Project: {project.ProjectName}");
                
                if (options.IncludeProjects)
                {
                    foreach (var reference in project.ProjectReferences)
                    {
                        Console.WriteLine($"  Ref: {reference}");
                    }
                }

                if (options.IncludePackages)
                {
                    foreach (var reference in project.PackageReferences)
                    {
                        Console.WriteLine($"  Ref: {reference}");
                    }
                }
            }
            return;
        }

        static ProjectInformation ParseProjectFile(ParserOptions options, string projectFile)
        {
            var projectInformation = new ProjectInformation(Path.GetFileNameWithoutExtension(projectFile));
            
            var document = XDocument.Load(projectFile);

            if (options.IncludePackages)
            {
                LoadProjectReferences(document, projectInformation);
            }

            if (options.IncludeProjects)
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
                projectInformation.ProjectReferences.Add(reference);
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
