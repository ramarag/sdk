// Copyright (c) .NET Foundation and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Globalization;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using Microsoft.Extensions.DependencyModel;
using Newtonsoft.Json;
using NuGet.ProjectModel;
using NuGet.Packaging.Core;

namespace Microsoft.NET.Build.Tasks
{
    /// <summary>
    /// Generates the $(project).deps.json file.
    /// </summary>
    public class GenerateDepsFile : TaskBase
    {
        [Required]
        public string ProjectPath { get; set; }

        [Required]
        public string AssetsFilePath { get; set; }

        [Required]
        public string DepsFilePath { get; set; }

        [Required]
        public string TargetFramework { get; set; }

        public string RuntimeIdentifier { get; set; }

        public string PlatformLibraryName { get; set; }

        [Required]
        public string AssemblyName { get; set; }

        [Required]
        public string AssemblyExtension { get; set; }

        [Required]
        public string AssemblyVersion { get; set; }

        [Required]
        public ITaskItem[] AssemblySatelliteAssemblies { get; set; }

        [Required]
        public ITaskItem[] ReferencePaths { get; set; }

        [Required]
        public ITaskItem[] ReferenceSatellitePaths { get; set; }

        public ITaskItem CompilerOptions { get; set; }

        public ITaskItem[] PrivateAssetsPackageReferences { get; set; }

        public string[] FilterProjectFiles { get; set; }

        public bool IsSelfContained { get; set; }

        List<ITaskItem> _filesWritten = new List<ITaskItem>();

        [Output]
        public ITaskItem[] FilesWritten
        {
            get { return _filesWritten.ToArray(); }
        }

        protected override void ExecuteCore()
        {
            LockFile lockFile = new LockFileCache(BuildEngine4).GetLockFile(AssetsFilePath);
            CompilationOptions compilationOptions = CompilationOptionsConverter.ConvertFrom(CompilerOptions);

            SingleProjectInfo mainProject = SingleProjectInfo.Create(
                ProjectPath,
                AssemblyName,
                AssemblyExtension,
                AssemblyVersion,
                AssemblySatelliteAssemblies);

            IEnumerable<ReferenceInfo> frameworkReferences =
                ReferenceInfo.CreateFrameworkReferenceInfos(ReferencePaths);

            IEnumerable<ReferenceInfo> directReferences =
                ReferenceInfo.CreateDirectReferenceInfos(ReferencePaths, ReferenceSatellitePaths);

            Dictionary<string, SingleProjectInfo> referenceProjects = SingleProjectInfo.CreateProjectReferenceInfos(
                ReferencePaths,
                ReferenceSatellitePaths);

            IEnumerable<string> privateAssets = PackageReferenceConverter.GetPackageIds(PrivateAssetsPackageReferences);

            ProjectContext projectContext = lockFile.CreateProjectContext(
                NuGetUtils.ParseFrameworkName(TargetFramework),
                RuntimeIdentifier,
                PlatformLibraryName,
                IsSelfContained);

            Dictionary<PackageIdentity, StringBuilder> packagesThatWhereFiltered = null;

            if (FilterProjectFiles != null && FilterProjectFiles.Length > 0)
            {
                packagesThatWhereFiltered = new Dictionary<PackageIdentity,StringBuilder>();
                foreach (var filterProjectFile in FilterProjectFiles)
                {
                    Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, Strings.ParsingFiles, filterProjectFile));
                    var packagesSpecified = CacheArtifactParser.Parse(filterProjectFile);
                    var filterFileName = Path.GetFileName(filterProjectFile);

                    foreach (var pkg in packagesSpecified)
                    {
                        Log.LogMessage(MessageImportance.Low, string.Format(CultureInfo.CurrentCulture, Strings.PackageInfoLog, pkg.Id, pkg.Version));
                        StringBuilder fileList;
                        if (packagesThatWhereFiltered.TryGetValue(pkg, out fileList))
                        {
                            fileList.Append(filterFileName);
                        }
                        else
                        {
                            packagesThatWhereFiltered.Add(pkg, new StringBuilder(filterFileName));
                        }
                    }
                    
                }
            }

            DependencyContext dependencyContext = new DependencyContextBuilder(mainProject, projectContext)
                .WithFrameworkReferences(frameworkReferences)
                .WithDirectReferences(directReferences)
                .WithReferenceProjectInfos(referenceProjects)
                .WithPrivateAssets(privateAssets)
                .WithCompilationOptions(compilationOptions)
                .WithReferenceAssembliesPath(FrameworkReferenceResolver.GetDefaultReferenceAssembliesPath())
                .WithPackagesThatWhereFiltered(packagesThatWhereFiltered)
                .Build();

            var writer = new DependencyContextWriter();
            using (var fileStream = File.Create(DepsFilePath))
            {
                writer.Write(dependencyContext, fileStream);
            }
            _filesWritten.Add(new TaskItem(DepsFilePath));

        }
    }
}
