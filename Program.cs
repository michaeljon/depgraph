﻿using System;
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
                    if (options.ForEach == false)
                    {
                        var projects = LoadProjectInformation(options);
                        GenerateGraph(options, options.GraphFile, options.SkipSolutionNode == false, projects);
                    }
                    else
                    {
                        var projectFiles = Directory.EnumerateFiles(options.Path, "*.csproj", SearchOption.AllDirectories);
                        foreach (var projectFile in projectFiles)
                        {
                            var projectInformation = ParseProjectFile(options, projectFile);
                            var graphFile = Path.ChangeExtension(projectFile, ".dot");
                            GenerateGraph(options, graphFile, false, new List<ProjectInformation>() { projectInformation });
                        }
                    }

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

            if (options.Verbose)
            {
                foreach (var exclusion in options.Exclude)
                {
                    Console.WriteLine($"Exlusion: {exclusion}");
                }
            }

            foreach (var projectFile in projectFiles)
            {
                if (options.Verbose)
                {
                    Console.WriteLine($"Project: {projectFile}");
                }

                // if the user has provided a filter, and the current filename matches, then skip it
                if (options.Exclude.Any(e => projectFile.Contains(e, StringComparison.OrdinalIgnoreCase)))
                    continue;

                var projectInformation = ParseProjectFile(options, projectFile);
                projects.Add(projectInformation);
            }

            if (options.Verbose)
            {
                foreach (var project in projects)
                {
                    Console.WriteLine($"Will process: {project.ProjectName}");
                }
            }

            return projects;
        }

        static void GenerateGraph(ParserOptions options, string graphFile, bool generateThisNode, IList<ProjectInformation> projects)
        {
            var nextHue = 0;
            foreach (var project in projects)
            {
                var hue = GenerateHue(nextHue++);
                project.FillColor = GetHueString(hue, 0.50, 0.65);
                project.BorderColor = GetHueString(hue, 1.0, 0.35);

                if (project.ProjectReferences.Count == 0)
                {
                    project.FillColor = "#f0f0f0";
                    project.BorderColor = "#808080";
                }
            }

            var projectList = projects
                .Select(p => p.ProjectName)
                .Union(projects.SelectMany(p => p.ProjectReferences))
                .ToHashSet()
                .OrderBy(p => p);

            var packageList = projects
                .SelectMany(p => p.PackageReferences)
                .ToHashSet()
                .OrderBy(p => p.Name);

            using (var streamWriter = new StreamWriter(graphFile))
            {
                streamWriter.WriteLine("digraph std {");
                if (options.LeftRight)
                {
                    streamWriter.WriteLine("    rankdir=LR;");
                }
                streamWriter.WriteLine("    graph [ bgcolor=white, fontname=Arial, fontcolor=blue, fontsize=8 ];");
                streamWriter.WriteLine("    edge [ fontname=\"Fira Code\", fontcolor=red, fontsize=8, arrowsize=0.4 ];");

                streamWriter.WriteLine("    #");
                streamWriter.WriteLine($"    # This file was generated by depgen on {DateTime.UtcNow}");
                streamWriter.WriteLine("    #");

                if (generateThisNode)
                {
                    streamWriter.WriteLine("    # this project");
                    streamWriter.WriteLine("    ");
                    streamWriter.WriteLine("    node [ fontname=\"Fira Code\", fontcolor=white, color=black, style=filled, fontsize=8, shape=oval ];");
                    streamWriter.WriteLine("    Solution");
                }

                if (options.IncludeProjects)
                {
                    streamWriter.WriteLine();
                    foreach (var project in projectList)
                    {
                        var p = projects
                            .FirstOrDefault(pr => pr.ProjectName == project);
                        if (p == null)
                        {
                            streamWriter.WriteLine($"    \"{project}\" [ fontname=\"Fira Code\", color=red, fontcolor=black, fontsize=8, shape=box, style=dotted ];");
                        }
                        else
                        {
                            var shape="box";
                            var color = p.BorderColor;
                            var fillcolor = p.FillColor;
                            var style = "filled";

                            if (p.ProjectReferences.Count == 0)
                            {
                                shape = "polygon";
                                style = "filled";
                            }
                            streamWriter.WriteLine($"    \"{project}\" [ fontname=\"Fira Code\", fontcolor=black, fontsize=8, color=\"{color}\", fillcolor=\"{fillcolor}\", shape={shape}, style={style} ];");
                        }
                    }
                }

                if (options.IncludePackages)
                {
                    streamWriter.WriteLine();
                    streamWriter.WriteLine("    node [ fontname=\"Fira Code\", fontcolor=black, color=\"#c0c0c0\" style=filled, fontsize=6, shape=oval ];");
                    foreach (var package in packageList)
                    {
                        var label = package.Name + "\\n" + package.Version;
                        streamWriter.WriteLine($"    \"{package}\" [label=\"{label}\"]");
                    }
                }

                foreach (var project in projects)
                {
                    if (options.Verbose)
                    {
                        Console.WriteLine($"{project.ProjectName}");
                    }

                    streamWriter.WriteLine();
                    streamWriter.WriteLine($"    # {project.ProjectName}");

                    if (generateThisNode)
                    {
                        streamWriter.WriteLine($"    Solution -> \"{project.ProjectName}\"");
                    }

                    if (options.IncludeProjects && project.ProjectReferences.Count > 0)
                    {
                        streamWriter.WriteLine($"    edge [ color=\"{project.BorderColor}\", style=solid ];");
                        foreach (var reference in project.ProjectReferences)
                        {
                            if (options.Verbose)
                            {
                                Console.WriteLine($"    -> \"{reference}\" (proj)");
                            }

                            streamWriter.WriteLine($"    \"{project.ProjectName}\" -> \"{reference}\"");
                        }
                    }

                    if (options.IncludePackages && project.PackageReferences.Count > 0)
                    {
                        streamWriter.WriteLine($"    edge [ color=\"{project.BorderColor}\", style=dotted ];");
                        foreach (var reference in project.PackageReferences)
                        {
                            if (options.Verbose)
                            {
                                Console.WriteLine($"    -> \"{reference}\" (pkg)");
                            }

                            streamWriter.WriteLine($"    \"{project.ProjectName}\" -> \"{reference}\"");
                        }
                    }
                }

                streamWriter.WriteLine("}");
            }
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
                .Select(r => new PackageInformation { Name = r.Attribute("Include").Value, Version = r.Attribute("Version").Value })
                .ToList();

            foreach (var reference in references)
            {
                projectInformation.PackageReferences.Add(reference);
            }
        }

        static double GenerateHue(int index)
        {
            var bitcount = 31;
            var rindex = 0;

            for (var i = 0; i < bitcount; i++)
            {
                rindex = (rindex << 1) | (index & 1);
                index >>= 1;
            }

            var hue = rindex / Math.Pow(2, bitcount);
            return ((hue + .6) % 1);
        }

        public static string GetHueString(double hue, double saturation, double level)
        {
            return string.Format("{0:0.000} {1:0.000} {2:0.000}", hue, saturation, level);
        }
    }
}
