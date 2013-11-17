﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Versioning;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Newtonsoft.Json.Linq;
using NuGet;

namespace Loader
{
    public class Project
    {
        public const string ProjectFileName = "project.json";

        public string ProjectFilePath { get; private set; }

        public string Name { get; private set; }

        public string Description { get; private set; }

        public string[] Authors { get; private set; }

        public SemanticVersion Version { get; private set; }

        public FrameworkName TargetFramework { get; private set; }

        public IList<Dependency> Dependencies { get; private set; }

        public CompilationOptions CompilationOptions { get; private set; }

        public IEnumerable<string> SourceFiles
        {
            get
            {
                string path = Path.GetDirectoryName(ProjectFilePath);

                string linkedFilePath = Path.Combine(path, ".include");

                var files = Enumerable.Empty<string>();
                if (File.Exists(linkedFilePath))
                {
                    files = File.ReadAllLines(linkedFilePath)
                                .Select(file => Path.Combine(path, file))
                                .Select(p => Path.GetFullPath(p));
                }

                return files.Concat(Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories));
            }
        }

        public static bool HasProjectFile(string path)
        {
            string projectPath = Path.Combine(path, ProjectFileName);

            return File.Exists(projectPath);
        }

        public static bool TryGetProjectName(string path, out string projectName)
        {
            projectName = null;

            if (!HasProjectFile(path))
            {
                return false;
            }

            string projectPath = Path.Combine(path, ProjectFileName);
            string json = File.ReadAllText(projectPath);
            var settings = JObject.Parse(json);
            var name = settings["name"];
            projectName = GetValue<string>(settings, "name");

            if (String.IsNullOrEmpty(projectName))
            {
                // Assume the directory name is the project name
                projectName = GetDirectoryName(path);
            }

            return true;
        }

        public static bool TryGetProject(string path, out Project project)
        {
            project = null;

            string projectPath = null;

            if (Path.GetFileName(path) == ProjectFileName)
            {
                projectPath = path;
            }
            else if (!HasProjectFile(path))
            {
                return false;
            }
            else
            {
                projectPath = Path.Combine(path, ProjectFileName);
            }

            project = new Project();

            string json = File.ReadAllText(projectPath);
            var settings = JObject.Parse(json);
            var version = settings["version"];
            var authors = settings["authors"];
            var compilationOptions = settings["compilationOptions"];
            string framework = GetValue<string>(settings, "targetFramework") ?? "net45";
            project.Name = GetValue<string>(settings, "name");

            if (String.IsNullOrEmpty(project.Name))
            {
                // Assume the directory name is the project name
                project.Name = GetDirectoryName(path);
            }

            project.Version = version == null ? new SemanticVersion("1.0.0") : new SemanticVersion(version.Value<string>());
            project.TargetFramework = VersionUtility.ParseFrameworkName(framework);
            project.Description = GetValue<string>(settings, "description");
            project.Authors = authors == null ? new string[] { } : authors.ToObject<string[]>();
            project.Dependencies = new List<Dependency>();
            project.ProjectFilePath = projectPath;
            project.CompilationOptions = GetCompilationOptions(compilationOptions);

            var dependencies = settings["dependencies"] as JArray;
            if (dependencies != null)
            {
                foreach (JObject dependency in (IEnumerable<JToken>)dependencies)
                {
                    foreach (var prop in dependency)
                    {
                        if (String.IsNullOrEmpty(prop.Key))
                        {
                            throw new InvalidDataException("Unable to resolve dependency ''.");
                        }

                        var properties = prop.Value.Value<JObject>();
                        var dependencyVersion = properties["version"];
                        SemanticVersion semVer = null;

                        if (dependencyVersion != null)
                        {
                            SemanticVersion.TryParse(dependencyVersion.Value<string>(), out semVer);
                        }

                        project.Dependencies.Add(new Dependency
                        {
                            Name = prop.Key,
                            Version = semVer
                        });
                    }
                }
            }

            return true;
        }

        private static CompilationOptions GetCompilationOptions(JToken compilationOptions)
        {
            var options = new CompilationOptions(OutputKind.DynamicallyLinkedLibrary);

            if (compilationOptions == null)
            {
                return options;
            }

            bool allowUnsafe = GetValue<bool>(compilationOptions, "allowUnsafe");
            string platformValue = GetValue<string>(compilationOptions, "platform");
            bool warningsAsErrors = GetValue<bool>(compilationOptions, "warningsAsErrors");

            Platform platform;
            if (!Enum.TryParse<Platform>(platformValue, out platform))
            {
                platform = Platform.AnyCPU;
            }

            ReportWarning warningOption = warningsAsErrors ? ReportWarning.Error : ReportWarning.Default;

            return options.WithAllowUnsafe(allowUnsafe)
                          .WithPlatform(platform)
                          .WithGeneralWarningOption(warningOption);
        }

        private static T GetValue<T>(JToken token, string name)
        {
            var obj = token[name];

            if (obj == null)
            {
                return default(T);
            }

            return obj.Value<T>();
        }
        private static string GetDirectoryName(string path)
        {
            return path.Substring(Path.GetDirectoryName(path).Length).Trim(Path.DirectorySeparatorChar);
        }
    }
}
